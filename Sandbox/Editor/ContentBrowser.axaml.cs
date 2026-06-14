using Avalonia.Controls;
using System.Collections.ObjectModel;
using HarpyEngine.Resources;

namespace HarpyEngine.Sandbox.Editor;

public partial class ContentBrowser : UserControl
{
    public ObservableCollection<AssetInfo> Assets { get; } = new();

    public ContentBrowser()
    {
        InitializeComponent();
        DataContext = this;
    }
}