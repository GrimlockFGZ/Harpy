using Avalonia.Controls;
using Avalonia.Interactivity;
using HarpyEngine.Sandbox.Editor.Models;

namespace HarpyEngine.Sandbox.Editor
{
    public partial class ConsolePanel : UserControl
    {
        private ConsoleViewModel ViewModel => (ConsoleViewModel)DataContext!;

        public ConsolePanel()
        {
            InitializeComponent();

            DataContextChanged += (_, _) =>
            {
                if (DataContext is ConsoleViewModel vm)
                {
                    vm.EntryAppended += OnEntryAppended;
                }
            };
        }

        private void OnEntryAppended()
        {
            if (!ViewModel.AutoScroll || ViewModel.Filtered.Count == 0) return;

            var lastEntry = ViewModel.Filtered[^1];
            LogList.ScrollIntoView(lastEntry);
        }

        private void OnClearClicked(object? sender, RoutedEventArgs e)
        {
            ViewModel.Clear();
        }
    }
}