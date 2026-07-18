using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.IO.Enumeration;
using System.Runtime.InteropServices;
using Engine.Core;

namespace HarpyEngine.Resources.Mnemosyne;

public record struct AssetUpdated(AssetInfo Info) : IEvent;
public record struct AssetRemoved(string RelativePath) : IEvent;

public class AssetDatabase
{
    // --- Singleton Architecture ---
    private static readonly Lazy<AssetDatabase> _instance = new(() => new AssetDatabase());
    public static AssetDatabase Instance => _instance.Value;

    // Enforce private constructor to guarantee zero external instantiations
    private AssetDatabase() { }

    private static readonly Dictionary<string, AssetType> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".glsl", AssetType.Shader },
        { ".compute", AssetType.Shader },
        { ".png", AssetType.Texture },
        { ".jpg", AssetType.Texture }, 
        { ".tga", AssetType.Texture },
        { ".cs", AssetType.Script },
        { ".obj", AssetType.Model },
        { ".fbx", AssetType.Model },
        { ".glb", AssetType.Model },
        { ".gltf", AssetType.Model },
        { ".anim", AssetType.Animation },
        { ".mat", AssetType.Material },
        { ".txt", AssetType.Unknown },
    };

    private static readonly FrozenDictionary<string, AssetType> FastMap = 
        ExtensionMap.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentQueue<string> _dirtyFiles = new();
    private readonly Dictionary<string, AssetInfo> _assets = new(2048, StringComparer.OrdinalIgnoreCase);
    private string _root = "";
    private FileSystemWatcher? _watcher;

    public void Init(string rootPath)
    {
        // 1. Normalize the incoming path format immediately
        var targetRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));

        // 2. IDEMPOTENCY GUARD: If already watching an Asset folder, block root-level hijacking attempts.
        if (_watcher != null)
        {
            if (_root.EndsWith("Assets", StringComparison.OrdinalIgnoreCase) && !targetRoot.EndsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning($"AssetDatabase already initialized on explicit directory context: {_root}. Intercepted and blocked lower-priority root clobber request targeting: {targetRoot}");
                return;
            }

            Logger.LogInfo($"Re-routing asset workspace context. Cleaning up old watch loop targeting: {_root}");
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }

        _root = targetRoot;
        Logger.LogInfo($"Initializing root tracking path: {_root}");
        ImportFolder(_root); 
        
        AttachFileWatcher();
    }

    public void UpdatePath(string rootPath)
    {
        var targetRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));

        if (_watcher != null)
        {
            if (_root.EndsWith("Assets", StringComparison.OrdinalIgnoreCase) && !targetRoot.EndsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning($"AssetDatabase already initialized on explicit directory context: {_root}. Intercepted and blocked lower-priority root clobber request targeting: {targetRoot}");
                return;
            }

            Logger.LogInfo($"Re-routing asset workspace context. Cleaning up old watch loop targeting: {_root}");
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
        _assets.Clear();
        _dirtyFiles.Clear();
        _root = targetRoot;
        Logger.LogInfo($"Initializing root tracking path: {_root}");
        ImportFolder(_root);
        AttachFileWatcher();
    }

    private void AttachFileWatcher()
    {
        var filter = OperatingSystem.IsLinux() ? "*" : "*.*";

        _watcher = new FileSystemWatcher(_root, filter)
        {
            IncludeSubdirectories = true,
            // Catch every relevant bit configuration including sizing changes for atomic write cycles
            NotifyFilter = NotifyFilters.LastWrite 
                           | NotifyFilters.FileName 
                           | NotifyFilters.DirectoryName 
                           | NotifyFilters.CreationTime
                           | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        _watcher.Changed += (_, e) => { Logger.LogTrace($"OS CHANGED event caught: {e.FullPath}"); UpdateAssetMetadata(e.FullPath); };
        _watcher.Created += (_, e) => { Logger.LogTrace($"OS CREATED event caught: {e.FullPath}"); UpdateAssetMetadata(e.FullPath); };
        _watcher.Deleted += (_, e) => { Logger.LogTrace($"OS DELETED event caught: {e.FullPath}"); OnDeleted(e.FullPath); };
        _watcher.Renamed += (_, e) => 
        { 
            Logger.LogTrace($"OS RENAMED event caught: {e.OldFullPath} -> {e.FullPath}"); 
            OnDeleted(e.OldFullPath); 
            UpdateAssetMetadata(e.FullPath); 
        };

        Logger.LogSuccess($"FileSystemWatcher successfully initialized and listening on: {_root}");
    }

    private void OnDeleted(string absolutePath)
    {
        var relative = Path.GetRelativePath(_root, absolutePath);
        if (!_assets.Remove(relative)) return;
        Logger.LogTrace($"Processing deletion. Removed tracking context for path: {relative}");
        Event<AssetRemoved>.Invoke(new AssetRemoved(relative));
    }

    public readonly record struct FileResult(bool Success, string? Path);

    public FileResult TryGetNextDirtyFile()
    {
        return _dirtyFiles.TryDequeue(out var path) ? new FileResult(true, path) : new FileResult(false, null);
    }
    
    public void UpdateAssetMetadata(string absolutePath)
    {
        if (Directory.Exists(absolutePath)) return;

        var relative = Path.GetRelativePath(_root, absolutePath);
        var ext = Path.GetExtension(absolutePath.AsSpan());

        if (!TryGetAssetType(ext, out var type))
        {
            Logger.LogTrace($"Ignoring asset with unknown extension: {absolutePath}");
            return;
        }
        
        var info = new AssetInfo(relative, absolutePath, type);
        _assets[relative] = info;
        _dirtyFiles.Enqueue(absolutePath);
        
        Logger.LogSuccess($"Asset cache updated/added. Type: {type} | Path: {relative}");
        Event<AssetUpdated>.Invoke(new AssetUpdated(info));
    }
    
    private static bool TryGetAssetType(ReadOnlySpan<char> extension, out AssetType type)
    {
        var lookup = FastMap.GetAlternateLookup<ReadOnlySpan<char>>();
        return lookup.TryGetValue(extension, out type);
    }

    public void ImportFolder(string rootPath)
    {
        Logger.LogInfo($"Performing cold folder scan on: {rootPath}");
        var options = new EnumerationOptions { RecurseSubdirectories = true };
        var lookup = FastMap.GetAlternateLookup<ReadOnlySpan<char>>();
        var rootLen = rootPath.EndsWith(Path.DirectorySeparatorChar) ? rootPath.Length : rootPath.Length + 1;

        var count = 0;
        var enumerable = new FileSystemEnumerable<bool>(
            rootPath,
            (ref entry) =>
            {
                if (entry.IsDirectory) return true;

                var ext = Path.GetExtension(entry.FileName);
                if (!lookup.TryGetValue(ext, out var type)) return true;
                
                var fullPath = entry.ToFullPath();
                var relative = fullPath[rootLen..];
                
                var lastWrite = entry.LastWriteTimeUtc.DateTime;
                var info = new AssetInfo(relative, fullPath, type);
                
                _assets[relative] = info;
                count++;
                return true;
            },
            options);

        foreach (var _ in enumerable) { }
        Logger.LogSuccess($"Import finished. Total source assets indexed into cache: {count}");
    }

    public void RequeueDirtyFile(string path)
    {
        _dirtyFiles.Enqueue(path);
    }

    public ref readonly AssetInfo GetAsset(string relativePath)
    {
        return ref CollectionsMarshal.GetValueRefOrNullRef(_assets, relativePath);
    }

    public string GetRootPath() => _root;
    public IEnumerable<AssetInfo> GetAllAssets() => _assets.Values;
}