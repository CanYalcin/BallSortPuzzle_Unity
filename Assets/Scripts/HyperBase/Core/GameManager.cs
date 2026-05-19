using UnityEngine;
using VContainer;

namespace HyperBase.Core
{
    /// <summary>
    /// Central Finite State Machine (FSM) that governs high-level game flow.
    /// States: Boot -> MainMenu <-> Gameplay -> Win / Fail -> Gameplay ...
    /// </summary>
    public class GameManager
    {
        private readonly EventBus _eventBus;
        private GameState _currentState = GameState.Boot;
        private GameState _stateBeforePause;

        public GameState CurrentState => _currentState;
        public bool IsPlaying => _currentState == GameState.Gameplay;
        public bool IsInMenu  => _currentState == GameState.MainMenu;

        [Inject]
        public GameManager(EventBus eventBus) => _eventBus = eventBus;

        public void TransitionTo(GameState newState)
        {
            if (_currentState == newState) { Debug.LogWarning($"[GameManager] Already in state: {newState}"); return; }
            var previous = _currentState;
            _currentState = newState;
            Debug.Log($"[GameManager] {previous} -> {newState}");
            _eventBus.Publish(new OnGameStateChanged(previous, newState));
        }

        public void Pause()
        {
            if (_currentState == GameState.Paused) return;
            _stateBeforePause = _currentState;
            UnityEngine.Time.timeScale = 0f;
            TransitionTo(GameState.Paused);
        }

        public void Resume()
        {
            if (_currentState != GameState.Paused) return;
            UnityEngine.Time.timeScale = 1f;
            TransitionTo(_stateBeforePause);
        }

        public bool IsInState(GameState state) => _currentState == state;
    }
}
