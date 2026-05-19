# Ads & IAP — Activation Guide

All controls live in a single asset:
**`Assets/Settings/AdConfig.asset`**

Open it in the Inspector and use the checkboxes described below.
No code changes are required for any of the scenarios listed here.

---

## The Six Toggle Fields

| Field | What it controls |
|---|---|
| `Enable Ads` | Master switch — entire AppLovin MAX SDK |
| `Enable IAP` | Master switch — entire RevenueCat SDK |
| `Enable Banner` | Banner ad at bottom of screen |
| `Enable Interstitial` | Full-screen ad between levels |
| `Enable Rewarded` | Rewarded ads (double reward, continue after fail) |

> `Enable Banner`, `Enable Interstitial`, and `Enable Rewarded` only matter
> when `Enable Ads` is also checked. If `Enable Ads` is off, all three are
> ignored regardless of their state.

---

## Common Scenarios

### Ads + IAP (default — both enabled)
```
Enable Ads  ✅
Enable IAP  ✅
```
- MAX SDK initialises on startup
- RevenueCat initialises on startup
- "NO ADS" button is visible on the main menu
- Interstitial fires after levels (respecting cooldown + min level)
- Rewarded buttons appear on Win and Fail screens when an ad is loaded

---

### Ads only — no IAP
```
Enable Ads  ✅
Enable IAP  ☐
```
- MAX SDK initialises normally
- RevenueCat SDK is **never initialised** (no network calls, no SDK overhead)
- "NO ADS" button is **automatically hidden** on the main menu
- All ad types still work normally (subject to their own toggles below)

---

### IAP only — no ads
```
Enable Ads  ☐
Enable IAP  ✅
```
- MAX SDK is **never initialised**
- RevenueCat initialises normally for other IAP products
- No banner, no interstitials, no rewarded ads — ever
- "NO ADS" button is visible (since IAP is active)
- The WinScreen "2x REWARD" button and FailScreen "CONTINUE" button
  are automatically hidden (they call `IsRewardedReady()` which returns
  false when ads are disabled)

---

### Neither — completely free game
```
Enable Ads  ☐
Enable IAP  ☐
```
- Neither SDK is initialised
- "NO ADS" button is hidden
- All reward/continue buttons are hidden
- Zero monetisation overhead at runtime

---

### Ads on, but disable specific ad types

You can disable individual ad types while keeping the MAX SDK alive:

**No banner, interstitials + rewarded only:**
```
Enable Ads          ✅
Enable Banner       ☐
Enable Interstitial ✅
Enable Rewarded     ✅
```

**Rewarded only (no banner, no interstitials):**
```
Enable Ads          ✅
Enable Banner       ☐
Enable Interstitial ☐
Enable Rewarded     ✅
```

**No rewarded (banner + interstitials only):**
```
Enable Ads          ✅
Enable Banner       ✅
Enable Interstitial ✅
Enable Rewarded     ☐
```
When `Enable Rewarded` is off:
- `IsRewardedReady()` always returns `false`
- The "2x REWARD" button on WinScreen is hidden
- The "CONTINUE" button on FailScreen is hidden

---

## What Happens at Runtime

When `Enable Ads = false`:
- `BootstrapEntryPoint.RunInitAsync()` skips the entire `_ads.Initialize()` call
- MAX SDK is never touched — no SDK key set, no network calls

When `Enable IAP = false`:
- `BootstrapEntryPoint.RunInitAsync()` skips the entire `_iap.Initialize()` call
- RevenueCat SDK is never touched
- The "NO ADS" button on MainMenuScreen is hidden automatically on `BeforeShow`

When `Enable Banner = false`:
- `AdManager.ShowBanner()` returns immediately
- `AdManager.Initialize()` skips banner creation inside the MAX callback

When `Enable Interstitial = false`:
- `AdManager.TryShowInterstitial()` returns immediately
- `AdManager.CanShowInterstitial()` returns `false`
- No interstitial is loaded at startup

When `Enable Rewarded = false`:
- `AdManager.IsRewardedReady()` returns `false`
- `AdManager.ShowRewarded()` logs a warning and calls `onComplete(false)`
- No rewarded ad is loaded at startup

---

## Handling the "No Ads" Purchase at Runtime

When a player purchases "No Ads" via IAP:

1. `IAPManager.PurchaseAsync()` completes successfully
2. `GameSceneEntryPoint` receives `OnPurchaseCompleted` and publishes `OnNoAdsActivated`
3. `AdManager.ActivateNoAds()` is called — banner is destroyed, interstitials
   and rewarded are silently blocked for the rest of the session
4. `PlayerData.IsNoAds` is set to `true` and saved
5. On the next app launch, `BootstrapEntryPoint` reads `IsNoAds = true` and
   calls `_ads.ActivateNoAds()` immediately after MAX initialises

The "NO ADS" button is also hidden automatically via the `OnNoAdsActivated` event
subscribed in `MainMenuScreen`.

> **Note:** Even with `IsNoAds = true` saved, the MAX SDK still initialises.
> This is intentional — MAX requires initialisation before any calls can be made,
> and `ActivateNoAds()` then blocks all ad display immediately after.
> If you want to skip MAX entirely for no-ads users, you can add a check in
> `BootstrapEntryPoint` step 8:
> ```csharp
> if (_ads.Config.EnableAds && !_save.Data.IsNoAds)
>     _ads.Initialize();
> ```

---

## Ad Unit IDs per Platform

The AdConfig asset automatically resolves the correct ad unit ID at build time:

```
Android build → uses Android_BannerAdUnitId / Android_InterstitialAdUnitId / Android_RewardedAdUnitId
iOS build     → uses iOS_BannerAdUnitId     / iOS_InterstitialAdUnitId     / iOS_RewardedAdUnitId
```

You must fill in **both sets** of IDs even if you only ship on one platform,
because Unity evaluates both at compile time.

---

## Interstitial Capping Fields

These only apply when `Enable Interstitial = true`:

| Field | Default | Description |
|---|---|---|
| `Interstitial Cooldown Seconds` | 30 | Minimum gap in seconds between two interstitials |
| `Interstitial Min Level` | 3 | Level index at which interstitials start showing (0-based, so 3 = 4th level) |

---

## Quick Reference — AdConfig.asset Location

```
Assets/
└── Settings/
    └── AdConfig.asset   ← open this in the Inspector
```

All changes take effect immediately on the next app launch (or next
`BootstrapEntryPoint.StartAsync()` call in Play Mode).

