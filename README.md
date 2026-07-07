[![Unity Tests](https://github.com/CanYalcin/BallSortPuzzle_Unity/actions/workflows/unity-ci.yml/badge.svg)](https://github.com/CanYalcin/BallSortPuzzle_Unity/actions/workflows/unity-ci.yml)

Ball Sort Puzzle

A hypercasual mobile puzzle game built in Unity 6, developed using an AI-assisted engineering workflow. The project demonstrates a complete, commercially-structured game architecture — dependency injection, event-driven systems, encrypted persistence, provider-pattern monetization and analytics, procedural content generation with automated solvability validation, and CI-tested gameplay logic.


Note on development: This project was built using an AI-assisted development methodology. All architecture decisions, design choices, debugging, and quality control were directed by the developer. The AI served as an implementation partner, not an author.




🎮 Gameplay

Classic ball sort puzzle — move colored balls between tubes until each tube contains only one color. Simple to learn, satisfying to master.


Tap a tube to select it, tap another to pour
Complete all tubes to win the level
30-level campaign, procedurally generated and solver-validated
Daily challenge with a 30-day rotating level pool and streak rewards
2 boost types: Undo and Extra Empty Tube, earned from level rewards or via rewarded ads



🏗️ Architecture

The project is split into two layers:

HyperBase (reusable framework)

A game-agnostic mobile game framework that can be dropped into any hypercasual project:

HyperBase/
├── Bootstrap/       — VContainer LifetimeScopes, scene entry points
├── Core/            — GameManager, EventBus, GameState machine, TimeManager (session/analytics timing)
├── Data/            — SaveManager (AES-256 encrypted), PlayerData
├── Analytics/       — Provider abstraction (Firebase + GameAnalytics)
├── Audio/           — AudioManager with config-driven music/sfx
├── Monetization/    — AdManager (AppLovin MAX), IAPManager (RevenueCat)
├── Notifications/   — NotificationManager (local push, Android + iOS)
├── StoreReview/     — RateUsManager (native store review prompt, Android + iOS)
├── VFX/             — VFXManager + UIBurstEffect (UI-space effect pooling)
├── ObjectPool/      — Generic component pooling, shared across VFX and gameplay
├── Gameplay/        — LevelManager, LevelDatabase, LevelConfig base
├── RemoteConfig/    — Firebase Remote Config wrapper
└── UI/              — UIManager screen stack, UIScreen base class

SortPuzzle (game-specific)

SortPuzzle/
├── Data/            — LevelData, TubeData, BoostConfig, DailyRewardConfig
├── Gameplay/        — PuzzleController, LevelController, TubeView, PourAnimator
├── Generation/      — LevelGenerator, LevelSolver (BFS, time-bounded)
├── Economy/         — GoldManager, BoostManager
├── DailyChallenge/  — DailyManager (local-time streak tracking, milestone rewards)
├── UI/Screens/      — All game screens
└── UI/Widgets/      — GoldCounterWidget, BoostBarWidget, StreakBadgeWidget


🔧 Tech Stack

SystemTechnologyEngineUnity 6Dependency InjectionVContainerAsyncUniTask (Cysharp)AdsAppLovin MAXIAP / SubscriptionsRevenueCatAnalyticsFirebase Analytics + GameAnalyticsRemote ConfigFirebase Remote Config — drives 10 live-tunable gameplay values with local fallbacksLocal NotificationsUnity Mobile Notifications (com.unity.mobile.notifications)Store Review PromptGoogle Play In-App Review + iOS SKStoreReviewControllerSave SystemAES-256 encrypted JSON, with automatic backup rotation and fallback on corrupted reads


⚙️ Key Systems

BFS Level Solver

Every generated level is validated with a breadth-first search solver before being saved. The solver returns the optimal solution path and par move count; levels that can't be solved, or that solve in fewer moves than the minimum par for their difficulty tier, are rejected and regenerated. The solver is wall-clock bounded (30s) rather than unbounded — a maximally-scrambled high-tube-count board can otherwise produce a reachable state space large enough to run for many minutes on an unlucky layout. Hitting the bound is treated identically to "proven unsolvable" by every caller, so this only affects the rare pathological case, not normal generation.

Procedural Level Generation

Levels are built by constructing a fully solved state, applying unrestricted random scramble moves (color-match constraints during scrambling are intentionally removed — they produce trivially easy levels), then validating with the BFS solver. Generation is parameterized (count, difficulty, capacity, and empty-tube count all as direct inputs, via an in-editor batch tool) rather than a fixed hardcoded curve — candidate levels are generated into a staging pool, played and curated by hand, then promoted into the shipped level databases. This decouples "content that exists" from "content that's actually in the campaign, in order," which is deliberately a manual curation step rather than an automated one.

Empty-tube count is guaranteed, not just requested: scrambling itself stays fully unrestricted (an earlier attempt to protect empty tubes during scrambling turned out to make generation impossible — every color tube starts completely full, so if the only empty tubes are also off-limits as destinations, there's no legal first move at all). Instead, a post-scramble step drains exactly N tubes back to empty, redistributing their contents into whatever room exists elsewhere. This is always exactly possible — total capacity across all tubes never changes, so emptying any N tubes is guaranteed to find enough slack in the rest.

Analytics & Live-Ops Tuning

Firebase Remote Config drives real gameplay tuning at runtime — interstitial cooldown/minimum-level/frequency, boost gold costs, the daily challenge reward, the starter-pack popup's trigger level, the rewarded-ad gold multiplier, and new-player starting balance. Every value has a local fallback, so the game behaves sensibly before any values are published remotely, or if a fetch fails.

Since this game has no lose-condition (no move limit, no enforced time limit), level abandonment is treated as the closest analytics equivalent to a "fail": leaving via the Home button, backgrounding the app, or quitting all report the same signal if the player made at least one move without winning. This matters specifically on Android, where exiting via the OS home button or app-switcher never routes through any in-game UI at all — OnApplicationPause/OnApplicationQuit are the only reliable hooks for that path. Guarded so a brief background-and-resume doesn't generate duplicate reports for a puzzle the player is still actively mid-attempt on; the guard clears on the next pour or restart, so a genuine later abandonment on the same level still gets reported. Restarts are tracked as their own distinct event (duration + moves for the attempt that just ended), kept separate from both completion and abandonment rather than blended into one running total across retries.

Object Pooling

A generic component pool (ObjectPoolManager) backs both ball animation (PourAnimator) and UI VFX (VFXManager) — instances are activated/deactivated rather than instantiated/destroyed, avoiding GC spikes during gameplay. Note: this project's UI is Screen Space - Overlay, which always renders on top of camera-rendered content, so VFX here are UI-native (pooled Image-based bursts), not ParticleSystem.

Event-Driven Architecture

All cross-system communication goes through a typed EventBus — no direct references between unrelated systems. Example flow:

LevelManager.CompleteCurrentLevel()
  → _gold.Add(reward)                   // GoldManager fires OnGoldChanged
  → _events.Publish(OnLevelCompleted)    // AnalyticsManager logs it; GameSceneEntryPoint
                                          //   drives daily-challenge completion and
                                          //   interstitial-ad cooldown checks
  → _game.TransitionTo(Win)              // GameSceneEntryPoint shows WinScreen, plays VFX

Encrypted Save System

SaveManager serializes PlayerData to JSON, encrypts with AES-256, and writes to Application.persistentDataPath. Every save rotates the previous file into a backup before writing; a failed/corrupted read on the primary file falls back to the backup automatically.

Daily Challenge System

Local-time daily lock (one play per calendar day, using the device's own clock rather than UTC, so the reset lines up with the player's actual day). 30-day rotating level pool. Consecutive-day streak tracking with milestone rewards at day 7, 14, 21, and 30. Streak-at-risk and daily-reminder local notifications scheduled around this on app open.


📱 Monetization

TypeStatusInterstitial adsCooldown + minimum-level + frequency gated, triggered on level completion — all three thresholds are Remote-Config-tunable (AdConfig values as local fallback)Rewarded ads3x gold on the win screen (non-daily wins only, shown only when a rewarded ad is actually ready) — watching the ad adds the extra multiplier's worth directly to the player's balance, not just the on-screen number. Multiplier itself is Remote-Config-tunable. Also grants an instant boost when a boost count hits zero mid-levelNo Ads IAPNon-consumable entitlement, checked before every interstitial/banner callGold packs & bundle tiersProduct ID structure defined in code (IAPManager.ProductIds); real store-side product IDs and RevenueCat configuration still pending before this goes liveDaily login bonusFlat gold once per local dayDaily challenge rewardRemote-Config-tunable, single source of truth across all four places it used to be hardcoded independentlyBoost economyThree paths: bought with gold via the Shop (Remote-Config-tunable pricing), earned as level-completion rewards, or granted via a rewarded ad when a boost count reaches zero mid-levelStarter pack popupAuto-opens the Shop once, after a Remote-Config-tunable level threshold, if unpurchased


🗂️ Project Structure

Assets/
├── Scenes/
│   ├── Bootstrap.unity        — App entry point, singleton services
│   ├── MainMenu.unity         — Main menu, shop, daily challenge
│   └── GameScene.unity        — Gameplay, win/fail screens
├── Scripts/
│   └── HyperBase/             — Reusable framework (see above)
├── SortPuzzle/
│   ├── Scripts/               — Game-specific code (see above)
│   ├── Prefabs/               — TubeView, BallSegment, UI widgets, VFX
│   └── Settings/
│       ├── Levels/            — Campaign LevelData ScriptableObjects
│       └── DailyLevels/       — 30 daily-challenge LevelData ScriptableObjects
└── Settings/                  — AdConfig, AudioConfig, LevelDatabase, VFXConfig


🛠️ Editor Tools

All accessible from the Unity menu bar under SortPuzzle/:

ToolMenu PathPurposeBatch Level GeneratorSortPuzzle/Generate LevelsGenerate N levels at a chosen difficulty, capacity, and guaranteed empty-tube count (campaign or daily target); candidates are solver-validated and staged for manual curationLevel EditorSortPuzzle/Level EditorHand-author or one-off auto-generate a single level, paint tubes directly, validate and saveDelete save fileSortPuzzle/Dev/Delete Save FileOpen save folderSortPuzzle/Dev/Open Save Folder

BootstrapInstaller also exposes a testing override toggle to swap in a staging LevelDatabase/DailyLevelDatabase for playtesting candidate levels without touching the production-wired references.


🚀 Getting Started

Prerequisites


Unity 6.x
Git LFS installed (git lfs install)


Setup

bashgit clone https://github.com/CanYalcin/BallSortPuzzle_Unity.git
cd BallSortPuzzle_Unity

Open the project in Unity Hub. Unity will import packages automatically.

SDK Credentials Required

Before running with live SDKs, add credentials for:


Firebase — drop google-services.json and GoogleService-Info.plist into Assets/
AppLovin MAX — Window → AppLovin → Integration Manager
RevenueCat — BootstrapInstaller._revenueCatApiKey in Bootstrap scene Inspector
GameAnalytics — Assets/GameAnalytics/Resources/Settings.asset


The game runs without these credentials in the Unity Editor — SDK calls are silently skipped.

First Run


Open Assets/Scenes/Bootstrap.unity
Press Play
The Bootstrap scene loads MainMenu automatically



🧭 Current Status

Core gameplay, economy, daily challenge, analytics, notifications, VFX, store-review, and remote-config/live-ops systems are implemented and covered by an EditMode test suite running in CI. Remote Config is fully wired end-to-end in code (10 keys, real consumers, local fallbacks verified) — the values just haven't been published to the Firebase Console yet, so the game is currently running entirely on its local fallback defaults, which is itself a live, working test of that fallback path. Ads and IAP infrastructure is fully built (AppLovin MAX / RevenueCat provider wrappers, cooldown-gated interstitial logic, rewarded-ad flows) but intentionally disabled via config (AdConfig.EnableAds = false) until real SDK credentials replace the current placeholder keys — ad-dependent UI (e.g. the win screen's 3x-gold option) correctly stays hidden as a result, not as a bug. A/B experiment infrastructure on top of Remote Config is planned but not yet started.

Next up: A/B testing infrastructure, performance profiling on real devices, store submission prep (live ad/IAP credentials, GDPR consent screen, store listing).


📊 Development Methodology

This project was built using an AI-assisted development workflow:


Architecture designed collaboratively with Claude (Anthropic)
All implementation generated via structured prompting with MCP (Model Context Protocol) direct Unity Editor integration
The developer directed all architectural decisions, reviewed every script, caught errors, made gameplay design calls, and iterated on systems
Demonstrates practical "AI as implementation partner" methodology for solo game development


This workflow allows a single developer to produce a codebase with professional architecture (DI, event bus, object pooling, encrypted persistence, provider pattern) in a fraction of the traditional time, while maintaining full understanding and ownership of the code.


📄 License

This project is available for portfolio viewing. Contact for licensing inquiries.


👤 Author

Built by Muhammed Can YALÇIN
canyalcin.work / linkedin.com/in/muhammedcanyalcin / muhammedcanyalcin@gmail.com