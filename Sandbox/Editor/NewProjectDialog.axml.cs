using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace HarpyEngine.Sandbox.Editor;

public partial class NewProjectDialog : Window
{
    public string? ProjectPath { get; private set; }

    public NewProjectDialog()
    {
        InitializeComponent();
    }

    private async void OnBrowseClicked(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select a Project Location",
            AllowMultiple = false
        });
        if (folders.Count > 0)
            LocationBox.Text = folders[0].TryGetLocalPath();
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnCreateClicked(object? sender, RoutedEventArgs e)
    {
        var name = ProjectNameBox.Text?.Trim();
        var location = LocationBox.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowError("Project name cannot be empty");
            return;
        }

        if (string.IsNullOrEmpty(location) || !Directory.Exists(location))
        {
            ShowError("Invalid project location");
            return;
        }
        var projectRoot = Path.Combine(location, name);
        var assetsRoot = Path.Combine(projectRoot, "Assets");

        try
        {
            Directory.CreateDirectory(assetsRoot);
            ProjectPath = assetsRoot;
            Close();
        }
        catch(Exception ex)
        {
            ShowError($"Failed to create project: {ex.Message}");
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}