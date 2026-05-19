# HyperCasual Base Project
## Manual Setup Guide for Developers

> Everything listed here must be done **by hand** before the project will run.
> The scenes, scripts, prefabs, and wiring have all been generated automatically.
> This document covers only the remaining manual steps.

---

## QUICK-START CHECKLIST

- [ ] **1. VContainer Parent Scope** — Both scenes (CRITICAL — app crashes without this)
- [ ] **2. Encryption Salt** — Change before first production build (CRITICAL)
- [ ] **3. AppLovin MAX Keys** — SDK key + all Ad Unit IDs
- [ ] **4. RevenueCat** — API keys + update product IDs in code
- [ ] **5. Firebase** — Drop config files + import SDK packages
- [ ] **6. GameAnalytics** — Game Key + Secret Key
- [ ] **7. Audio Clips** — Assign SFX and music in AudioConfig
- [ ] **8. Level Configuration** — Create LevelConfig assets
- [ ] **9. VFX Prefabs** — Assign particle systems (optional)
- [ ] **10. Game Title** — Change "YOUR GAME" text
- [ ] **11. Visual Styling** — Replace color placeholders with real sprites/fonts
- [ ] **12. Add Gameplay** — Put your game objects under [GameplayContent]
- [ ] **13. AppLovin Mediation** — Install ad network adapters

---

## STEP 1 — VContainer Parent Scope ⚠️ CRITICAL

**Do this first. Without it the app throws NullReferenceException immediately on boot.**

Both `MainMenu.unity` and `GameScene.unity` contain a `[GameInstaller]` GameObject
that must be linked to the Bootstrap scope.

**For MainMenu.unity:**
1. Open `Assets/Scenes/MainMenu.unity`
2. Select `[GameInstaller]` in the Hierarchy
3. In the Inspector, find the **VContainer LifetimeScope** component
4. Find the `Parent` field
5. Click the circle picker → select **`BootstrapInstaller`**
   (it lives on `[Bootstrap]` in the Bootstrap scene)

**For GameScene.unity:**
1. Open `Assets/Scenes/GameScene.unity`
2. Select `[GameInstaller]`
3. Same steps — set `Parent` = `BootstrapInstaller`

> **Why:** VContainer's child scopes cannot inject singleton services from a parent
> scope unless they are explicitly linked. The Bootstrap scope holds all managers
> (GameManager, EventBus, AudioManager, etc.). Without the link, all [Inject]
> attributes on the screen scripts silently receive null references.

---

## STEP 2 — Encryption Salt ⚠️ CRITICAL

**Must be done before your FIRST production build.**
Changing the salt after users have save files will corrupt all existing saves permanently.

Open:
```
Assets/Scripts/HyperBase/Data/EncryptionHelper.cs
```

Find and change this line:
```csharp
// BEFORE:
private const string ProjectSalt = "HyperBase_ChangeMePerProject_2025!";

// AFTER — replace with any unique random string (20–40 chars):
private const string ProjectSalt = "MyGame_Xk9pQ2mT7vL3wN5r_2025";
```

Rules for choosing a salt:
- Must be unique to your game (not shared with other projects)
- Use letters, numbers, underscores — avoid quotes or backslashes
- Never commit it to a public repository
- Write it down somewhere safe — you cannot recover saves if you lose it

---

## STEP 3 — AppLovin MAX Keys

Open `Assets/Settings/AdConfig.asset` in the Inspector and fill in every field:

| Field | Where to find it |
|---|---|
| `Sdk Key` | AppLovin dashboard → Account → Keys |
| `Android_BannerAdUnitId` | AppLovin → Monetize → Ad Units → your Android banner |
| `Android_InterstitialAdUnitId` | AppLovin → Monetize → Ad Units → your Android interstitial |
| `Android_RewardedAdUnitId` | AppLovin → Monetize → Ad Units → your Android rewarded |
| `iOS_BannerAdUnitId` | AppLovin → Monetize → Ad Units → your iOS banner |
| `iOS_InterstitialAdUnitId` | AppLovin → Monetize → Ad Units → your iOS interstitial |
| `iOS_RewardedAdUnitId` | AppLovin → Monetize → Ad Units → your iOS rewarded |

**Optional tuning fields on AdConfig:**

| Field | Default | Description |
|---|---|---|
| `InterstitialCooldownSeconds` | 30 | Minimum seconds between two interstitials |
| `InterstitialMinLevel` | 3 | First level index where interstitials can show |
| `ShowBannerOnStart` | true | Auto-show banner after MAX initialises |

---

## STEP 4 — RevenueCat

### Set API Keys

