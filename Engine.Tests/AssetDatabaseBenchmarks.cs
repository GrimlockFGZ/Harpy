using BenchmarkDotNet.Attributes;
using HarpyEngine.Resources.Mnemosyne;

namespace Engine.Tests;

[MemoryDiagnoser] // Tracks RAM usage and GC collections
public class AssetDatabaseBenchmarks
{
    private AssetDatabase _db;
    private string _tempPath;
    private string[] _testFiles;

    // This runs once before the benchmarks start
    [GlobalSetup]
    public void Setup()
    {
        _db = new AssetDatabase();
        _tempPath = Path.Combine(Path.GetTempPath(), "HarpyBenchmark");
        
        if (Directory.Exists(_tempPath)) Directory.Delete(_tempPath, true);
        Directory.CreateDirectory(_tempPath);

        // Create 100 dummy files to scan
        for (int i = 0; i < 100; i++)
        {
            File.WriteAllText(Path.Combine(_tempPath, $"test_asset_{i}.png"), "dummy data");
        }
        
        _db.Init(_tempPath);
        _testFiles = Directory.GetFiles(_tempPath);
    }

    [Benchmark]
    public void BenchmarkImportFolder()
    {
        _db.ImportFolder(_tempPath);
    }

    [Benchmark]
    public void BenchmarkGetAsset()
    {
        // Tests the lookup speed for a relative path
        _db.GetAsset("test_asset_50.png");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempPath)) Directory.Delete(_tempPath, true);
    }
}