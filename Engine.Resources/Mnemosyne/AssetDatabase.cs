using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.IO.Enumeration;
using System.Runtime.InteropServices;

namespace HarpyEngine.Resources.Mnemosyne;

using Engine.Core;

public record struct AssetUpdated(AssetInfo Info) : IEvent;
public record struct AssetRemoved(string RelativePath) : IEvent;

public class AssetDatabase
{

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
    };

    private static readonly FrozenDictionary<string, AssetType> FastMap = 
        ExtensionMap.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentQueue<string> _dirtyFiles = new();
    private readonly Dictionary<string, AssetInfo> _assets = new(2048, StringComparer.OrdinalIgnoreCase);
    private string _root = "";

    public void Init(string rootPath)
    {
        _root = rootPath;
        ImportFolder(rootPath); 
        
        var watcher = new FileSystemWatcher(rootPath, "*.*")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        watcher.Changed += (s, e) => UpdateAssetMetadata(e.FullPath);
        watcher.Created += (s, e) => UpdateAssetMetadata(e.FullPath);
        watcher.Deleted += (s, e) => OnDeleted(e.FullPath);
    }

    private void OnDeleted(string absolutePath)
    {
        var relative = Path.GetRelativePath(_root, absolutePath);
        if (_assets.Remove(relative))
        {
            Event<AssetRemoved>.Invoke(new AssetRemoved(relative));
        }
    }

    public readonly record struct FileResult(bool Success, string? Path);

    public FileResult TryGetNextDirtyFile()
    {
        if (_dirtyFiles.TryDequeue(out var path))
        {
            return new FileResult(true, path);
        }
        return new FileResult(false, null);
    }
    

    public void UpdateAssetMetadata(string absolutePath)
    {
        var relative = Path.GetRelativePath(_root, absolutePath);
        var ext = Path.GetExtension(absolutePath.AsSpan());

        if (!TryGetAssetType(ext, out var type)) return;
        var info = new AssetInfo(relative, absolutePath, type, File.GetLastWriteTime(absolutePath));
        _assets[relative] = info;
        _dirtyFiles.Enqueue(absolutePath);
        Event<AssetUpdated>.Invoke(new AssetUpdated(info));
    }
    
    private static bool TryGetAssetType(ReadOnlySpan<char> extension, out AssetType type)
    {
        var lookup = FastMap.GetAlternateLookup<ReadOnlySpan<char>>();
        return lookup.TryGetValue(extension, out type);
    }

    public void ImportFolder(string rootPath)
    {
        var options = new EnumerationOptions { RecurseSubdirectories = true };
        var lookup = FastMap.GetAlternateLookup<ReadOnlySpan<char>>();
        int rootLen = rootPath.EndsWith(Path.DirectorySeparatorChar) ? rootPath.Length : rootPath.Length + 1;

        var enumerable = new FileSystemEnumerable<bool>(
            rootPath,
            (ref entry) =>
            {
                var ext = Path.GetExtension(entry.FileName);
                if (!lookup.TryGetValue(ext, out var type)) return true;
                
                var fullPath = entry.ToFullPath();
                var relative = fullPath[rootLen..];
                
                var lastWrite = entry.LastWriteTimeUtc.DateTime;
                var info = new AssetInfo(relative, fullPath, type, lastWrite);
                
                _assets[relative] = info;
                return true;
            },
            options);

        foreach (var _ in enumerable) { }
    }

    public void RequeueDirtyFile(string path)
    {
        _dirtyFiles.Enqueue(path);
    }

    public ref readonly AssetInfo GetAsset(string relativePath)
    {
        return ref CollectionsMarshal.GetValueRefOrNullRef(_assets, relativePath);
    }

    public IEnumerable<AssetInfo> GetAllAssets() => _assets.Values;
}