using Engine.Core;

public interface IEvent;

public record struct EntityCreated(Entity Entity) : IEvent;
public record struct EntityDestroyed(Entity Entity) : IEvent;

public static class Event<T> where T : IEvent
{
    private static Action<T>? _action;

    public static IDisposable Subscribe(Action<T> handler)
    {
        _action += handler;
        
        return new Subscription(handler);
    }

    public static void Invoke(T args)
    {
        _action?.Invoke(args);
    }

    /// <summary>
    /// Statically typed generic subscription that directly targets the parent's event state.
    /// </summary>
    private sealed class Subscription(Action<T> handler) : IDisposable
    {
        private Action<T>? _handler = handler;

        public void Dispose()
        {
            var handlerToTarget = Interlocked.Exchange(ref _handler, null);
            if (handlerToTarget is null) return;

            _action -= handlerToTarget;
        }
    }
}