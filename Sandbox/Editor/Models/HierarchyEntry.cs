using Engine.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HarpyEngine.Sandbox.Editor.Models;

public sealed class HierarchyEntry : INotifyPropertyChanged
{
    public Entity Entity { get; init; }
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static HierarchyEntry FromEntity(Entity entity)
    {
        return new HierarchyEntry
        {
            Entity = entity,
            Name = $"Entity {entity.Id}"
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? memberName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
    }
}