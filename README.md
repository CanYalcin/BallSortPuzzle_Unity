[![Unity Tests](https://github.com/CanYalcin/BallSortPuzzle_Unity/actions/workflows/unity-ci.yml/badge.svg)](https://github.com/CanYalcin/BallSortPuzzle_Unity/actions/workflows/unity-ci.yml)

# Ball Sort Puzzle

A hypercasual mobile puzzle game built in Unity 6, developed using an AI-assisted engineering workflow. The project demonstrates a complete, commercially-structured game architecture — dependency injection, event-driven systems, encrypted persistence, provider-pattern monetization and analytics, procedural content generation with automated solvability validation, and CI-tested gameplay logic.

> **Note on development:** This project was built using an AI-assisted development methodology. All architecture decisions, design choices, debugging, and quality control were directed by the developer. The AI served as an implementation partner, not an author.

---

## 🎮 Gameplay

Classic ball sort puzzle — move colored balls between tubes until each tube contains only one color. Simple to learn, satisfying to master.

- Tap a tube to select it, tap another to pour
- Complete all tubes to win the level
- 30-level campaign, procedurally generated and solver-validated
- Daily challenge with a 30-day rotating level pool and streak rewards
- 2 boost types: Undo and Extra Empty Tube, earned from level rewards or via rewarded ads

---

## 🏗️ Architecture

The project is split into two layers:

### HyperBase (reusable framework)
A game-agnostic mobile game framework that can be dropped into any hypercasual project:

```
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
```

### SortPuzzle (game-specific)
```
SortPuzzle/
├── Data/            — LevelData, TubeData, BoostConfig, DailyRewardConfig
├── Gameplay/        — PuzzleController, LevelController, TubeView, PourAnimator
├── Generation/      — LevelGenerator, LevelSolver (BFS, time-bounded)
├── Economy/         — GoldManager, BoostManager
├── DailyChallenge/  — DailyManager (local-time streak tracking, milestone rewards)
├── UI/Screens/      — All game screens
└── UI/Widgets/      — GoldCounterWidget, BoostBarWidget, StreakBadgeWidget
```

---

## 🔧 Tech Stack

| System | Technology |
|---|---|
| Engine | Unity 6 |
| Dependency Injection | VContainer |
| Async | UniTask (Cysharp) |
| Ads | AppLovin MAX |
| IAP / Subscriptions | RevenueCat |
| Analytics | Firebase Analytics + GameAnalytics |
| Remote Config | Firebase Remote Config |
| Local Notifications | Unity Mobile Notifications (`com.unity.mobile.notifications`) |
| Store Review Prompt | Google Play In-App Review + iOS `SKStoreReviewController` |
| Save System | AES-256 encrypted JSON, with automatic backup rotation and fallback on corrupted reads |

---

## ⚙️ Key Systems

### BFS Level Solver
Every generated level is validated with a breadth-first search solver before being saved. The solver returns the optimal solution path and par move count; levels that can't be solved, or that solve in fewer moves than the minimum par for their difficulty tier, are rejected and regenerated. The solver is wall-clock bounded (30s) rather than unbounded — a maximally-scrambled high-tube-count board can otherwise produce a reachable state space large enough to run for many minutes on an unlucky layout. Hitting the bound is treated identically to "proven unsolvable" by every caller, so this only affects the rare pathological case, not normal generation.

### Procedural Level Generation
Levels are built by constructing a fully solved state, applying unrestricted random scramble moves (color-match constraints during scrambling are intentionally removed — they produce trivially easy levels), then validating with the BFS solver. Generation is parameterized (count, difficulty, capacity, and empty-tube count all as direct inputs, via an in-editor batch tool) rather than a fixed hardcoded curve — candidate levels are generated into a staging pool, played and curated by hand, then promoted into the shipped level databases. This decouples "content that exists" from "content that's actually in the campaign, in order," which is deliberately a manual curation step rather than an automated one.

Empty-tube count is guaranteed, not just requested: scrambling itself stays fully unrestricted (an earlier attempt to protect empty tubes *during* scrambling turned out to make generation impossible — every color tube starts completely full, so if the only empty tubes are also off-limits as destinations, there's no legal first move at all). Instead, a post-scramble step drains exactly N tubes back to empty, redistributing their contents into whatever room exists elsewhere. This is always exactly possible — total capacity across all tubes never changes, so emptying any N tubes is guaranteed to find enough slack in the rest.

### Object Pooling
A generic component pool (`ObjectPoolManager`) backs both ball animation (`PourAnimator`) and UI VFX (`VFXManager`) — instances are activated/deactivated rather than instantiated/destroyed, avoiding GC spikes during gameplay. Note: this project's UI is Screen Space - Overlay, which always renders on top of camera-rendered content, so VFX here are UI-native (pooled `Image`-based bursts), not `ParticleSystem`.

### Event-Driven Architecture
All cross-system communication goes through a typed `EventBus` — no direct references between unrelated systems. Example flow:
```
LevelManager.CompleteCurrentLevel()
  → _gold.Add(reward)                   // GoldManager fires OnGoldChanged
  → _events.Publish(OnLevelCompleted)    // AnalyticsManager logs it; GameSceneEntryPoint
                                          //   drives daily-challenge completion and
                                          //   interstitial-ad cooldown checks
  → _game.TransitionTo(Win)              // GameSceneEntryPoint shows WinScreen, plays VFX
```

### Encrypted Save System
`SaveManager` serializes `PlayerData` to JSON, encrypts with AES-256, and writes to `Application.persistentDataPath`. Every save rotates the previous file into a backup before writing; a failed/corrupted read on the primary file falls back to the backup automatically.

### Daily Challenge System
Local-time daily lock (one play per calendar day, using the device's own clock rather than UTC, so the reset lines up with the player's actual day). 30-day rotating level pool. Consecutive-day streak tracking with milestone rewards at day 7, 14, 21, and 30. Streak-at-risk and daily-reminder local notifications scheduled around this on app open.

---

## 📱 Monetization

| Type | Status |
|---|---|
| Interstitial ads | Cooldown + minimum-level gated (30s cooldown, no earlier than level 3 by default — tunable via `AdConfig`), triggered on level completion |
| Rewarded ads | 3x gold on the win screen (non-daily wins only, shown only when a rewarded ad is actually ready) — watching the ad adds the extra 2x directly to the player's balance, not just the on-screen number. Also grants an instant boost when a boost count hits zero mid-level |
| No Ads IAP | Non-consumable entitlement, checked before every interstitial/banner call |
| Gold packs & bundle tiers | Product ID structure defined in code (`IAPManager.ProductIds`); real store-side product IDs and RevenueCat configuration still pending before this goes live |
| Daily login bonus | Flat gold once per local day |
| Boost economy | Three paths: bought with gold via the Shop, earned as level-completion rewards, or granted via a rewarded ad when a boost count reaches zero mid-level |

---

## 🗂️ Project Structure

```
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
```

---

## 🛠️ Editor Tools

All accessible from the Unity menu bar under `SortPuzzle/`:

| Tool | Menu Path | Purpose |
|---|---|---|
| Batch Level Generator | `SortPuzzle/Generate Levels` | Generate N levels at a chosen difficulty, capacity, and guaranteed empty-tube count (campaign or daily target); candidates are solver-validated and staged for manual curation |
| Level Editor | `SortPuzzle/Level Editor` | Hand-author or one-off auto-generate a single level, paint tubes directly, validate and save |
| Delete save file | `SortPuzzle/Dev/Delete Save File` | |
| Open save folder | `SortPuzzle/Dev/Open Save Folder` | |

`BootstrapInstaller` also exposes a testing override toggle to swap in a staging `LevelDatabase`/`DailyLevelDatabase` for playtesting candidate levels without touching the production-wired references.

---

## 🚀 Getting Started

### Prerequisites
- Unity 6.x
- Git LFS installed (`git lfs install`)

### Setup
```bash
git clone https://github.com/CanYalcin/BallSortPuzzle_Unity.git
cd BallSortPuzzle_Unity
```
Open the project in Unity Hub. Unity will import packages automatically.

### SDK Credentials Required
Before running with live SDKs, add credentials for:
- **Firebase** — drop `google-services.json` and `GoogleService-Info.plist` into `Assets/`
- **AppLovin MAX** — Window → AppLovin → Integration Manager
- **RevenueCat** — `BootstrapInstaller._revenueCatApiKey` in Bootstrap scene Inspector
- **GameAnalytics** — `Assets/GameAnalytics/Resources/Settings.asset`

The game runs without these credentials in the Unity Editor — SDK calls are silently skipped.

### First Run
1. Open `Assets/Scenes/Bootstrap.unity`
2. Press Play
3. The Bootstrap scene loads MainMenu automatically

---

## 🧭 Current Status

Core gameplay, economy, daily challenge, analytics, notifications, VFX, and store-review systems are implemented and covered by an EditMode test suite running in CI. Ads and IAP infrastructure is fully built (AppLovin MAX / RevenueCat provider wrappers, cooldown-gated interstitial logic, rewarded-ad flows) but intentionally disabled via config (`AdConfig.EnableAds = false`) until real SDK credentials replace the current placeholder keys — ad-dependent UI (e.g. the win screen's 3x-gold option) correctly stays hidden as a result, not as a bug. Remote Config-driven live-ops tuning and A/B experiment infrastructure are planned but not yet started.

Next up: production-readiness pass (live ad/IAP credentials, performance profiling on real devices, store listing prep).

---

## 📊 Development Methodology

This project was built using an **AI-assisted development workflow**:

- Architecture designed collaboratively with Claude (Anthropic)
- All implementation generated via structured prompting with MCP (Model Context Protocol) direct Unity Editor integration
- The developer directed all architectural decisions, reviewed every script, caught errors, made gameplay design calls, and iterated on systems
- Demonstrates practical "AI as implementation partner" methodology for solo game development

This workflow allows a single developer to produce a codebase with professional architecture (DI, event bus, object pooling, encrypted persistence, provider pattern) in a fraction of the traditional time, while maintaining full understanding and ownership of the code.

---

## 📄 License

This project is available for portfolio viewing. Contact for licensing inquiries.

---

## 👤 Author

Built by Muhammed Can YALÇIN
canyalcin.work / linkedin.com/in/muhammedcanyalcin / muhammedcanyalcin@gmail.com
