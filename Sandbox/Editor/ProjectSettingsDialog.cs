using Avalonia.Controls;
using Avalonia.Interactivity;
using Engine.Core;
using JetBrains.Annotations;

namespace HarpyEngine.Sandbox.Editor;

public partial class ProjectSettingsDialog : Window
{
    public ProjectSettingsDialog()
    {
        InitializeComponent();

        MinLevelBox.ItemsSource = Enum.GetValues<Logger.LogLevel>();
        MinLevelBox.SelectedItem = Logger.MinLevel;
    }

    [UsedImplicitly]
    private void OnApplyClicked(object? sender, RoutedEventArgs e)
    {
        if (MinLevelBox.SelectedItem is Logger.LogLevel level)
            Logger.MinLevel = level;

        Close();
    }

    [UsedImplicitly]
    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
