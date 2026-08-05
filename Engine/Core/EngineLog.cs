using System.Collections.ObjectModel;
using System.Text;

namespace Engine.Core;

public enum LogLevel
{
    Trace,
    Info,
    Warning,
    Error,
    Critical
}

public readonly record struct LogEntry(DateTime Timestamp, LogLevel Level, string Category, string Message);

/// <summary>
/// Central logging service backing the editor's Console panel. Anything logged through
/// <see cref="Info"/>/<see cref="Warning"/>/<see cref="Error"/>/<see cref="Critical"/> is appended
/// to <see cref="Entries"/>. In addition, <see cref="Install"/> tees the standard Console output
/// stream so pre-existing <c>Console.WriteLine</c> calls scattered across the engine keep working
/// and automatically show up in the panel too, without needing to touch every call site.
/// </summary>
public static class EngineLog
{
    private const int MaxEntries = 5000;
    private static bool _installed;

    public static ObservableCollection<LogEntry> Entries { get; } = [];

    /// <summary>
    /// The host application (Sandbox) should assign this once at startup to marshal collection
    /// mutations onto its UI thread, e.g. <c>EngineLog.UiInvoke = a =&gt; Dispatcher.UIThread.Post(a);</c>.
    /// Engine.Core itself has no UI framework dependency, so this stays a plain delegate.
    /// When unset, entries are added synchronously on the calling thread.
    /// </summary>
    public static Action<Action>? UiInvoke { get; set; }

    /// <summary>
    /// Begins mirroring <see cref="Console.Out"/> into <see cref="Entries"/>. Safe to call multiple
    /// times; only installs once. Called automatically the first time anything is logged.
    /// </summary>
    public static void Install()
    {
        if (_installed) return;
        _installed = true;

        Console.SetOut(new ConsoleTee(Console.Out));
    }

    public static void Trace(string message, string category = "General") => Write(LogLevel.Trace, category, message);
    public static void Info(string message, string category = "General") => Write(LogLevel.Info, category, message);
    public static void Warning(string message, string category = "General") => Write(LogLevel.Warning, category, message);
    public static void Error(string message, string category = "General") => Write(LogLevel.Error, category, message);
    public static void Critical(string message, string category = "General") => Write(LogLevel.Critical, category, message);

    public static void Clear()
    {
        RunOnUiThread(() => Entries.Clear());
    }

    private static void Write(LogLevel level, string category, string message)
    {
        Install();
        var entry = new LogEntry(DateTime.Now, level, category, message);
        RunOnUiThread(() => AddEntry(entry));
    }

    private static void RunOnUiThread(Action action)
    {
        var invoke = UiInvoke;
        if (invoke is null) action();
        else invoke(action);
    }

    private static void AddEntry(LogEntry entry)
    {
        Entries.Add(entry);
        while (Entries.Count > MaxEntries)
        {
            Entries.RemoveAt(0);
        }
    }

    /// <summary>
    /// Wraps the original console writer, forwarding every line it receives both to the real
    /// console (so terminal output is unaffected) and into <see cref="EngineLog"/>.
    /// </summary>
    private sealed class ConsoleTee(TextWriter original) : TextWriter
    {
        private readonly StringBuilder _buffer = new();

        public override Encoding Encoding => original.Encoding;

        public override void Write(char value)
        {
            original.Write(value);

            if (value == '\n')
            {
                FlushLine();
            }
            else if (value != '\r')
            {
                _buffer.Append(value);
            }
        }

        public override void Write(string? value)
        {
            if (string.IsNullOrEmpty(value)) return;
            foreach (var ch in value)
            {
                Write(ch);
            }
        }

        public override void WriteLine(string? value)
        {
            original.WriteLine(value);

            if (!string.IsNullOrEmpty(value))
            {
                _buffer.Append(value);
            }

            FlushLine();
        }

        private void FlushLine()
        {
            if (_buffer.Length == 0) return;

            var line = _buffer.ToString();
            _buffer.Clear();

            var (level, category) = Classify(line);
            EngineLog.Write(level, category, line);
        }

        private static (LogLevel Level, string Category) Classify(string line)
        {
            var category = "Engine";
            if (line.StartsWith('[') )
            {
                var closeIndex = line.IndexOf(']');
                if (closeIndex > 0)
                {
                    category = line[1..closeIndex];
                }
            }

            var upper = line.ToUpperInvariant();
            if (upper.Contains("CRITICAL")) return (LogLevel.Critical, category);
            if (upper.Contains("ERROR") || upper.Contains("EXCEPTION")) return (LogLevel.Error, category);
            if (upper.Contains("WARN")) return (LogLevel.Warning, category);

            return (LogLevel.Info, category);
        }
    }
}