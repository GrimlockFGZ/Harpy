using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Engine.Core;

namespace HarpyEngine.Sandbox.Editor.Models
{
    public class ConsoleViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<LogEntry> Filtered { get; } = [];

        private bool _showInfo = true;
        public bool ShowInfo
        {
            get => _showInfo;
            set
            {
                if (_showInfo == value) return;
                _showInfo = value;
                OnPropertyChanged();
                Refresh();
            }
        }

        private bool _showWarning = true;
        public bool ShowWarning
        {
            get => _showWarning;
            set
            {
                if (_showWarning == value) return;
                _showWarning = value;
                OnPropertyChanged();
                Refresh();
            }
        }

        private bool _showError = true;
        public bool ShowError
        {
            get => _showError;
            set
            {
                if (_showError == value) return;
                _showError = value;
                OnPropertyChanged();
                Refresh();
            }
        }

        private bool _autoScroll = true;
        public bool AutoScroll
        {
            get => _autoScroll;
            set
            {
                if (_autoScroll == value) return;
                _autoScroll = value;
                OnPropertyChanged();
            }
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
                Refresh();
            }
        }

        /// <summary>Raised whenever an entry is appended (not on a full filter rebuild), used by the view to auto-scroll.</summary>
        public event Action? EntryAppended;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ConsoleViewModel()
        {
            EngineLog.Entries.CollectionChanged += OnSourceChanged;
            Refresh();
        }

        public void Clear() => EngineLog.Clear();

        private void OnSourceChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            {
                var appended = false;
                foreach (LogEntry entry in e.NewItems)
                {
                    if (!PassesFilter(entry)) continue;
                    Filtered.Add(entry);
                    appended = true;
                }

                if (appended) EntryAppended?.Invoke();
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            Filtered.Clear();
            foreach (var entry in EngineLog.Entries)
            {
                if (PassesFilter(entry)) Filtered.Add(entry);
            }

            EntryAppended?.Invoke();
        }

        private bool PassesFilter(LogEntry entry)
        {
            var levelOk = entry.Level switch
            {
                LogLevel.Error or LogLevel.Critical => ShowError,
                LogLevel.Warning => ShowWarning,
                _ => ShowInfo,
            };

            if (!levelOk) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            return entry.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                   || entry.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        private void OnPropertyChanged([CallerMemberName] string? memberName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }
    }
}