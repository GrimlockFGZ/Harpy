using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using Engine;
using Engine.Core;
using HarpyEngine.Resources;
using HarpyEngine.Resources.Mnemosyne;
using HarpyEngine.Sandbox.Editor.Models;

namespace HarpyEngine.Sandbox.Editor
{
    public partial class EditorWindow : Window
    {
        private readonly AssetDatabase _db;
        private readonly Registry _registry = new();
        private readonly Dictionary<string, AssetInfo> _assetCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly HierarchyViewModel _hierarchyViewModel = new();
        private readonly InspectorViewModel _inspectorViewModel = new();

        public ObservableCollection<AssetInfo> UiAssets { get; } = new();

        public EditorWindow()
        {
            InitializeComponent();
            
            DataContext = this;

            Title = "Harpy Engine";
            WindowState = WindowState.FullScreen;
            
            var assetUri = new Uri("avares://Sandbox/Assets/Favicon.ico");
            Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(assetUri));
            
         

            _hierarchyViewModel.SetRegistry(_registry);
            _hierarchyViewModel.SelectedEntryChanged += OnHierarchySelectionChanged;
            _inspectorViewModel.ApplyRequested += OnApplyRequested;
            _inspectorViewModel.AddTransformRequested += OnAddTransformRequested;
            _inspectorViewModel.RemoveTransformRequested += OnRemoveTransformRequested;
            _registry.EntityCreated += _ => UpdateViewportTriangleCount();
            _registry.EntityDestroyed += _ => UpdateViewportTriangleCount();

            HierarchyPanel.DataContext = _hierarchyViewModel;
            InspectorPanel.DataContext = _inspectorViewModel;

            SeedScene();

            _db = new AssetDatabase();
            var assetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            
            _db.AssetUpdated += OnAssetUpdated;
            _db.AssetRemoved += OnAssetRemoved;

            _db.Init(assetPath);

            foreach (var asset in _db.GetAllAssets())
            {
                UiAssets.Add(asset);
                _assetCache[asset.RelativePath] = asset;
            }
        }

        private void SeedScene()
        {
            UpdateViewportTriangleCount();
        }

        private void UpdateViewportTriangleCount()
        {
            ViewportPanel.TriangleInstanceCount = _registry.GetAllEntities().Count();
        }

        private void OnHierarchySelectionChanged(HierarchyEntry? entry)
        {
            if (entry is null)
            {
                _inspectorViewModel.ClearSelection();
                return;
            }

            if (_registry.HasComponent<Transform>(entry.Entity))
            {
                var transform = _registry.GetComponent<Transform>(entry.Entity);
                _inspectorViewModel.SetSelection(entry, true, transform);
            }
            else
            {
                _inspectorViewModel.SetSelection(entry, false, null);
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
            _inspectorViewModel.SetSelection(entry, true, _registry.GetComponent<Transform>(entry.Entity));
            _inspectorViewModel.SetStatus("Transform added.");
        }

        private void OnRemoveTransformRequested(HierarchyEntry entry)
        {
            if (_registry.RemoveComponent<Transform>(entry.Entity))
            {
                _inspectorViewModel.SetSelection(entry, false, null);
                _inspectorViewModel.SetStatus("Transform removed.");
            }
            else
            {
                _inspectorViewModel.SetStatus("Entity has no Transform to remove.");
            }
        }

        private void OnAssetUpdated(AssetInfo info)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_assetCache.TryGetValue(info.RelativePath, out var existing))
                {
                    var index = UiAssets.IndexOf(existing);
                    if (index == -1) return;
                    UiAssets[index] = info;
                    _assetCache[info.RelativePath] = info;
                }
                else
                {
                    UiAssets.Add(info);
                    _assetCache[info.RelativePath] = info;
                }
            });
        }

        private void OnAssetRemoved(string relativePath)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_assetCache.Remove(relativePath, out var existing))
                {
                    UiAssets.Remove(existing);
                }
            });
        }
    }
}