1. Open `Assets/Scenes/Bootstrap.unity`
2. In the Hierarchy select `[Bootstrap] → [Purchases]`
3. On the **Purchases** component set:
   - `Revenue Cat API Key Apple` → your iOS public API key
   - `Revenue Cat API Key Google` → your Android public API key
4. Confirm `Use Runtime Setup` is **unchecked** (it should already be)

### Update Product IDs

Open `Assets/Scripts/HyperBase/Monetization/IAPManager.cs`
and replace the placeholder IDs with your actual App Store / Play Console product identifiers:

```csharp
public static class ProductIds
{
    public const string NoAds       = "com.YOURCOMPANY.YOURGAME.noads";
    public const string StarterPack = "com.YOURCOMPANY.YOURGAME.starterpack";
    public const string GemSmall    = "com.YOURCOMPANY.YOURGAME.gems_small";
    public const string GemMedium   = "com.YOURCOMPANY.YOURGAME.gems_medium";
    public const string GemLarge    = "com.YOURCOMPANY.YOURGAME.gems_large";
}
```

These strings must match **exactly** what you've set up in App Store Connect
and Google Play Console.

---

## STEP 5 — Firebase

### Create Project
1. Go to https://console.firebase.google.com
2. Create a new project (or use an existing one)
3. Add an **iOS app** and an **Android app** to the project
4. Use your game's bundle identifier (e.g. `com.yourcompany.yourgame`)

### Drop Config Files
Download both config files and place them at these exact paths:

| File | Destination |
|---|---|
| `google-services.json` | `Assets/StreamingAssets/google-services.json` |
| `GoogleService-Info.plist` | `Assets/StreamingAssets/GoogleService-Info.plist` |

### Import SDK Packages
Download the Firebase Unity SDK from https://firebase.google.com/docs/unity/setup
then in Unity import:
- `FirebaseAnalytics.unitypackage`
- `FirebaseRemoteConfig.unitypackage`

### Resolve Dependencies (Android only)
After importing, run:
```
Unity menu → Assets → External Dependency Manager → Android Resolver → Resolve All
```

---

## STEP 6 — GameAnalytics

1. Create a game at https://gameanalytics.com
2. In Unity open: `GameAnalytics → Setup`
3. Click **Select Platform Folder** and choose `Assets/`
4. Enter your **Game Key** and **Secret Key** for each platform (iOS + Android)

---

## STEP 7 — Audio Clips

Open `Assets/Settings/AudioConfig.asset` in the Inspector
and drag `AudioClip` assets into each slot:

| Field | What to assign |
|---|---|
| `Button Click` | Short tap/click sound for all UI buttons |
| `Coin Collect` | Coin or reward pickup sound |
| `Level Complete` | Win fanfare / jingle |
| `Level Fail` | Failure sting |
| `Purchase Success` | IAP success chime |
| `Reward Earned` | Rewarded ad reward granted sound |
| `Main Menu Music` | Looping background music for the main menu |
| `Gameplay Music` | Looping background music during gameplay |
| `Win Music` | Music that plays on the win screen |

Leave any field empty if you don't need that sound — the system
null-checks all clips before playing.

---

## STEP 8 — Level Configuration

A starter `Level_000.asset` (Level 1 → GameScene, 100 coins) already exists.

### To add more levels:

1. In the Project window, right-click `Assets/Settings/Levels/`
2. Select **Create → HyperBase → Level Config**
3. Name it `Level_001`, `Level_002`, etc.
4. In the Inspector set:

| Field | Description |
|---|---|
| `Level Index` | Must equal the array position in LevelDatabase (0-based) |
| `Display Name` | Text shown in the HUD (e.g. "Level 2") |
| `Scene Name` | Exact scene name in Build Settings (e.g. `GameScene`) |
| `Soft Currency Reward` | Coins awarded when this level is completed |
| `Difficulty Rating` | 1–10 scale (optional, for your own use) |
| `Time Limit` | 0 = no limit |

5. Open `Assets/Settings/Levels/LevelDatabase.asset`
6. Increase the `Levels` array size by 1
7. Drag the new `LevelConfig` asset into the new array slot

> **Important:** The level index in the LevelConfig asset must match its position
> in the array. Level at index 0 = Level_000, index 1 = Level_001, etc.
> The game loops back to index 0 after the last level.

---

## STEP 9 — VFX Particle Prefabs (Optional)

Open `Assets/Settings/VFXConfig.asset` and add entries for any effects you want.

For each entry:

| Field | Description |
|---|---|
| `Type` | Choose from the `VFXType` enum (see list below) |
| `Prefab` | A ParticleSystem prefab from your project |
| `Prewarm Count` | Instances to pre-pool at startup (default 3) |

Available VFX types: `CoinCollect`, `CoinExplosion`, `LevelComplete`, `LevelFail`,
`ButtonTap`, `StarBurst`, `Confetti`, `HitImpact`, `WinCelebration`

