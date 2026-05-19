using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyperBase.Core
{
    /// <summary>
    /// Type-safe, allocation-minimal event bus using readonly struct events.
    /// Usage:
    ///   _eventBus.Subscribe<OnLevelStarted>(OnLevelStarted);
    ///   _eventBus.Publish(new OnLevelStarted(3));
    ///   _eventBus.Unsubscribe<OnLevelStarted>(OnLevelStarted);
    /// </summary>
    public class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _handlers[type] = list;
            }
            list.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        public void Publish<T>(T eventData) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;
            var snapshot = new List<Delegate>(list);
            foreach (var handler in snapshot)
            {
                try { ((Action<T>)handler)(eventData); }
                catch (Exception e) { Debug.LogError($"[EventBus] Exception in handler for <{typeof(T).Name}>: {e}"); }
            }
        }

        public void Clear() => _handlers.Clear();
    }
}
