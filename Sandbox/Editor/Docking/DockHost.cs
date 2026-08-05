using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace HarpyEngine.Sandbox.Editor.Docking;

/// <summary>
/// Root surface for the docking system. Owns the layout tree (<see cref="Root"/>) and coordinates
/// drag gestures started by <see cref="DockGroup"/> tab headers: dropping on the center of a group
/// tabs the item in; dropping on an edge splits that space; dropping outside the host pops the item
/// into its own floating window.
/// </summary>
public sealed class DockHost : Grid
{
    private const double EdgeFraction = 0.25;

    private readonly ContentControl _contentHost;
    private readonly Border _indicator;

    private DockItem? _draggedItem;
    private DockGroup? _dragSource;

    public IDockNode? Root
    {
        get;
        set
        {
            field = value;
            if (field is not null)
            {
                if (field.View.Parent is Panel oldPanel) oldPanel.Children.Remove(field.View);
                else if (field.View.Parent is Decorator dec) dec.Child = null;

                field.Parent = null;
                DockSplit.PropagateHost(field, this);
            }
            _contentHost.Content = field?.View;
        }
    }

    public DockHost()
    {
        _contentHost = new ContentControl();

        _indicator = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#7c5cfc"), 0.28),
            BorderBrush = Brush.Parse("#7c5cfc"),
            BorderThickness = new Thickness(2),
            IsVisible = false
        };

        var overlay = new Canvas { IsHitTestVisible = false };
        overlay.Children.Add(_indicator);

