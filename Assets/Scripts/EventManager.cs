using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThatGameJam
{
    public static class EventManager
    {
        private static readonly Dictionary<Type, Delegate> listeners = new Dictionary<Type, Delegate>();

        public static void Subscribe<TEvent>(Action<TEvent> listener)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));

            Type eventType = typeof(TEvent);
            if (listeners.TryGetValue(eventType, out Delegate existing))
                listeners[eventType] = Delegate.Combine(existing, listener);
            else
                listeners.Add(eventType, listener);
        }

        public static void Unsubscribe<TEvent>(Action<TEvent> listener)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));

            Type eventType = typeof(TEvent);
            if (!listeners.TryGetValue(eventType, out Delegate existing)) return;

            Delegate updated = Delegate.Remove(existing, listener);
            if (updated == null) listeners.Remove(eventType);
            else listeners[eventType] = updated;
        }

        public static void Publish<TEvent>(TEvent eventData)
        {
            if (listeners.TryGetValue(typeof(TEvent), out Delegate callbacks))
                ((Action<TEvent>)callbacks).Invoke(eventData);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetListeners()
        {
            listeners.Clear();
        }
    }
}
