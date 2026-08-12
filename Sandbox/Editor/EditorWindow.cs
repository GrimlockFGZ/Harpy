using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Engine;
using Engine.Core;
using HarpyEngine.Rendering.Voxel;
using HarpyEngine.Resources.Mnemosyne;
using HarpyEngine.Sandbox.Editor.Docking;
using HarpyEngine.Sandbox.Editor.Models;
using Transform = Engine.Transform;

namespace HarpyEngine.Sandbox.Editor;

public partial class EditorWindow : Window
{
    private readonly Registry _registry = new();
    private readonly HierarchyViewModel _hierarchyViewModel = new();
    private readonly InspectorViewModel _inspectorViewModel = new();
    private readonly ConsoleViewModel _consoleViewModel = new();
    private readonly List<IDisposable> _busSubscriptions = [];

    private readonly Hierarchy _hierarchyPanel = new();
    private readonly HarpyViewport _viewportPanel = new();
    private readonly ContentBrowser _contentBrowserPanel = new();
    private readonly Inspector _inspectorPanel = new();
    private readonly ConsolePanel _consolePanel = new();

    private DockItem _hierarchyItem = null!;
    private DockItem _inspectorItem = null!;
    private DockItem _contentBrowserItem = null!;
    private DockItem _consoleItem = null!;

    public EditorWindow()
    {
        InitializeComponent();

        DataContext = this;

        Title = "Harpy Engine";
        WindowState = WindowState.Maximized;
        Width = 1920;
        Height = 1080;

        var assetUri = new Uri("avares://Sandbox/Assets/Favicon.ico");
        Icon = new WindowIcon(AssetLoader.Open(assetUri));

        _hierarchyViewModel.SetRegistry(_registry);

        _busSubscriptions.Add(Event<SelectionChanged>.Subscribe(evt => OnHierarchySelectionChanged(evt.Entry)));
        _busSubscriptions.Add(Event<ApplyRequestedEvent>.Subscribe(evt => OnApplyRequested(evt.Entry)));
        _busSubscriptions.Add(Event<AddTransformRequestedEvent>.Subscribe(evt => OnAddTransformRequested(evt.Entry)));
        _busSubscriptions.Add(Event<RemoveTransformRequestedEvent>.Subscribe(evt => OnRemoveTransformRequested(evt.Entry)));
        _busSubscriptions.Add(Event<AddVoxelBlockRequestedEvent>.Subscribe(evt => OnAddVoxelBlockRequested(evt.Entry)));
        _busSubscriptions.Add(Event<RemoveVoxelBlockRequestedEvent>.Subscribe(evt => OnRemoveVoxelBlockRequested(evt.Entry)));
        _busSubscriptions.Add(Event<EntityCreated>.Subscribe(_ => UpdateViewportTriangleCount()));
        _busSubscriptions.Add(Event<EntityDestroyed>.Subscribe(_ => UpdateViewportTriangleCount()));

        _hierarchyPanel.DataContext = _hierarchyViewModel;
        _inspectorPanel.DataContext = _inspectorViewModel;
        _consolePanel.DataContext = _consoleViewModel;

        _viewportPanel.SetRegistry(_registry);

        BuildDockLayout();

        SeedScene();

        var db = AssetDatabase.Instance;
        var assetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
        db.Init(assetPath);

        _contentBrowserPanel.Initialize(db, assetPath);

        EngineLog.Info("Editor window ready.", "Editor");
    }

    private void BuildDockLayout()
    {
        _hierarchyItem = new DockItem("hierarchy", "HIERARCHY", Brush.Parse("#7c5cfc"), _hierarchyPanel, DockSide.Left);
        var viewportItem = new DockItem("viewport", "VIEWPORT", Brush.Parse("#5bc0de"), _viewportPanel) { CanClose = false };
        _contentBrowserItem = new DockItem("content-browser", "CONTENT BROWSER", Brush.Parse("#f39c12"), _contentBrowserPanel, DockSide.Bottom);
        _consoleItem = new DockItem("console", "CONSOLE", Brush.Parse("#1ca880"), _consolePanel, DockSide.Bottom);
        _inspectorItem = new DockItem("inspector", "INSPECTOR", Brush.Parse("#a48bff"), _inspectorPanel, DockSide.Right);

        var viewportGroup = new DockGroup();
        viewportGroup.AddItem(viewportItem);

        var bottomGroup = new DockGroup();
        bottomGroup.AddItem(_contentBrowserItem);
        bottomGroup.AddItem(_consoleItem);

        var centerSplit = new DockSplit(Orientation.Vertical, viewportGroup, bottomGroup, firstRatio: 3, secondRatio: 1);

        var hierarchyGroup = new DockGroup();
        hierarchyGroup.AddItem(_hierarchyItem);

        var leftSplit = new DockSplit(Orientation.Horizontal, hierarchyGroup, centerSplit, firstRatio: 1, secondRatio: 4);

        var inspectorGroup = new DockGroup();
        inspectorGroup.AddItem(_inspectorItem);

        var rootSplit = new DockSplit(Orientation.Horizontal, leftSplit, inspectorGroup, firstRatio: 5, secondRatio: 1);

        RootDock.Root = rootSplit;
    }

    private void OnReopenHierarchyClicked(object? sender, RoutedEventArgs e) => ReopenIfClosed(_hierarchyItem);
    private void OnReopenInspectorClicked(object? sender, RoutedEventArgs e) => ReopenIfClosed(_inspectorItem);
    private void OnReopenContentBrowserClicked(object? sender, RoutedEventArgs e) => ReopenIfClosed(_contentBrowserItem);
    private void OnReopenConsoleClicked(object? sender, RoutedEventArgs e) => ReopenIfClosed(_consoleItem);

