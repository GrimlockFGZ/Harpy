using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace HarpyEngine.Sandbox.Editor.Docking;

/// <summary>
/// A branch node in the docking tree: two child nodes divided by a draggable <see cref="GridSplitter"/>.
/// </summary>
public sealed class DockSplit : Grid, IDockNode
{
    public DockSplit? Parent { get; set; }
    public DockHost? Host { get; set; }
    public Control View => this;

    public Orientation Orientation { get; }
    public IDockNode First { get; private set; }
    public IDockNode Second { get; private set; }

    private readonly GridSplitter _splitter;

    public DockSplit(Orientation orientation, IDockNode first, IDockNode second, double firstRatio = 1, double secondRatio = 1)
    {
        Orientation = orientation;
        First = first;
        Second = second;
        first.Parent = this;
        second.Parent = this;

        if (orientation == Orientation.Horizontal)
        {
            ColumnDefinitions.Add(new ColumnDefinition(firstRatio, GridUnitType.Star));
            ColumnDefinitions.Add(new ColumnDefinition(4, GridUnitType.Pixel));
            ColumnDefinitions.Add(new ColumnDefinition(secondRatio, GridUnitType.Star));

            _splitter = new GridSplitter
            {
                Width = 4,
                Background = Brush.Parse("#0a0a0a"),
                ResizeDirection = GridResizeDirection.Columns,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        }
        else
        {
            RowDefinitions.Add(new RowDefinition(firstRatio, GridUnitType.Star));
            RowDefinitions.Add(new RowDefinition(4, GridUnitType.Pixel));
            RowDefinitions.Add(new RowDefinition(secondRatio, GridUnitType.Star));

            _splitter = new GridSplitter
            {
                Height = 4,
                Background = Brush.Parse("#0a0a0a"),
                ResizeDirection = GridResizeDirection.Rows,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        }

        RebuildVisual();
    }

    /// <summary>Swaps one of this split's children out for a different node (used when splitting/collapsing).</summary>
    internal void ReplaceChild(IDockNode oldChild, IDockNode newChild)
    {
        Control oldView;
        if (ReferenceEquals(First, oldChild)) 
        { 
            oldView = First.View; 
            First = newChild; 
        }
        else if (ReferenceEquals(Second, oldChild)) 
        { 
            oldView = Second.View; 
            Second = newChild; 
        }
        else 
        {
            return;
        }

        newChild.Parent = this;
        PropagateHost(newChild, Host);

        // Ensure the new child's view is detached from any previous parent container first
        if (newChild.View.Parent is Panel oldParentPanel)
        {
            oldParentPanel.Children.Remove(newChild.View);
        }
        else if (newChild.View.Parent is Decorator decorator)
        {
            decorator.Child = null;
        }

        // Safely remove the old view and insert the new view without breaking index maps
        int index = Children.IndexOf(oldView);
        if (index >= 0)
        {
            Children.RemoveAt(index);
    
            if (Orientation == Orientation.Horizontal) 
            {
                SetColumn(newChild.View, index == 0 ? 0 : 2);
            }
            else 
            {
                SetRow(newChild.View, index == 0 ? 0 : 2);
            }

            Children.Insert(index, newChild.View);
        }
        else
        {
            RebuildVisual();
        }
    }
    
    private static readonly Stack<IDockNode> _propagateStack = new();
    internal static void PropagateHost(IDockNode? node, DockHost? host)
    {
        if (node is null) return;

        var stack = _propagateStack;
        var startDepth = stack.Count;
        stack.Push(node);

        while (stack.Count > startDepth)
        {
            var current = stack.Pop();
            current.Host = host;

            if (current is DockSplit split)
            {
                // Push Second first so First pops (and is fully processed) before it —
                // preserves the original pre-order (self, then First's subtree, then Second's).
                stack.Push(split.Second);
                stack.Push(split.First);
            }
        }
    }

    /// <summary>
    /// Called when one of this split's children has become empty. Removes this split from the tree
    /// entirely, promoting the remaining sibling into this split's former position.
    /// </summary>
    internal void Collapse(IDockNode emptyChild)
    {
        var sibling = ReferenceEquals(First, emptyChild) ? Second : First;
        sibling.Parent = null;

        if (Parent is not null)
        { 
            Parent.ReplaceChild(this, sibling);
        }
        else if (Host is not null)
        {
            Host.Root = sibling;
        }
    }
    private void RebuildVisual()
    {
        Children.Clear();

        DetachFromVisualParent(First.View);
        DetachFromVisualParent(Second.View);

        if (Orientation == Orientation.Horizontal)
        {
            SetColumn(First.View, 0);
            SetColumn(_splitter, 1);
            SetColumn(Second.View, 2);
        }
        else
        {
            SetRow(First.View, 0);
            SetRow(_splitter, 1);
            SetRow(Second.View, 2);
        }

        Children.Add(First.View);
        Children.Add(_splitter);
        Children.Add(Second.View);
    }

    private static void DetachFromVisualParent(Control view)
    {
        if (view.Parent is Panel panel) panel.Children.Remove(view);
        else if (view.Parent is Decorator decorator) decorator.Child = null;
    }
}