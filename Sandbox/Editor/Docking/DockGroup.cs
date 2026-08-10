using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace HarpyEngine.Sandbox.Editor.Docking;

/// <summary>
/// A leaf node in the docking tree: a tab strip plus the content of the active tab.
/// Dragging a tab header far enough starts a dock/undock/float gesture handled by the owning
/// <see cref="DockHost"/>.
/// </summary>
public sealed class DockGroup : Grid, IDockNode
{
    private const double DragThreshold = 5.0;

    public DockSplit? Parent { get; set; }
    public DockHost? Host { get; set; }
    public Control View => this;

    public List<DockItem> Items { get; } = [];
    public int ActiveIndex { get; private set; }

    private readonly StackPanel _tabStrip;
    private readonly ContentControl _content;

    public DockGroup()
    {
        var headerRow = new RowDefinition { Height = GridLength.Auto };
        var contentRow = new RowDefinition(1, GridUnitType.Star);
        RowDefinitions.Add(headerRow);
        RowDefinitions.Add(contentRow);

        _tabStrip = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        var tabStripBorder = new Border
        {
            Background = Brush.Parse("#161616"),
            BorderBrush = Brush.Parse("#1e1e1e"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(4, 0),
            Child = _tabStrip
        };
        SetRow(tabStripBorder, 0);

        _content = new ContentControl { Background = Brush.Parse("#141414") };
        SetRow(_content, 1);

        Children.Add(tabStripBorder);
        Children.Add(_content);
    }

    public void AddItem(DockItem item, int? index = null)
    {
        item.Group = this;

        if (index is { } i && i >= 0 && i <= Items.Count)
        {
            Items.Insert(i, item);
        }
        else
        {
            Items.Add(item);
        }

        ActiveIndex = Items.IndexOf(item);
        RebuildTabs();
        UpdateContent();
    }

    public void RemoveItem(DockItem item)
    {
        var index = Items.IndexOf(item);
        if (index < 0) return;

        Items.RemoveAt(index);
        item.Group = null;

        if (Items.Count == 0)
        {
            _content.Content = null;
            RebuildTabs();
            Parent?.Collapse(this);
            return;
        }

        ActiveIndex = Math.Min(ActiveIndex, Items.Count - 1);
        RebuildTabs();
        UpdateContent();
    }

    public void SelectItem(DockItem item)
    {
        var index = Items.IndexOf(item);
        if (index < 0 || index == ActiveIndex) return;

        ActiveIndex = index;
        RebuildTabs();
        UpdateContent();
    }

    private void UpdateContent()
    {
        _content.Content = Items.Count > 0 ? Items[ActiveIndex].Content : null;
    }

    private void RebuildTabs()
    {
        _tabStrip.Children.Clear();
        for (var i = 0; i < Items.Count; i++)
        {
            _tabStrip.Children.Add(BuildTabHeader(Items[i], i == ActiveIndex));
        }
    }

    private Border BuildTabHeader(DockItem item, bool isActive)
    {
        var header = new Border
        {
            Padding = new Thickness(10, 6),
            Background = isActive ? new SolidColorBrush(Color.Parse("#1a1630")) : Brushes.Transparent,
            BorderBrush = isActive ? item.Accent : Brushes.Transparent,
            BorderThickness = new Thickness(0, 0, 0, 2),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        row.Children.Add(new Rectangle
        {
            Width = 6,
            Height = 6,
            Fill = item.Accent,
            RadiusX = 1,
            RadiusY = 1,
            VerticalAlignment = VerticalAlignment.Center
        });
        row.Children.Add(new TextBlock
        {
            Text = item.Title,
            FontSize = 11,
            Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.Parse("#999999")),
            VerticalAlignment = VerticalAlignment.Center
        });

        if (item.CanClose)
        {
            var closeButton = new TextBlock
            {
                Text = "\u00d7",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.Parse("#666666")),
                Margin = new Thickness(6, 0, 0, 0),
                Cursor = new Cursor(StandardCursorType.Hand),
                VerticalAlignment = VerticalAlignment.Center
            };
            closeButton.PointerPressed += (_, e) =>
            {
                e.Handled = true;
                RemoveItem(item);
            };
            row.Children.Add(closeButton);
        }

        header.Child = row;

        Point? dragStart = null;
        var dragging = false;

        header.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(header).Properties.IsLeftButtonPressed) return;

            // Selection is deferred to PointerReleased: selecting here would call RebuildTabs()
            // immediately, tearing down this very header mid-gesture and breaking its capture.
            dragStart = e.GetPosition(this);
            dragging = false;
            e.Pointer.Capture(header);
        };

        header.PointerMoved += (_, e) =>
        {
            if (dragStart is null || Host is null) return;

            var current = e.GetPosition(this);
            if (!dragging)
            {
                var dx = current.X - dragStart.Value.X;
                var dy = current.Y - dragStart.Value.Y;
                if (Math.Sqrt(dx * dx + dy * dy) < DragThreshold) return;

                dragging = true;
                Host.BeginDrag(item, this);
            }

            Host.UpdateDrag(e.GetPosition(Host));
        };

        header.PointerReleased += (_, e) =>
        {
            e.Pointer.Capture(null);

            if (dragging && Host is not null)
            {
                Host.CompleteDrag(e.GetPosition(Host));
            }
            else if (!dragging)
            {
                // A plain click with no drag: select this tab now that it's safe to rebuild.
                SelectItem(item);
            }

            dragStart = null;
            dragging = false;
        };

        return header;
    }
}