Unused types can be left empty — the VFXManager silently skips missing entries.

To play a VFX from your game code:
```csharp
[Inject] private VFXManager _vfx;

_vfx.Play(VFXType.CoinCollect, transform.position);
```

---

## STEP 10 — Game Title

The main menu currently shows the placeholder text **"YOUR GAME"**.

**Option A — Change in the scene:**
1. Open `Assets/Scenes/MainMenu.unity`
2. In the Hierarchy navigate to:
   `[Canvas] → [SafeArea] → MainMenuScreen → Middle → GameTitle`
3. In the Inspector find the `TextMeshProUGUI` component
4. Change the `Text` field to your game's name

**Option B — Change in the prefab (applies everywhere the prefab is used):**
1. In the Project window open `Assets/Prefabs/UI/Screens/MainMenuScreen.prefab`
2. Navigate to `Middle → GameTitle`
3. Change the `Text` field

---

## STEP 11 — Visual Styling

All UI elements currently use **solid colour placeholders** — no sprites or custom fonts.
To make the UI look like your game:

### Buttons
- Select any button (e.g. `PlayButton`, `RetryButton`, etc.)
- On the `Image` component → assign your button sprite
- Set `Image Type` to `Sliced` for scalable 9-patch buttons
- Optionally adjust the `Button → Colors` for hover/press states

### Backgrounds and Panels
- Select `Background` or `Header` panels under any screen
- On the `Image` component → assign your background sprite
- Or change the `Color` field to match your colour palette

### Fonts
- Select any `TextMeshProUGUI` component
- Assign your `TMP_FontAsset` to the `Font Asset` field
- To set a global default font:
  `Window → TextMeshPro → Settings → Default Font Asset`

### Currency Icons
Under `MainMenuScreen → Header`:
- `SoftCurrencyDisplay` — add a child `Image` for your coin icon
- `HardCurrencyDisplay` — add a child `Image` for your gem icon
Position them to the left of the `Counter` text object.

### Screen Backgrounds
Each screen has a `Background` Image child. Replace its colour or sprite:
- `MainMenuScreen/Background` — main menu dark bg
- `LoadingScreen/Background` — solid dark bg (typically kept dark)
- `WinScreen/Panel` and `FailScreen/Panel` — popup panels

---

## STEP 12 — Add Your Gameplay to GameScene

The `[GameplayContent]` GameObject in `GameScene` is the placeholder
for your actual game. Add your game objects as children of it.

```
[GameplayContent]       ← your game goes here
├── Board               (Match-3, puzzle grids...)
├── Player              (runners, characters...)
├── LevelRoot           (obstacles, environments...)
└── etc.
```

### Connecting to the Framework

Inject `LevelManager` into your gameplay MonoBehaviour via VContainer:

```csharp
using HyperBase.Gameplay;
using VContainer;

public class MyGameController : MonoBehaviour
{
    [Inject] private LevelManager _levelManager;

    // Call when the player wins:
    public void OnWin() => _levelManager.CompleteCurrentLevel();

    // Call when the player fails:
    public void OnFail() => _levelManager.FailCurrentLevel();

    // Call when retry button is pressed:
    public void OnRetry() => _levelManager.RetryCurrentLevel();
}
```

Register it in `GameInstaller.cs` (in the `Configure` method):
```csharp
builder.RegisterComponentInHierarchy<MyGameController>();
```

### Useful Events to Publish

Use the EventBus to communicate between systems without tight coupling:

```csharp
[Inject] private EventBus _events;

// Notify the UI that progress changed (updates the ProgressBar):
_events.Publish(new OnLevelProgressChanged(0.65f));  // 65% complete

// Trigger a VFX:
[Inject] private VFXManager _vfx;
_vfx.Play(VFXType.CoinCollect, coinTransform.position);

// Play a sound:
[Inject] private AudioManager _audio;
_audio.PlayCoinCollect();
```

---

## STEP 13 — AppLovin Mediation

1. In Unity open: `AppLovin → Integration Manager`
2. Install adapters for your chosen ad networks
   (Google AdMob, Meta Audience Network, Unity Ads, IronSource, etc.)
3. **iOS only:** Enable `SKAdNetwork` support in the MAX settings window
4. **Android:** Ensure `minSdkVersion` is 21 or higher
   (`Project Settings → Player → Android → Minimum API Level`)

---

## ADDING A NEW GAME TYPE

This framework supports multiple game types from a single codebase:

1. Duplicate `GameScene.unity` → rename it (e.g. `Match3.unity`, `Runner.unity`)
2. Add the new scene to `File → Build Settings`
3. Create a `LevelConfig` pointing to the new scene → add to `LevelDatabase`
4. In the new scene select `[GameInstaller]` → set **Parent = `BootstrapInstaller`**
5. Add your game objects under `[GameplayContent]`
6. Call `_levelManager.CompleteCurrentLevel()` / `FailCurrentLevel()` from gameplay