        Children.Add(_contentHost);
        Children.Add(overlay);
    }

    /// <summary>Adds an item as a new tab in the first available group (used to reopen a closed panel).</summary>
    public void AddToFirstGroup(DockItem item)
    {
        var group = EnumerateGroups(Root).FirstOrDefault();
        if (group is not null)
        {
            group.AddItem(item);
        }
        else
        {
            var newGroup = new DockGroup { Host = this };
            newGroup.AddItem(item);
            Root = newGroup;
        }
    }

    internal void BeginDrag(DockItem item, DockGroup source)
    {
        _draggedItem = item;
        _dragSource = source;
    }

    internal void UpdateDrag(Point pointInHost)
    {
        if (_draggedItem is null) return;

        var hovered = FindGroupAt(pointInHost);
        if (hovered is null)
        {
            _indicator.IsVisible = false;
            return;
        }

        var origin = hovered.TranslatePoint(new Point(0, 0), this) ?? default;
        var local = new Point(pointInHost.X - origin.X, pointInHost.Y - origin.Y);
        var zone = ComputeZone(local, hovered.Bounds.Size);

        PositionIndicator(origin, hovered.Bounds.Size, zone);
    }

    internal void CompleteDrag(Point pointInHost)
    {
        _indicator.IsVisible = false;

        var item = _draggedItem;
        var source = _dragSource;
        _draggedItem = null;
        _dragSource = null;

        if (item is null || source is null) return;

        var withinHost = pointInHost.X >= 0 && pointInHost.Y >= 0
                                            && pointInHost.X <= Bounds.Width && pointInHost.Y <= Bounds.Height;

        if (!withinHost)
        {
            if (ReferenceEquals(Root, source) && source.Items.Count == 1) return;
            
            var screenPoint = ToScreenPoint(pointInHost);
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                source.RemoveItem(item);
                var floating = new DockFloatingWindow(item, this, screenPoint);
                floating.Show();
            }, Avalonia.Threading.DispatcherPriority.Background);

        
            return;
        }

        var hovered = FindGroupAt(pointInHost);
        if (hovered is null) return;

        if (ReferenceEquals(hovered, source) && source.Items.Count == 1) return;

        var origin = hovered.TranslatePoint(new Point(0, 0), this) ?? default;
        var local = new Point(pointInHost.X - origin.X, pointInHost.Y - origin.Y);
        var zone = ComputeZone(local, hovered.Bounds.Size);

        EnqueueMutation(() =>
        {
            source.RemoveItem(item);

            if (zone == DockSide.Center)
            {
                hovered.AddItem(item);
            }
            else
            {
                SplitInto(hovered, item, zone);
            }
        });
    }
    private PixelPoint ToScreenPoint(Point pointInHost)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return new PixelPoint(0, 0); // Fallback safely instead of crashing

        try
        {
            var pointInTopLevel = this.TranslatePoint(pointInHost, topLevel) ?? pointInHost;
            return topLevel.PointToScreen(pointInTopLevel);
        }
        catch
        {
            return new PixelPoint((int)pointInHost.X, (int)pointInHost.Y);
        }
    }

    private void SplitInto(DockGroup target, DockItem item, DockSide side)
    {
        var oldParent = target.Parent;
        
        var newGroup = new DockGroup { Host = this };
        newGroup.AddItem(item);

        var orientation = side is DockSide.Left or DockSide.Right ? Orientation.Horizontal : Orientation.Vertical;
        var newGroupFirst = side is DockSide.Left or DockSide.Top;

        IDockNode first = newGroupFirst ? newGroup : target;
        IDockNode second = newGroupFirst ? target : newGroup;

        var split = new DockSplit(orientation, first, second) { Host = this };

        if (oldParent is null)
        {
            Root = split;
        }
        else
        {
            oldParent.ReplaceChild(target, split);
        }
    }

    private DockGroup? FindGroupAt(Point pointInHost)
    {
        DockGroup? found = null;

        foreach (var group in EnumerateGroups(Root))
        {
            var origin = group.TranslatePoint(new Point(0, 0), this);
            if (origin is null) continue;

            var size = group.Bounds.Size;
            if (pointInHost.X >= origin.Value.X && pointInHost.X <= origin.Value.X + size.Width &&
                pointInHost.Y >= origin.Value.Y && pointInHost.Y <= origin.Value.Y + size.Height)
            {
                found = group;
            }
        }

        return found;
    }

    private static DockSide ComputeZone(Point local, Size size)
    {
        var xf = size.Width <= 0 ? 0 : local.X / size.Width;
        var yf = size.Height <= 0 ? 0 : local.Y / size.Height;

        if (xf < EdgeFraction) return DockSide.Left;
        if (xf > 1 - EdgeFraction) return DockSide.Right;
        if (yf < EdgeFraction) return DockSide.Top;
        if (yf > 1 - EdgeFraction) return DockSide.Bottom;

        return DockSide.Center;
    }

    private void PositionIndicator(Point groupOrigin, Size groupSize, DockSide zone)
    {
        _indicator.IsVisible = true;

        double x = groupOrigin.X, y = groupOrigin.Y, w = groupSize.Width, h = groupSize.Height;

        switch (zone)
        {
            case DockSide.Left:
                w *= 0.5;
                break;
            case DockSide.Right:
                x += groupSize.Width * 0.5;
                w *= 0.5;
                break;
            case DockSide.Top:
                h *= 0.5;
                break;
            case DockSide.Bottom:
                y += groupSize.Height * 0.5;
                h *= 0.5;
                break;
            case DockSide.Center:
            default:
                break;
        }

        Canvas.SetLeft(_indicator, x);
        Canvas.SetTop(_indicator, y);
        _indicator.Width = w;
        _indicator.Height = h;
    }

    private static IEnumerable<DockGroup> EnumerateGroups(IDockNode? node)
    {
        while (true)
        {
            switch (node)
            {
                case null:
                    yield break;
                case DockGroup group:
                    yield return group;
                    break;
                case DockSplit split:
                    foreach (var g in EnumerateGroups(split.First)) yield return g;
                    node = split.Second;
                    continue;
            }

            break;
        }
    }

    private readonly Queue<System.Action> _pendingMutations = new();
    private bool _isMutating;

    internal void EnqueueMutation(System.Action mutation)
    {
        _pendingMutations.Enqueue(mutation);
        ProcessQueue();
    }

    private void ProcessQueue()
    {
        if (_isMutating) return;
        _isMutating = true;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            try
            {
                while (_pendingMutations.Count > 0)
                {
                    var action = _pendingMutations.Dequeue();
                    action();
                }
            }
            finally
            {
                _isMutating = false;
            
                // If new items were added while processing, drain them too
                if (_pendingMutations.Count > 0)
                {
                    ProcessQueue();
                }
            }
        }, Avalonia.Threading.DispatcherPriority.Normal);
    }
}