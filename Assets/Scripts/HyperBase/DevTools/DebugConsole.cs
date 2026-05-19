// DebugConsole.cs — stripped from release builds via compiler guards.
// Activate: tap 5x anywhere within 2 seconds.
// IMPORTANT: namespace is HyperBase.DevTools, NOT HyperBase.Debug,
//            to avoid shadowing UnityEngine.Debug.

#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Collections.Generic;
using HyperBase.Core;
using HyperBase.Currency;
using HyperBase.Data;
using HyperBase.Gameplay;
using UnityEngine;
using VContainer;

namespace HyperBase.DevTools
{
    public class DebugConsole : MonoBehaviour
    {
        private const int   TapsNeeded = 5;
        private const float TapWindow  = 2f;

        private int     _taps;
        private float   _windowEnd;
        private bool    _visible;
        private Rect    _rect = new Rect(20, 20, 440, 560);
        private Vector2 _scroll;
        private readonly List<string> _logs = new();
        private const int MaxLogs = 40;

        private SaveManager     _save;
        private CurrencyManager _currency;
        private LevelManager    _levels;
        private GameManager     _game;

        [Inject]
        public void Construct(SaveManager save, CurrencyManager currency,
                              LevelManager levels, GameManager game)
        {
            _save = save; _currency = currency; _levels = levels; _game = game;
        }

        private void OnEnable()  => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;

        private void Update()
        {
            bool touched = Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began;
            bool clicked = Input.GetMouseButtonDown(0);
            if (!touched && !clicked) return;

            float now = Time.unscaledTime;
            if (now > _windowEnd) { _taps = 0; _windowEnd = now + TapWindow; }
            _taps++;
            if (_taps >= TapsNeeded) { _visible = !_visible; _taps = 0; }
        }

        private void OnGUI()
        {
            if (!_visible) return;
            GUI.skin.label.fontSize  = 18;
            GUI.skin.button.fontSize = 20;
            _rect = GUI.Window(99, _rect, DrawWindow, "HyperBase Debug");
        }

        private void DrawWindow(int id)
        {
            var d = _save?.Data;
            if (d != null)
            {
                GUILayout.Label("Level: " + d.CurrentLevelIndex + "  Sessions: " + d.TotalSessionCount);
                GUILayout.Label("Soft: " + d.SoftCurrency + "   Hard: " + d.HardCurrency);
                GUILayout.Label("State: " + _game?.CurrentState + "   NoAds: " + d.IsNoAds);
            }

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1000 Soft")) _currency?.Add(CurrencyType.Soft, 1000);
            if (GUILayout.Button("+100 Hard"))  _currency?.Add(CurrencyType.Hard, 100);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Complete")) _levels?.CompleteCurrentLevel();
            if (GUILayout.Button("Fail"))     _levels?.FailCurrentLevel();
            if (GUILayout.Button("Retry"))    _levels?.RetryCurrentLevel();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))     _save?.Save();
            if (GUILayout.Button("Del Save")) _save?.DeleteSave();
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label("Logs (" + _logs.Count + "):");
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(140));
            foreach (var l in _logs) GUILayout.Label(l);
            GUILayout.EndScrollView();

            if (GUILayout.Button("X  Close")) _visible = false;
            GUI.DragWindow();
        }

        private void OnLog(string msg, string stack, LogType type)
        {
            string icon = type == LogType.Error ? "E" : type == LogType.Warning ? "W" : "I";
            _logs.Insert(0, "[" + icon + "] " + msg);
            if (_logs.Count > MaxLogs) _logs.RemoveAt(_logs.Count - 1);
        }
    }
}

#endif
