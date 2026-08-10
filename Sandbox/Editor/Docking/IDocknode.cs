using Avalonia.Controls;

namespace HarpyEngine.Sandbox.Editor.Docking;

/// <summary>
/// A node in the docking layout tree. Leaves are <see cref="DockGroup"/> (a tabbed panel host);
/// branches are <see cref="DockSplit"/> (two nodes divided by a draggable splitter).
/// </summary>
public interface IDockNode
{
    /// <summary>The visual that represents this node in the tree.</summary>
    Control View { get; }

    /// <summary>The split that contains this node, or null if this node is the tree root.</summary>
    DockSplit? Parent { get; set; }

    /// <summary>The dock host that owns this tree.</summary>
    DockHost? Host { get; set; }
}