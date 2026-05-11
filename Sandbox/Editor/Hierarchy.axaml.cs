using Avalonia.Controls;
using Avalonia.Interactivity;
using HarpyEngine.Sandbox.Editor.Models;

namespace HarpyEngine.Sandbox.Editor // <-- Make sure this matches your project root/folder structure
{
    public partial class Hierarchy : UserControl // Or the base class your XAML uses
    {
        public HierarchyViewModel ViewModel => (HierarchyViewModel)DataContext!;

        public Hierarchy()
        {
            InitializeComponent();
        }

        private void OnAddEntityClick(object? sender, RoutedEventArgs e)
        {
            ViewModel.CreateEntity();
        }

        private void OnRemoveEntityClick(object? sender, RoutedEventArgs e)
        {
            ViewModel.DestroySelectedEntity();
        }
    }
}