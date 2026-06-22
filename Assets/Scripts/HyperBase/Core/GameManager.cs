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

        public bool IsInState(GameState state) => _currentState == state;
    }
}
