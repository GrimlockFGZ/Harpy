public interface IEvent{}

/// <summary>
///  Static event bus for all events in the engine, exposed to sandbox so user can use it too
///  requires event data to be flagged with the
///  <see cref="IEvent"/> IEvent interface
/// </summary>

public static class Event<T> where T : IEvent
{
    private static Action<T>? _action;

    public static IDisposable Subscribe(Action<T> handler)
    {
        _action += handler;
        return new Subscription(() => _action -= handler);
    }

    public static void Invoke(T args)
    {
        _action?.Invoke(args);
    }

    private sealed class Subscription : IDisposable
    {
        private Action _unsubscribe;

        public Subscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            _unsubscribe.Invoke();
            _unsubscribe = null;
        }
    }
}