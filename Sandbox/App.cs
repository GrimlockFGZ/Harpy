using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Engine.Core;
using HarpyEngine.Sandbox.Editor;

namespace HarpyEngine.Sandbox;

public class App : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Engine.Core has no UI framework dependency, so it exposes a plain delegate for
        // marshalling log updates onto the UI thread; wire it up before anything can log.
        EngineLog.UiInvoke = action => Dispatcher.UIThread.Post(action);
        EngineLog.Install();
        EngineLog.Info("Harpy Engine starting up.", "Editor");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new EditorWindow();
        }
        
        AllocationTracker.StartTracking();
        base.OnFrameworkInitializationCompleted();
        
    }
}