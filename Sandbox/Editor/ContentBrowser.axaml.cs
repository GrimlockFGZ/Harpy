using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Engine.Core;
using HarpyEngine.Resources;
using HarpyEngine.Resources.Mnemosyne;

namespace HarpyEngine.Sandbox.Editor;

public partial class ContentBrowser : UserControl
{
    private string _rootDirectory = "";
    private string _currentDirectory = "";
    private AssetDatabase? _database;

    public ObservableCollection<AssetInfo> Assets { get; } = [];

    public ContentBrowser()
    {
        InitializeComponent();
        DataContext = this;

        var listBox = this.FindControl<ListBox>("AssetListBox");
        if (listBox != null)
        {
            listBox.DoubleTapped += OnAssetDoubleTapped;
        }

        Event<AssetUpdated>.Subscribe(OnAssetUpdated);
        Event<AssetRemoved>.Subscribe(OnAssetRemoved);
    }

    public void Initialize(AssetDatabase database, string rootDirectory)
    {
        _database = database;
        _rootDirectory = rootDirectory;
        _currentDirectory = rootDirectory;

        Logger.LogInfo($"UI Component bound to directory context: {rootDirectory}");
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (_database == null) return;

        Logger.LogTrace($"Running UI Refresh for active directory: {_currentDirectory}");
        Assets.Clear();

        try
        {
            if (Directory.Exists(_currentDirectory))
            {
                foreach (var dir in Directory.GetDirectories(_currentDirectory))
                {
                    var relative = Path.GetRelativePath(_rootDirectory, dir);
                    Assets.Add(new AssetInfo(relative, dir, AssetType.Folder));
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Directory iteration breakdown: {ex.Message}");
        }

        var currentRelativeDir = Path.GetRelativePath(_rootDirectory, _currentDirectory);
        if (currentRelativeDir is "." or "./") currentRelativeDir = ""; 

        var itemsInThisFolder = _database.GetAllAssets().Where(asset =>
        {
            var assetDir = Path.GetDirectoryName(asset.RelativePath) ?? "";
            return string.Equals(assetDir.Replace('\\', '/').TrimEnd('/'), currentRelativeDir.Replace('\\', '/').TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        }).ToList();

        Logger.LogTrace($"Database contains {itemsInThisFolder.Count} file(s) matching this folder view profile context.");

        foreach (var asset in itemsInThisFolder)
        {
            Assets.Add(asset);
        }

        UpdateBreadcrumbs();
    }

    private void OnAssetUpdated(AssetUpdated ev)
    {
        var assetDir = Path.GetDirectoryName(ev.Info.AbsolutePath) ?? "";
        
        var isDirectChildFile = string.Equals(assetDir.Replace('\\', '/').TrimEnd('/'), _currentDirectory.Replace('\\', '/').TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        var isDirectChildFolder = ev.Info.Type == AssetType.Folder && isDirectChildFile;

        Logger.LogTrace($"OnAssetUpdated intercepted: Path={ev.Info.AbsolutePath} | Type={ev.Info.Type} | IsTargetChild={isDirectChildFile || isDirectChildFolder}");

        if (!isDirectChildFile && !isDirectChildFolder) return;

        Logger.LogTrace("Dispatching UI refresh request payload to the UI Thread context loop...");
        Dispatcher.UIThread.Post(RefreshUI);
    }

    private void OnAssetRemoved(AssetRemoved ev)
    {
        if (_database == null) return;
        var fullPath = Path.Combine(_database.GetRootPath(), ev.RelativePath);
        var assetDir = Path.GetDirectoryName(fullPath) ?? "";

        var isDirectChildFile = string.Equals(assetDir.Replace('\\', '/').TrimEnd('/'), _currentDirectory.Replace('\\', '/').TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        var isOurCurrentFolderDeleted = string.Equals(fullPath.Replace('\\', '/').TrimEnd('/'), _currentDirectory.Replace('\\', '/').TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

        Logger.LogTrace($"OnAssetRemoved intercepted: RelPath={ev.RelativePath} | IsTargetChildOrActiveFolder={isDirectChildFile || isOurCurrentFolderDeleted}");

        if (!isDirectChildFile && !isOurCurrentFolderDeleted) return;

        Dispatcher.UIThread.Post(() =>
        {
            if (isOurCurrentFolderDeleted)
            {
                Logger.LogWarning("Target active view directory was deleted natively; falling back directly to root.");
                _currentDirectory = _rootDirectory;
            }
            RefreshUI();
        });
    }

    private void OnAssetDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (sender is not ListBox listBox || listBox.SelectedItem is not AssetInfo selectedAsset) return;
        if (selectedAsset.Type == AssetType.Folder)
        {
            Logger.LogTrace($"Diving downward into child folder directory context: {selectedAsset.AbsolutePath}");
            _currentDirectory = selectedAsset.AbsolutePath;
            RefreshUI();
        }
        else
        {
            Logger.LogInfo($"Executing open pipeline request anchor for asset target item: {selectedAsset.RelativePath}");
                
            LaunchAssetFile(selectedAsset);
        }
    }

private static void LaunchAssetFile(AssetInfo asset)
{
    if (!File.Exists(asset.AbsolutePath))
    {
        Logger.LogError($"Cannot execute pipeline. File missing on disk: {asset.AbsolutePath}");
        return;
    }

    try
    {
        var startInfo = new ProcessStartInfo();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            startInfo.FileName = "xdg-open";
            startInfo.Arguments = $"\"{asset.AbsolutePath}\"";
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            startInfo.FileName = asset.AbsolutePath;
            startInfo.UseShellExecute = true;
        }
        else // macOS
        {
            startInfo.FileName = "open";
            startInfo.Arguments = $"\"{asset.AbsolutePath}\"";
            startInfo.UseShellExecute = false;
        }

        Logger.LogTrace($"Dispatching OS execution handle: {startInfo.FileName} {startInfo.Arguments}");
        Process.Start(startInfo);
    }
    catch (Exception ex)
    {
        Logger.LogError($"Failed to spin up host application for {asset.RelativePath}: {ex.Message}");
    }
}

    private void UpdateBreadcrumbs()
    {
        var breadcrumbContainer = this.FindControl<StackPanel>("BreadcrumbContainer");
        if (breadcrumbContainer == null) return;

        breadcrumbContainer.Children.Clear();

        var rootSegment = new TextBlock { Text = "Assets", Classes = { "BreadcrumbSegment" } };
        rootSegment.Tapped += (_,_) => { _currentDirectory = _rootDirectory; RefreshUI(); };
        breadcrumbContainer.Children.Add(rootSegment);

        var relative = Path.GetRelativePath(_rootDirectory, _currentDirectory);
        if (relative == ".") return;

        var steps = relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
       var accumulatedPath = _rootDirectory;

        foreach (var step in steps)
        {
            accumulatedPath = Path.Combine(accumulatedPath, step);
            var capturedPath = accumulatedPath; 

            breadcrumbContainer.Children.Add(new TextBlock { Text = " › ", Classes = { "BreadcrumbSep" } });

            var segment = new TextBlock { Text = step, Classes = { "BreadcrumbSegment" } };
            segment.Tapped += (_, _) => { _currentDirectory = capturedPath; RefreshUI(); };
            breadcrumbContainer.Children.Add(segment);
        }
    }
}