using HyperBase.UI;
using HyperBase.UI.Screens;
using SortPuzzle.Gameplay;
using SortPuzzle.UI.Screens;
using SortPuzzle.UI.Widgets;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HyperBase.Bootstrap
{
    /// <summary>
    /// Per-scene VContainer LifetimeScope. Inherits all root singletons from BootstrapInstaller.
    ///
    /// Inspector setup:
    ///   1. Set Parent = BootstrapInstaller in the VContainer header.
    ///   2. Drag all screen GameObjects from the scene Canvas into the slots below.
    ///   3. GameScene only: drag LevelController, GoldCounterWidget, BoostBarWidget.
    ///   4. MainMenu only: drag DailyChallengeScreen, ShopScreen, WorldMapScreen.
    ///      Leave those four empty in GameScene/MainMenu respectively.
    /// </summary>
    public class GameInstaller : LifetimeScope
    {
        [Header("Core UI Screens (both scenes)")]
        [SerializeField] private MainMenuScreen  _mainMenu;
        [SerializeField] private GameplayScreen  _gameplay;
        [SerializeField] private WinScreen       _win;
        [SerializeField] private FailScreen      _fail;
        [SerializeField] private LoadingScreen   _loading;
        [SerializeField] private SettingsScreen  _settings;

        [Header("MainMenu-only Screens (leave empty in GameScene)")]
        [SerializeField] private DailyChallengeScreen _dailyChallenge;
        [SerializeField] private ShopScreen           _shop;
        [SerializeField] private WorldMapScreen       _worldMap;

        [Header("GameScene-only (leave empty in MainMenu)")]
        [SerializeField] private LevelController   _levelController;
        [SerializeField] private GoldCounterWidget _goldCounterWidget;
        [SerializeField] private BoostBarWidget    _boostBarWidget;

        protected override void Configure(IContainerBuilder builder)
        {
            // Core screens — null-safe for scenes that don't have all screens
            if (_mainMenu)   builder.RegisterComponent(_mainMenu);
            if (_gameplay)   builder.RegisterComponent(_gameplay);
            if (_win)        builder.RegisterComponent(_win);
            if (_fail)       builder.RegisterComponent(_fail);
            if (_loading)    builder.RegisterComponent(_loading);
            if (_settings)   builder.RegisterComponent(_settings);

            // SortPuzzle C# services — scoped per scene
            builder.Register<PuzzleController>(Lifetime.Scoped);
            builder.Register<BoostSystem>     (Lifetime.Scoped);

            // Optional MainMenu screens
            if (_dailyChallenge) builder.RegisterComponent(_dailyChallenge);
            if (_shop)           builder.RegisterComponent(_shop);
            if (_worldMap)       builder.RegisterComponent(_worldMap);

            // Optional GameScene MonoBehaviours
            if (_levelController   != null) builder.RegisterComponent(_levelController);
            if (_goldCounterWidget != null) builder.RegisterComponent(_goldCounterWidget);
            if (_boostBarWidget    != null) builder.RegisterComponent(_boostBarWidget);

            builder.RegisterEntryPoint<GameSceneEntryPoint>();
        }
    }
}
