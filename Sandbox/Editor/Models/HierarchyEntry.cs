using System.ComponentModel;
using System.Runtime.CompilerServices;
using Engine.Core;

namespace HarpyEngine.Sandbox.Editor.Models;

public sealed class HierarchyEntry : INotifyPropertyChanged
{
    public Entity Entity { get; private init; }
    public string Tag { get; set; } = "Empty";


    public string Name
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

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