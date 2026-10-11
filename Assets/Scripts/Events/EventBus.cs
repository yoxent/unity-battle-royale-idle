using System;
using System.Collections.Generic;

namespace BattleRoyale.Events
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> handlers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (handlers.TryGetValue(typeof(T), out var existing))
            {
                handlers[typeof(T)] = Delegate.Combine(existing, handler);
            }
            else
            {
                handlers[typeof(T)] = handler;
            }
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null ||
                !handlers.TryGetValue(typeof(T), out var existing))
            {
                return;
            }

            var updated = Delegate.Remove(existing, handler);
            if (updated == null)
            {
                handlers.Remove(typeof(T));
            }
            else
            {
                handlers[typeof(T)] = updated;
            }
        }

        public static void Publish<T>(T payload)
        {
            if (!handlers.TryGetValue(typeof(T), out var existing))
            {
                return;
            }

            foreach (var handler in existing.GetInvocationList())
            {
                ((Action<T>)handler).Invoke(payload);
            }
        }
    }
}
