using Engine.Exceptions;
using HarpyEngine.Exceptions;

namespace HarpyEngine.Resources.Mnemosyne;

using Engine.Core;

public record struct ReloadRequested(object Resource) : IEvent;

public static class ResourceManager
{
    private static AssetDatabase _db = AssetDatabase.Instance;
    public static event Func<string, string, object>? OnShaderRequest;

    private static readonly Dictionary<string, object> _resources = new();
    private static readonly Dictionary<string, List<object>> _fileDependencies = new();

    public static void Init(AssetDatabase db) => _db = db;

    public static void LoadShader(string name, string vPath, string fPath)
    {
        // We assume the delegate handles the GL compilation logic
        var shader = OnShaderRequest?.Invoke(vPath, fPath);

        if (shader != null)
        {
            _resources[name] = shader;
            RegisterDependency(vPath, shader);
            RegisterDependency(fPath, shader);
        }
        else
        {
            throw new RenderingException(
                $"Shader '{name}' failed to load. " +
                $"Check if the files exist at '{vPath}' and '{fPath}', " +
                "and verify that the GLSL syntax is correct.");
        }
    }

    public static void CheckForReloads()
    {
        while (_db.TryGetNextDirtyFile() is { Success: true, Path: var path })
        {
            if (path == null) continue;

            var lookupPath = Path.GetFullPath(path).ToLowerInvariant();

            if (!_fileDependencies.TryGetValue(lookupPath, out var assets)) continue;
            foreach (var asset in assets) 
            {
                Event<ReloadRequested>.Invoke(new ReloadRequested(asset));
            }
        }
    }

    private static void RegisterDependency(string path, object asset)
    {
        var normalizedPath = Path.GetFullPath(path).ToLowerInvariant();
        if (!_fileDependencies.ContainsKey(normalizedPath)) _fileDependencies[normalizedPath] = new();
        _fileDependencies[normalizedPath].Add(asset);
    }

    public static T Get<T>(string name) 
    {
        if (!_resources.TryGetValue(name, out var resource)) 
        {
            throw new ResourceNotFoundException(name);
        }
        return (T)resource;
    }
}