using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.IO.Enumeration;
using Engine.Core;

namespace HarpyEngine.Resources.Mnemosyne;

public record struct AssetUpdated(AssetInfo Info) : IEvent;
public record struct AssetRemoved(string RelativePath) : IEvent;

// Sealed: prevents subclassing from creating a second construction path
// or exposing the protected/private constructor via a derived type.
public sealed class AssetDatabase
{
    // --- Singleton Architecture ---
    private static readonly Lazy<AssetDatabase> _instance =
        new(() => new AssetDatabase(), LazyThreadSafetyMode.ExecutionAndPublication);

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

    // ConcurrentDictionary: safe for the FileSystemWatcher's background-thread
    // writes to interleave with reads from GetAsset/GetAllAssets without locking.
    private readonly ConcurrentDictionary<string, AssetInfo> _assets =
        new(concurrencyLevel: Environment.ProcessorCount, capacity: 2048, StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentQueue<string> _dirtyFiles = new();

    // Guards _root / _watcher reassignment and whole-collection operations
    // (Init, UpdatePath, ImportFolder) so they can't interleave with each other
    // or with a watcher callback mutating state mid-swap.
    private readonly Lock _lifecycleLock = new();

    private volatile string _root = "";
    private FileSystemWatcher? _watcher;

    // Init is idempotent bootstrap: "make sure the database is running against
    // *some* root." First caller wins; later callers are silent no-ops as long
    // as a watcher is already active. Multiple independent systems (Renderer,
    // EditorWindow, CI harness, etc.) can all call this defensively on startup
    // without fighting each other or emitting a warning-per-startup.
    public void Init(string rootPath)
    {
        lock (_lifecycleLock)
        {
            if (_watcher != null)
            {
                Logger.LogTrace(
                    $"Init no-op: AssetDatabase already running against '{_root}'. " +
                    $"Ignoring bootstrap request for '{rootPath}'.");
                return;
            }
        }

        // No watcher yet — this is genuinely the first init, do the real work.
        Reinitialize(rootPath, isExplicitRootChange: false);
    }

    // UpdatePath is an explicit, intentional root change (e.g. user switches
    // projects in the editor). It always re-roots, unless the currently active
    // root was itself set via an explicit call and the new target looks like a
    // less-specific/parent path — that case is still rejected and logged, since
    // it's most likely an accidental clobber rather than an intentional switch.
    public void UpdatePath(string rootPath) => Reinitialize(rootPath, isExplicitRootChange: true);

    private void Reinitialize(string rootPath, bool isExplicitRootChange)
    {
        var targetRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));

        lock (_lifecycleLock)
        {
            if (_watcher != null)
            {
                if (isExplicitRootChange &&
                    _root.EndsWith("Assets", StringComparison.OrdinalIgnoreCase) &&
                    !targetRoot.EndsWith("Assets", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogWarning(
                        $"AssetDatabase already initialized on explicit directory context: {_root}. " +
                        $"Intercepted and blocked lower-priority root clobber request targeting: {targetRoot}");
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
    }

    private void AttachFileWatcher()
    {
        // Caller must hold _lifecycleLock.
        var filter = OperatingSystem.IsLinux() ? "*" : "*.*";

        var watcher = new FileSystemWatcher(_root, filter)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite
                           | NotifyFilters.FileName
                           | NotifyFilters.DirectoryName
                           | NotifyFilters.CreationTime
                           | NotifyFilters.Size,
        };

        watcher.Changed += (_, e) => { Logger.LogTrace($"OS CHANGED event caught: {e.FullPath}"); UpdateAssetMetadata(e.FullPath); };
        watcher.Created += (_, e) => { Logger.LogTrace($"OS CREATED event caught: {e.FullPath}"); UpdateAssetMetadata(e.FullPath); };
        watcher.Deleted += (_, e) => { Logger.LogTrace($"OS DELETED event caught: {e.FullPath}"); OnDeleted(e.FullPath); };
        watcher.Renamed += (_, e) =>
        {
            Logger.LogTrace($"OS RENAMED event caught: {e.OldFullPath} -> {e.FullPath}");
            OnDeleted(e.OldFullPath);
            UpdateAssetMetadata(e.FullPath);
        };

        watcher.EnableRaisingEvents = true;
        _watcher = watcher;

        Logger.LogSuccess($"FileSystemWatcher successfully initialized and listening on: {_root}");
    }

    private void OnDeleted(string absolutePath)
    {
        var root = _root;
        var relative = Path.GetRelativePath(root, absolutePath);
        if (!_assets.TryRemove(relative, out _)) return;

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

        var root = _root;
        var relative = Path.GetRelativePath(root, absolutePath);
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
        // Caller must hold _lifecycleLock (or accept the collection may be
        // concurrently mutated by watcher events for a root that isn't fully
        // swapped in yet).
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

    // Returning by value instead of `ref readonly`: a ConcurrentDictionary
    // gives no stable internal storage to point a ref at, and a torn/dangling
    // ref into a concurrently-mutated collection is worse than a copy.
    public bool TryGetAsset(string relativePath, out AssetInfo info) =>
        _assets.TryGetValue(relativePath, out info);

    public AssetInfo GetAsset(string relativePath)
    {
        return _assets.TryGetValue(relativePath, out var info)
            ? info
            : throw new KeyNotFoundException($"No asset tracked for path: {relativePath}");
    }

    public string GetRootPath() => _root;
    public IEnumerable<AssetInfo> GetAllAssets() => _assets.Values;
}