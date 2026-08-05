using Avalonia.Controls;
using Avalonia.Media;

namespace HarpyEngine.Sandbox.Editor.Docking;

public enum DockSide
{
    Left,
    Right,
    Top,
    Bottom,
    Center
}

/// <summary>
/// A single dockable panel: a title, an accent color for its tab, the content control it hosts,
/// and the side it should dock to the first time it's opened.
/// </summary>
public sealed class DockItem(
    string id,
    string title,
    IBrush accent,
    Control content,
    DockSide defaultSide = DockSide.Center)
{
    public string Id { get; } = id;
    public string Title { get; set; } = title;
    public IBrush Accent { get; set; } = accent;
    public Control Content { get; } = content;
    public DockSide DefaultSide { get; set; } = defaultSide;
    public bool CanClose { get; set; } = true;

    /// <summary>The group currently hosting this item, or null if it has been closed.</summary>
    internal DockGroup? Group { get; set; }
}