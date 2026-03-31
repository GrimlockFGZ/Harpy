using System.Collections.ObjectModel;
using Avalonia.Threading;
using Engine.Core;

namespace HarpyEngine.Sandbox.Editor.Models
{
    public class HierarchyViewModel 
    {
        private Registry? _registry;
        public ObservableCollection<string> Entries { get; } = [];
        
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
                    Entries.Add($"Entity {entity.Id}");
                }
            }
        }

        private void OnEntityCreated(Entity entity)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Entries.Add($"Entity {entity.Id}");
            });
        }

        private void OnEntityDestroyed(Entity entity)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Entries.Remove($"Entity {entity.Id}");
            });
        }

        public void AddEntry(string entry)
        {
            Entries.Add(entry);
        }
    }
}