using System.Runtime.CompilerServices;

namespace Engine.Core;

public static class Logger
{
    public enum LogLevel { Trace, Success, Info, Warning, Error, Fatal }

    public static void Log(
        LogLevel level, 
        string message, 
        [CallerFilePath] string filePath = "", 
        [CallerLineNumber] int lineNumber = 0)
    {
        var fileName = Path.GetFileName(filePath);
        var prevColor = Console.ForegroundColor;

        Console.ForegroundColor = level switch
        {
            LogLevel.Error or LogLevel.Fatal => ConsoleColor.Red,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Trace => ConsoleColor.Gray,
            LogLevel.Success => ConsoleColor.Green,
            _ => ConsoleColor.White
        };

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] [{fileName}:{lineNumber}] {message}");
        Console.ForegroundColor = prevColor;
    }
    
    public static void Log(string message, [CallerFilePath] string f = "", [CallerLineNumber] int l = 0) 
        => Log(LogLevel.Info, message, f, l);

    public static void LogInfo(string message, [CallerFilePath] string f = "", [CallerLineNumber] int l = 0) 
        => Log(LogLevel.Info, message, f, l);

    public static void LogError(string message, [CallerFilePath] string f = "", [CallerLineNumber] int l = 0) 
        => Log(LogLevel.Error, message, f, l);

    public static void LogFatal(string message, [CallerFilePath] string f = "", [CallerLineNumber] int l = 0) 
        => Log(LogLevel.Fatal, message, f, l);

    public static void LogWarning(string message, [CallerFilePath] string f = "", [CallerLineNumber] int l = 0) 
        => Log(LogLevel.Warning, message, f, l);

    public static void LogTrace(string message, [CallerFilePath] string f = "", [CallerLineNumber] int l = 0) 
        => Log(LogLevel.Trace, message, f, l);

    public static void LogSuccess(string message, [CallerFilePath] string f = "", [CallerLineNumber] int l = 0) 
        => Log(LogLevel.Success, message, f, l);
}