    private void ReopenIfClosed(DockItem item)
    {
        if (item.Group is not null) return; // already open, either docked or floating
        RootDock.AddToFirstGroup(item);
    }

    private void SeedScene()
    {
        // Spawn one entity per solid block type, laid out in a row along X
        var solidTypes = Enum.GetValues<BlockType>().Where(t => t != BlockType.Air).ToArray();
        for (var i = 0; i < solidTypes.Length; i++)
        {
            var entity = _registry.CreateEntity();
            _registry.AddComponent(entity, new Transform(new Vector3(i * 2f, 0f, 0f)));
            _registry.AddComponent(entity, new VoxelBlock(solidTypes[i]));
        }

        UpdateViewportTriangleCount();
    }

    private void UpdateViewportTriangleCount()
    {
        _viewportPanel.TriangleInstanceCount = _registry.GetAllEntities().Count();
    }

    private void OnHierarchySelectionChanged(HierarchyEntry? entry)
    {
        if (entry is null)
        {
            _inspectorViewModel.ClearSelection();
            return;
        }

        var hasTransform = _registry.HasComponent<Transform>(entry.Entity);
        Transform? transform = hasTransform ? _registry.GetComponent<Transform>(entry.Entity) : null;

        var hasVoxelBlock = _registry.HasComponent<VoxelBlock>(entry.Entity);
        var voxelBlockType = hasVoxelBlock ? _registry.GetComponent<VoxelBlock>(entry.Entity).Type : BlockType.Air;

        _inspectorViewModel.SetSelection(entry, hasTransform, transform, hasVoxelBlock, voxelBlockType);
    }

    private void OnAddVoxelBlockRequested(HierarchyEntry entry)
    {
        if (_registry.HasComponent<VoxelBlock>(entry.Entity))
        {
            _inspectorViewModel.SetStatus("Entity already has a VoxelBlock.");
            return;
        }

        _registry.AddComponent(entry.Entity, new VoxelBlock(BlockType.Stone));
        OnHierarchySelectionChanged(entry);
        _inspectorViewModel.SetStatus("VoxelBlock added.");
    }

    private void OnRemoveVoxelBlockRequested(HierarchyEntry entry)
    {
        if (_registry.RemoveComponent<VoxelBlock>(entry.Entity))
        {
            OnHierarchySelectionChanged(entry);
            _inspectorViewModel.SetStatus("VoxelBlock removed.");
        }
        else
        {
            _inspectorViewModel.SetStatus("Entity has no VoxelBlock to remove.");
        }
    }

    private void OnApplyRequested(HierarchyEntry entry)
    {
        entry.Name = string.IsNullOrWhiteSpace(_inspectorViewModel.SelectedEntityName)
            ? $"Entity {entry.Entity.Id}"
            : _inspectorViewModel.SelectedEntityName.Trim();

        if (!_inspectorViewModel.TryGetTransformValues(out var position, out var scale))
        {
            _inspectorViewModel.SetStatus("Invalid numeric values in Transform fields.");
            return;
        }

        if (_registry.HasComponent<Transform>(entry.Entity))
        {
            ref var transform = ref _registry.GetComponent<Transform>(entry.Entity);
            transform = transform.WithPosition(position).WithScale(scale);
            _inspectorViewModel.SetStatus("Transform updated.");
        }
        else
        {
            _inspectorViewModel.SetStatus("Entity has no Transform. Add it first.");
        }
    }

    private void OnAddTransformRequested(HierarchyEntry entry)
    {
        if (_registry.HasComponent<Transform>(entry.Entity))
        {
            _inspectorViewModel.SetStatus("Entity already has a Transform.");
            return;
        }

        if (!_inspectorViewModel.TryGetTransformValues(out var position, out var scale))
        {
            _inspectorViewModel.SetStatus("Invalid numeric values in Transform fields.");
            return;
        }

        _registry.AddComponent(entry.Entity, new Transform(position, Quaternion.Identity, scale));
        OnHierarchySelectionChanged(entry);
        _inspectorViewModel.SetStatus("Transform added.");
    }

    private void OnRemoveTransformRequested(HierarchyEntry entry)
    {
        if (_registry.RemoveComponent<Transform>(entry.Entity))
        {
            OnHierarchySelectionChanged(entry);
            _inspectorViewModel.SetStatus("Transform removed.");
        }
        else
        {
            _inspectorViewModel.SetStatus("Entity has no Transform to remove.");
        }
    }

    private void OnProjectSettingsClicked(object? sender, RoutedEventArgs e)
    {
        var dialog = new ProjectSettingsDialog();
        dialog.ShowDialog(this);
    }

    private void OnNewProjectClicked(object? sender, RoutedEventArgs e)
    {
        var dialog = new NewProjectDialog();
        dialog.Closed += OnNewDialogClosed;
        dialog.ShowDialog(this);
    }
    private void OnNewDialogClosed(object? sender, EventArgs e)
    {
        if (sender is not NewProjectDialog dialog || dialog.ProjectPath == null) return;
            
        AssetDatabase.Instance.UpdatePath(dialog.ProjectPath);
        _contentBrowserPanel.Initialize(AssetDatabase.Instance,dialog.ProjectPath);
        var projectName = Path.GetFileName(Path.TrimEndingDirectorySeparator(dialog.ProjectPath));
        Title = $"Harpy Engine - {projectName}";
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        foreach (var subscription in _busSubscriptions)
            subscription.Dispose();

        _busSubscriptions.Clear();
    }
}