using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using HarpyEngine.Resources;
using HarpyEngine.Resources.Mnemosyne;

namespace HarpyEngine.Sandbox.Editor
{
    public partial class EditorWindow : Window
    {
        private readonly AssetDatabase _db;
        private readonly Dictionary<string, AssetInfo> _assetCache = new(StringComparer.OrdinalIgnoreCase);

        public ObservableCollection<AssetInfo> UiAssets { get; } = new();

        public EditorWindow()
        {
            InitializeComponent();
            
            DataContext = this;

            Title = "Harpy Engine";
            WindowState = WindowState.Maximized;
            
            var assetUri = new Uri("avares://Sandbox/Assets/Favicon.ico");
            Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(assetUri));
            
         

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