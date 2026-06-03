using Avalonia.Controls;
using Avalonia.Interactivity;
using HarpyEngine.Sandbox.Editor.Models;

namespace HarpyEngine.Sandbox.Editor 
{
    public partial class Hierarchy : UserControl
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