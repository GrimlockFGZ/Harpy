using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using HarpyEngine.Sandbox.Editor;

namespace HarpyEngine.Sandbox;

public class App : Application
{
    public override void Initialize()
    {
        // Load the base Avalonia theme
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new EditorWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}