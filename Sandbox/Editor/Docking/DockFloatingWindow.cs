using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace HarpyEngine.Sandbox.Editor.Docking;

/// <summary>
/// A standalone window hosting a single detached dock item. Provides a "Dock" button to send
/// the panel back into the main editor's layout.
/// </summary>
public sealed class DockFloatingWindow : Window
{
    public DockFloatingWindow(DockItem item, DockHost mainHost, PixelPoint position)
    {
        Title = item.Title;
        Width = 1000;
        Height = 1000;
        Background = Brush.Parse("#0f0f0f");
        Position = new PixelPoint(position.X - 20, position.Y - 12);

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));

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
        var group = new DockGroup();

        dockButton.Click += (_, _) =>
        {
            group.RemoveItem(item);
            mainHost.AddToFirstGroup(item);
            Close();
        };
        toolbar.Child = dockButton;
        Grid.SetRow(toolbar, 0);
        
        group.AddItem(item);
        Grid.SetRow(group, 1);

        root.Children.Add(toolbar);
        root.Children.Add(group);
        Content = root;
    }
}