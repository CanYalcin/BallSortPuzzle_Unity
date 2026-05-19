⚠️ Critical (app crashes or corrupts saves without these)

VContainer Parent Scope — In both MainMenu and GameScene, select [GameInstaller] → VContainer LifetimeScope Inspector → set Parent = BootstrapInstaller
Change Encryption Salt — Data/EncryptionHelper.cs line: ProjectSalt = "..." — change before first production build

Required to monetise

AppLovin MAX keys — open Assets/Settings/AdConfig.asset, fill in SDK key + all 6 ad unit IDs
RevenueCat keys — Bootstrap scene → [Purchases] component → set Apple/Google API keys; update product IDs in IAPManager.cs
Firebase — drop google-services.json + GoogleService-Info.plist into Assets/StreamingAssets/, import SDK packages
GameAnalytics — GameAnalytics → Setup → enter Game Key + Secret Key

Required for content

Audio clips — Assets/Settings/AudioConfig.asset → assign 9 clips (SFX + BGM)
Game title — change "YOUR GAME" text on MainMenuScreen → Middle → GameTitle
Level configs — create LevelConfig assets for your levels and add to LevelDatabase

Your game

Add gameplay — put your game objects under [GameplayContent] in GameScene; call _levelManager.CompleteCurrentLevel() / FailCurrentLevel() from your code

Polish (can ship without, but looks placeholder)

Sprites/fonts — replace solid colour panels and default TMP font with your actual assets
VFX prefabs — assign particle systems in VFXConfig.asset (optional)


Reading Order for the Guides

SETUP_README.md first — step-by-step for everything above in order
ADS_IAP_GUIDE.md only if you want to understand the toggle system or use a non-default monetisation setup (e.g. ads-only, IAP-only, or disable specific ad types)