using System.ComponentModel;
using System.Runtime.CompilerServices;
using Engine;
using HarpyEngine.Sandbox.Editor.Models;

namespace HarpyEngine.Sandbox.Editor.Models;

public sealed class InspectorViewModel : INotifyPropertyChanged
{
    private HierarchyEntry? _selectedEntry;
    private string _selectedEntityName = "No entity selected";
    private string _positionX = "0";
    private string _positionY = "0";
    private string _positionZ = "0";
    private string _scaleX = "1";
    private string _scaleY = "1";
    private string _scaleZ = "1";
    private bool _hasTransform;
    private string _status = "-";

    public string SelectedEntityName
    {
        get => _selectedEntityName;
        set
        {
            if (_selectedEntityName == value) return;
            _selectedEntityName = value;
            OnPropertyChanged();
        }
    }

    public string PositionX
    {
        get => _positionX;
        set
        {
            if (_positionX == value) return;
            _positionX = value;
            OnPropertyChanged();
        }
    }

    public string PositionY
    {
        get => _positionY;
        set
        {
            if (_positionY == value) return;
            _positionY = value;
            OnPropertyChanged();
        }
    }

    public string PositionZ
    {
        get => _positionZ;
        set
        {
            if (_positionZ == value) return;
            _positionZ = value;
            OnPropertyChanged();
        }
    }

    public string ScaleX
    {
        get => _scaleX;
        set
        {
            if (_scaleX == value) return;
            _scaleX = value;
            OnPropertyChanged();
        }
    }

    public string ScaleY
    {
        get => _scaleY;
        set
        {
            if (_scaleY == value) return;
            _scaleY = value;
            OnPropertyChanged();
        }
    }

    public string ScaleZ
    {
        get => _scaleZ;
        set
        {
            if (_scaleZ == value) return;
            _scaleZ = value;
            OnPropertyChanged();
        }
    }

    public bool HasTransform
    {
        get => _hasTransform;
        private set
        {
            if (_hasTransform == value) return;
            _hasTransform = value;
            OnPropertyChanged();
        }
    }

    public string Status
    {
        get => _status;
        private set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<HierarchyEntry>? ApplyRequested;
    public event Action<HierarchyEntry>? AddTransformRequested;
    public event Action<HierarchyEntry>? RemoveTransformRequested;

    public void SetSelection(HierarchyEntry entry, bool hasTransform, Transform? transform)
    {
        _selectedEntry = entry;
        SelectedEntityName = entry.Name;
        HasTransform = hasTransform;
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
        ApplyRequested?.Invoke(_selectedEntry);
    }

    public void AddTransform()
    {
        if (_selectedEntry is null) return;
        AddTransformRequested?.Invoke(_selectedEntry);
    }

    public void RemoveTransform()
    {
        if (_selectedEntry is null) return;
        RemoveTransformRequested?.Invoke(_selectedEntry);
    }

    public void SetStatus(string message)
    {
        Status = message;
    }

    private void OnPropertyChanged([CallerMemberName] string? memberName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
    }
}