using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Diagnostics;
using System.IO;

namespace HarpyEngine.Sandbox.Editor.Docking;

/// <summary>
/// A branch node in the docking tree: two child nodes divided by a draggable <see cref="GridSplitter"/>.
/// </summary>
public sealed class DockSplit : Grid, IDockNode
{
    private static readonly StreamWriter? _logWriter;

    static DockSplit()
    {
        try
        {
            // Initializes a dedicated log file in the base execution directory
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docking_debug.log");
            _logWriter = new StreamWriter(logPath, append: false) { AutoFlush = true };
            _logWriter.WriteLine($"=== DockSplit Logging Initialized at {System.DateTime.Now} ===");
        }
        catch
        {
            // Fallback gracefully if file permissions or paths fail
            _logWriter = null;
        }
    }

    private static void Log(string message)
    {
        string formattedMessage = $"[DockSplit] [{System.DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(formattedMessage);
        _logWriter?.WriteLine(formattedMessage);
    }

    public DockSplit? Parent { get; set; }
    public DockHost? Host { get; set; }
    public Control View => this;

    public Orientation Orientation { get; }
    public IDockNode First { get; private set; }
    public IDockNode Second { get; private set; }

    private readonly GridSplitter _splitter;

    public DockSplit(Orientation orientation, IDockNode first, IDockNode second, double firstRatio = 1, double secondRatio = 1)
    {
        Log($"Initializing new DockSplit with Orientation: {orientation}");

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
        Log($"ReplaceChild requested. OldChild Type: {oldChild.GetType().Name}, NewChild Type: {newChild.GetType().Name}");

        Control oldView;
        if (ReferenceEquals(First, oldChild)) 
        { 
            Log("Match found on 'First' child slot.");
            oldView = First.View; 
            First = newChild; 
        }
        else if (ReferenceEquals(Second, oldChild)) 
        { 
            Log("Match found on 'Second' child slot.");
            oldView = Second.View; 
            Second = newChild; 
        }
        else 
        {
            Log("WARNING: oldChild was not found in either First or Second slots!");
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
            Log("Successfully swapped child view safely via index removal/insertion.");
        }
        else
        {
            Log("ERROR: oldView index was negative (-1). Performing safe full rebuild.");
            RebuildVisual();
        }
    }
    /// <summary>Assigns <paramref name="host"/> to every node in the subtree rooted at <paramref name="node"/>.</summary>
    internal static void PropagateHost(IDockNode? node, DockHost? host)
    {
        if (node is null) return;

        node.Host = host;
        if (node is DockSplit split)
        {
            PropagateHost(split.First, host);
            PropagateHost(split.Second, host);
        }
    }

    /// <summary>
    /// Called when one of this split's children has become empty. Removes this split from the tree
    /// entirely, promoting the remaining sibling into this split's former position.
    /// </summary>
    internal void Collapse(IDockNode emptyChild)
    {
        Log("Collapse triggered because a child became empty.");

        var sibling = ReferenceEquals(First, emptyChild) ? Second : First;
        sibling.Parent = null;

        if (Parent is not null)
        {
            Log("Promoting sibling via Parent.ReplaceChild.");
            Parent.ReplaceChild(this, sibling);
        }
        else if (Host is not null)
        {
            Log("Promoting sibling as root of Host.");
            Host.Root = sibling;
        }
    }
    private void RebuildVisual()
    {
        Log($"RebuildVisual executing. Clear existing children count: {Children.Count}");
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

        Log($"RebuildVisual completed. New children count: {Children.Count}");
    }

    private static void DetachFromVisualParent(Control view)
    {
        if (view.Parent is Panel panel) panel.Children.Remove(view);
        else if (view.Parent is Decorator decorator) decorator.Child = null;
    }
}