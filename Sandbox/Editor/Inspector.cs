using Avalonia.Controls;
using Avalonia.Interactivity;
using HarpyEngine.Sandbox.Editor.Models;

namespace HarpyEngine.Sandbox.Editor
{
    public partial class Inspector : UserControl
    {
        private InspectorViewModel ViewModel => (InspectorViewModel)DataContext!;

        public Inspector()
        {
            InitializeComponent();
        }

        // Called by the "Apply Changes" button.
        // Delegates to the view model, which fires ApplyRequestedEvent on the
        // global event bus. EditorWindow.OnApplyRequested handles the actual
        // registry-write and sets the status string.
        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            ViewModel.ApplyChanges();
        }

        // Called by the "Transform" item inside the "+ Component" flyout.
        // Fires AddTransformRequestedEvent; EditorWindow.OnAddTransformRequested
        // checks for duplicates, writes to the registry, and refreshes the view model.
        private void OnAddTransformClick(object? sender, RoutedEventArgs e)
        {
            ViewModel.AddTransform();
        }

        // Called by the ✕ button on the Transform component header.
        // Fires RemoveTransformRequestedEvent; EditorWindow.OnRemoveTransformRequested
        // removes the component from the registry and clears the transform fields.
        private void OnRemoveTransformClick(object? sender, RoutedEventArgs e)
        {
            ViewModel.RemoveTransform();
        }

        private void OnAddVoxelBlockClick(object? sender, RoutedEventArgs e)
        {
            ViewModel.AddVoxelBlock();
        }

        private void OnRemoveVoxelBlockClick(object? sender, RoutedEventArgs e)
        {
            ViewModel.RemoveVoxelBlock();
        }
    }
}