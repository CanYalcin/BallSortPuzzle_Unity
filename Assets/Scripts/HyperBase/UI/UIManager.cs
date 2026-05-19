using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace HyperBase.UI
{
    /// <summary>
    /// Stack-based UI screen manager. All transitions are async and lock-guarded.
    /// Usage: RegisterScreen(screen); await ShowScreenAsync&lt;T&gt;(); await GoBackAsync();
    /// </summary>
    public class UIManager
    {
        private readonly Dictionary<Type, UIScreen> _screens = new();
        private readonly Stack<UIScreen>            _history = new();
        private UIScreen _current;
        private bool     _busy;

        [Inject] public UIManager() { }

        public void RegisterScreen<T>(T screen) where T : UIScreen
        {
            _screens[typeof(T)] = screen;
            screen.gameObject.SetActive(false);
        }

        public async UniTask ShowScreenAsync<T>(bool addToHistory = true) where T : UIScreen
        {
            if (_busy) return;
            if (!_screens.TryGetValue(typeof(T), out var next)) { Debug.LogError($"[UIManager] Not registered: {typeof(T).Name}"); return; }
            if (_current == next) return;
            _busy = true;
            if (_current != null)
            {
                if (addToHistory) _history.Push(_current);
                await _current.HideAsync();
            }
            _current = next;
            await _current.ShowAsync();
            _busy = false;
        }

        public async UniTask GoBackAsync()
        {
            if (_busy || _history.Count == 0) return;
            _busy = true;
            if (_current != null) await _current.HideAsync();
            _current = _history.Pop();
            await _current.ShowAsync();
            _busy = false;
        }

        public T GetScreen<T>() where T : UIScreen
            => _screens.TryGetValue(typeof(T), out var s) ? s as T : null;

        public bool IsVisible<T>() where T : UIScreen
            => _screens.TryGetValue(typeof(T), out var s) && s.IsVisible;

        public void ClearHistory() => _history.Clear();
        public UIScreen CurrentScreen => _current;

        public void HandleBackButton()
        {
            if (_current != null) _current.OnBackPressed();
        }
    }
}
