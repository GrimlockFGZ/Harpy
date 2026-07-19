using System.ComponentModel;
using System.Runtime.CompilerServices;
using Engine;
using HarpyEngine.Rendering.Voxel;

namespace HarpyEngine.Sandbox.Editor.Models;

// ============================================================================
// Global Event Bus Messages
// ============================================================================
public record PropertyChangedEvent(object Sender, string PropertyName) : IEvent;
public record ApplyRequestedEvent(HierarchyEntry Entry) : IEvent;
public record AddTransformRequestedEvent(HierarchyEntry Entry) : IEvent;
public record RemoveTransformRequestedEvent(HierarchyEntry Entry) : IEvent;
public record AddVoxelBlockRequestedEvent(HierarchyEntry Entry) : IEvent;
public record RemoveVoxelBlockRequestedEvent(HierarchyEntry Entry) : IEvent;

// ============================================================================
// Inspector View Model
// ============================================================================
public sealed class InspectorViewModel : INotifyPropertyChanged
{
    private HierarchyEntry? _selectedEntry;
    public event PropertyChangedEventHandler? PropertyChanged;

    // ------------------------------------------------------------------------
    // Properties (Using Streamlined Field-Backed Property Syntaxes)
    // ------------------------------------------------------------------------
    public string SelectedEntityName { get => field; set => Set(ref field, value); } = "No entity selected";
    public string PositionX          { get => field; set => Set(ref field, value); } = "0";
    public string PositionY          { get => field; set => Set(ref field, value); } = "0";
    public string PositionZ          { get => field; set => Set(ref field, value); } = "0";
    public string ScaleX             { get => field; set => Set(ref field, value); } = "1";
    public string ScaleY             { get => field; set => Set(ref field, value); } = "1";
    public string ScaleZ             { get => field; set => Set(ref field, value); } = "1";
    public bool HasTransform         { get => field; private set => Set(ref field, value); }
    public bool HasVoxelBlock        { get => field; private set => Set(ref field, value); }
    public string VoxelBlockType     { get => field; private set => Set(ref field, value); } = "";
    public string Status             { get => field; private set => Set(ref field, value); } = "-";

    // ------------------------------------------------------------------------
    // Public Operations / Methods
    // ------------------------------------------------------------------------
    public void SetSelection(HierarchyEntry entry, bool hasTransform, Transform? transform, bool hasVoxelBlock = false, BlockType voxelBlockType = BlockType.Air)
    {
        _selectedEntry = entry;
        SelectedEntityName = entry.Name;
        HasTransform = hasTransform;
        HasVoxelBlock = hasVoxelBlock;
        VoxelBlockType = hasVoxelBlock ? voxelBlockType.ToString() : "";
        Status = "-";

        if (transform is { } t)
        {
            PositionX = t.Position.X.ToString("0.###");
            PositionY = t.Position.Y.ToString("0.###");
            PositionZ = t.Position.Z.ToString("0.###");
            ScaleX = t.Scale.X.ToString("0.###");
            ScaleY = t.Scale.Y.ToString("0.###");
            ScaleZ = t.Scale.Z.ToString("0.###");
        }
        else
        {
            PositionX = "0";
            PositionY = "0";
            PositionZ = "0";
            ScaleX = "1";
            ScaleY = "1";
            ScaleZ = "1";
        }
    }

    public void ClearSelection()
    {
        _selectedEntry = null;
        SelectedEntityName = "No entity selected";
        HasTransform = false;
        HasVoxelBlock = false;
        VoxelBlockType = "";
        Status = "-";
    }

    public bool TryGetTransformValues(out Vector3 position, out Vector3 scale)
    {
        position = default;
        scale = default;
        if (!float.TryParse(PositionX, out var px) ||
            !float.TryParse(PositionY, out var py) ||
            !float.TryParse(PositionZ, out var pz) ||
            !float.TryParse(ScaleX, out var sx) ||
            !float.TryParse(ScaleY, out var sy) ||
            !float.TryParse(ScaleZ, out var sz))
        {
            return false;
        }

        position = new Vector3(px, py, pz);
        scale = new Vector3(sx, sy, sz);
        return true;
    }

    public void ApplyChanges()
    {
        if (_selectedEntry is null) return;
        Event<ApplyRequestedEvent>.Invoke(new(_selectedEntry));
    }

    public void AddTransform()
    {
        if (_selectedEntry is null) return;
        Event<AddTransformRequestedEvent>.Invoke(new(_selectedEntry));
    }

    public void RemoveTransform()
    {
        if (_selectedEntry is null) return;
        Event<RemoveTransformRequestedEvent>.Invoke(new(_selectedEntry));
    }

    public void AddVoxelBlock()
    {
        if (_selectedEntry is null) return;
        Event<AddVoxelBlockRequestedEvent>.Invoke(new(_selectedEntry));
    }

    public void RemoveVoxelBlock()
    {
        if (_selectedEntry is null) return;
        Event<RemoveVoxelBlockRequestedEvent>.Invoke(new(_selectedEntry));
    }

    public void SetStatus(string message)
    {
        Status = message;
    }

    // ------------------------------------------------------------------------
    // Private Helpers & Event Dispatching
    // ------------------------------------------------------------------------
    private bool Set<T>(ref T fieldLocation, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(fieldLocation, value)) return false;

        fieldLocation = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? memberName = null)
    {
        if (memberName is null) return;

        // 1. Alert local UI Data-Binding frameworks
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));

        // 2. Alert engine architectural layers globally via static event channels
        Event<PropertyChangedEvent>.Invoke(new PropertyChangedEvent(this, memberName));
    }
}