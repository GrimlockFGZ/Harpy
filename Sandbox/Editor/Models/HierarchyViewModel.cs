using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using Engine.Core;

namespace HarpyEngine.Sandbox.Editor.Models
{
    public record struct SelectionChanged(HierarchyEntry? Entry) : IEvent;

    public class HierarchyViewModel : INotifyPropertyChanged
    {
        private Registry? _registry;
        private readonly List<IDisposable> _subscriptions = [];

        public ObservableCollection<HierarchyEntry> Entries { get; } = [];

        public HierarchyEntry? SelectedEntry
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                OnPropertyChanged();
                Event<SelectionChanged>.Invoke(new SelectionChanged(value));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void SetRegistry(Registry registry)
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();

            _registry = registry;
            Entries.Clear();

            if (_registry == null) return;
            _subscriptions.Add(Event<EntityCreated>.Subscribe(OnEntityCreated));
            _subscriptions.Add(Event<EntityDestroyed>.Subscribe(OnEntityDestroyed));

            foreach (var entity in _registry.GetAllEntities())
            {
                Entries.Add(HierarchyEntry.FromEntity(entity));
            }
        }

        private void OnEntityCreated(EntityCreated @event)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Entries.Add(HierarchyEntry.FromEntity(@event.Entity));
            });
        }

        private void OnEntityDestroyed(EntityDestroyed @event)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var entry = Entries.FirstOrDefault(e => e.Entity == @event.Entity);
                if (entry is null) return;

                var removedSelected = SelectedEntry == entry;
                Entries.Remove(entry);

                if (removedSelected)
                {
                    SelectedEntry = null;
                }
            });
        }

        public Entity CreateEntity()
        {
            if (_registry is null)
                throw new InvalidOperationException("Registry is not set.");

            return _registry.CreateEntity();
        }

        public void DestroySelectedEntity()
        {
            if (_registry is null || SelectedEntry is null) return;
            _registry.DestroyEntity(SelectedEntry.Entity);
        }

        private void OnPropertyChanged([CallerMemberName] string? memberName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }
    }
}