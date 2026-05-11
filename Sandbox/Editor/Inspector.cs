using Avalonia.Controls;
using Avalonia.Interactivity;
using HarpyEngine.Sandbox.Editor.Models;

namespace HarpyEngine.Sandbox.Editor;

public partial class Inspector : UserControl
{
    public InspectorViewModel ViewModel => (InspectorViewModel)DataContext!;

    public Inspector()
    {
        InitializeComponent();
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.ApplyChanges();
    }

    private void OnAddTransformClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.AddTransform();
    }

    private void OnRemoveTransformClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.RemoveTransform();
    }
}