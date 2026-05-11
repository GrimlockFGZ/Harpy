using Avalonia;
using Avalonia.Controls;
using System.Collections;

namespace HarpyEngine.Sandbox.Editor;

public partial class ContentBrowser : UserControl
{
    // Define the property for the data
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<ContentBrowser, IEnumerable?>(nameof(ItemsSource));

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ContentBrowser()
    {
        InitializeComponent();
    }
}