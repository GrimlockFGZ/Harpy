using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace HarpyEngine.Sandbox.Editor.Docking;

/// <summary>
/// A standalone window hosting a single detached dock item. Provides a "Dock" button to send
/// the panel back into the main editor's layout.
/// </summary>
public sealed class DockFloatingWindow : Window
{
    private readonly DockItem _item;
    private readonly DockHost _mainHost;
    private readonly RowDefinition _autoLenghtRowDefinition = new() { Height = GridLength.Auto };
    private readonly RowDefinition _starRowDefinition = new()
    { 
        Height = new GridLength(1, GridUnitType.Star) 
    };
    
    private readonly DockGroup _group = new();

    public DockFloatingWindow(DockItem item, DockHost mainHost, PixelPoint position)
    {
        Title = item.Title;
        Width = 1000;
        Height = 1000;
        Background = Brush.Parse("#0f0f0f");
        Position = new PixelPoint(position.X - 20, position.Y - 12);

        var root = new Grid();
        root.RowDefinitions.Add(_autoLenghtRowDefinition);
        root.RowDefinitions.Add(_starRowDefinition);

        var toolbar = new Border
        {
            Background = Brush.Parse("#161616"),
            BorderBrush = Brush.Parse("#1e1e1e"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(8, 5)
        };

        var dockButton = new Button
        {
            Content = "\u2913 Dock back to editor",
            FontSize = 11,
            Padding = new Thickness(8, 4),
            Background = Brush.Parse("#1e1640"),
            Foreground = Brush.Parse("#a48bff"),
            BorderBrush = Brush.Parse("#7c5cfc44"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3)
        };



        dockButton.Click += (sender, e) => DoDockBack(item, mainHost, _group);

        toolbar.Child = dockButton;
        Grid.SetRow(toolbar, 0);

        _group.AddItem(item);
        Grid.SetRow(_group, 1);

        root.Children.Add(toolbar);
        root.Children.Add(_group);
        Content = root;
    }

    private void DoDockBack(DockItem item, DockHost mainHost, DockGroup group)
    {
        group.RemoveItem(item);
        mainHost.AddToFirstGroup(item);
        Close();
    }
}