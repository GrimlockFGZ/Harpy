using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using Engine.Core;

namespace HarpyEngine.Sandbox.Editor.Models
{
    public class HierarchyViewModel : INotifyPropertyChanged
    {
        private Registry? _registry;
        private HierarchyEntry? _selectedEntry;

        public ObservableCollection<HierarchyEntry> Entries { get; } = [];

        public HierarchyEntry? SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (_selectedEntry == value) return;
                _selectedEntry = value;
                OnPropertyChanged();
                SelectedEntryChanged?.Invoke(value);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<HierarchyEntry?>? SelectedEntryChanged;
        
        public HierarchyViewModel()
        {
        }

        public void SetRegistry(Registry registry)
        {
            if (_registry != null)
            {
                _registry.EntityCreated -= OnEntityCreated;
                _registry.EntityDestroyed -= OnEntityDestroyed;
            }

            _registry = registry;
            Entries.Clear();

            if (_registry != null)
            {
                _registry.EntityCreated += OnEntityCreated;
                _registry.EntityDestroyed += OnEntityDestroyed;

                foreach (var entity in _registry.GetAllEntities())
                {
                    Entries.Add(HierarchyEntry.FromEntity(entity));
                }
            }
        }

        private void OnEntityCreated(Entity entity)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Entries.Add(HierarchyEntry.FromEntity(entity));
            });
        }

        private void OnEntityDestroyed(Entity entity)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var entry = Entries.FirstOrDefault(e => e.Entity == entity);
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