The Bootstrap scene and all 52 manager scripts are shared automatically.
Zero reconfiguration is needed per new game type.

---

## OPTIONAL — Local Push Notifications

To enable on-device push notifications:

1. Install **Unity Mobile Notifications** via Package Manager:
   `com.unity.mobile.notifications` (version 2.3 or higher)
2. Add scripting define symbol in:
   `Project Settings → Player → Scripting Define Symbols`
   Add: `HYPERBASE_NOTIFICATIONS`
3. The notification code in `NotificationManager.cs` activates automatically

---

## OPTIONAL — Debug Console

The `DebugConsole` (5-tap cheat overlay) is already set up on `[Bootstrap]/[DebugConsole]`.

- **Active in:** `UNITY_EDITOR` and `DEVELOPMENT_BUILD`
- **Stripped from:** release builds (no action needed)

Available cheats: +1000 Soft Currency, +100 Hard Currency,
Complete Level, Fail Level, Retry Level, Save, Delete Save.

Activate it during a play session by **tapping 5 times anywhere on screen within 2 seconds**.

---

## KEY FILE LOCATIONS

| System | File | Notes |
|---|---|---|
| App startup sequence | `Bootstrap/BootstrapEntryPoint.cs` | Init order for all services |
| DI registrations | `Bootstrap/BootstrapInstaller.cs` | All singleton registrations |
| Scene / state → UI | `Bootstrap/GameSceneEntryPoint.cs` | State machine → screen transitions |
| Per-scene DI scope | `Bootstrap/GameInstaller.cs` | Screen instances registered here |
| Game state machine | `Core/GameManager.cs` | States: Boot / MainMenu / Gameplay / Win / Fail / Pause |
| All event structs | `Core/GameEvents.cs` | OnLevelStarted, OnCurrencyChanged, etc. |
| Player data model | `Data/PlayerData.cs` | All serialised save fields |
| **Encryption salt** | `Data/EncryptionHelper.cs` | ⚠ MUST change before first production build |
| **IAP product IDs** | `Monetization/IAPManager.cs` | ⚠ Update with your store product IDs |
| Ad manager | `Monetization/AdManager.cs` | Banner / Interstitial / Rewarded via MAX |
| Level progression | `Gameplay/LevelManager.cs` | Complete / Fail / Retry |
| Currency mutations | `Currency/CurrencyManager.cs` | Add / TrySpend / SetAmount |
| Audio (SFX + BGM) | `Audio/AudioManager.cs` | PlaySfx, PlayMusic, crossfade |
| UI screen base | `UI/UIScreen.cs` | Fade / scale transitions via CanvasGroup |
| Analytics | `Analytics/AnalyticsManager.cs` | Fan-out to Firebase + GameAnalytics |

---

## SCENE OVERVIEW

### Bootstrap.unity (index 0)
```
[Bootstrap]                         DontDestroyOnLoad
├── BootstrapInstaller              VContainer root scope — all refs wired
├── ObjectPoolManager
├── ApplicationLifecycleHandler     Save on pause/quit
├── [Purchases]                     RevenueCat — set API keys here
└── [DebugConsole]                  Dev-only 5-tap overlay
```

### MainMenu.unity (index 1)
```
[GameInstaller]    ← set Parent = BootstrapInstaller  ⚠
[EventSystem]
[Canvas]           1080×1920, ScaleWithScreenSize, match 0.5
  └── [SafeArea]   Respects notch + home indicator
       ├── MainMenuScreen   ACTIVE  — Header / Middle / Footer
       ├── GameplayScreen   inactive
       ├── WinScreen        inactive
       ├── FailScreen       inactive
       ├── LoadingScreen    inactive
       └── SettingsScreen   inactive
```

### GameScene.unity (index 2)
```
[GameInstaller]    ← set Parent = BootstrapInstaller  ⚠
[EventSystem]
[Canvas]           1080×1920, ScaleWithScreenSize, match 0.5
  └── [SafeArea]
       ├── MainMenuScreen   inactive
       ├── GameplayScreen   ACTIVE  ← game HUD visible on load
       ├── WinScreen        inactive
       ├── FailScreen       inactive
       ├── LoadingScreen    inactive
       └── SettingsScreen   inactive
[GameplayContent]  ← add your game objects here
```

---

*HyperCasual Base Project — generated by Claude (Anthropic) using MCP for Unity*
*Stack: Unity 6 · VContainer · AppLovin MAX · RevenueCat · Firebase · GameAnalytics · UniTask*
