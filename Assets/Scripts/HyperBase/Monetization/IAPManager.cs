using Cysharp.Threading.Tasks;
using HyperBase.Core;
using UnityEngine;
using VContainer;

namespace HyperBase.Monetization
{
    public class IAPManager
    {
        public static class ProductIds
        {
            // Standalone
            public const string NoAds          = "com.yourcompany.game.noads";

            // Gold packs
            public const string Gold1000       = "com.yourcompany.game.gold_1000";
            public const string Gold5000       = "com.yourcompany.game.gold_5000";
            public const string Gold10000      = "com.yourcompany.game.gold_10000";
            public const string Gold25000      = "com.yourcompany.game.gold_25000";

            // Bundle packages
            public const string PackStarter    = "com.yourcompany.game.pack_starter";  // one-time
            public const string PackSmall      = "com.yourcompany.game.pack_small";
            public const string PackMid        = "com.yourcompany.game.pack_mid";
            public const string PackBig        = "com.yourcompany.game.pack_big";
            public const string PackMega       = "com.yourcompany.game.pack_mega";
            public const string PackPremium    = "com.yourcompany.game.pack_premium";
        }
        public static class EntitlementIds { public const string Premium = "premium"; }

        private readonly EventBus _eventBus;
        private Purchases _sdk;
        private bool _isInitialized;

        [Inject]
        public IAPManager(EventBus eventBus) => _eventBus = eventBus;

        public void Initialize(string apiKey = null)
        {
            _sdk = Object.FindAnyObjectByType<Purchases>();
            if (_sdk == null) { Debug.LogError("[IAPManager] Purchases component not found."); return; }
            if (!string.IsNullOrEmpty(apiKey))
            {
#if UNITY_IOS
                _sdk.revenueCatAPIKeyApple = apiKey;
#elif UNITY_ANDROID
                _sdk.revenueCatAPIKeyGoogle = apiKey;
#endif
            }
            if (Debug.isDebugBuild) _sdk.SetLogLevel(Purchases.LogLevel.Verbose);
            _isInitialized = true;
            Debug.Log("[IAPManager] RevenueCat ready.");
        }

        public async UniTask<bool> PurchaseAsync(string productId)
        {
            if (!EnsureReady()) return false;
            var tcs = new UniTaskCompletionSource<bool>();
            _sdk.GetOfferings((offerings, offeringsError) =>
            {
                if (offeringsError != null) { tcs.TrySetResult(false); return; }
                Purchases.Package pkg = FindPackage(offerings, productId);
                if (pkg == null) { tcs.TrySetResult(false); return; }
                // Probe: MakePurchaseFunc with 1 arg
                _sdk.PurchasePackage(pkg, (result) =>
                {
                    if (result.Error != null)  // or result.UserCancelled
                    {
                        _eventBus.Publish(new OnPurchaseFailed(productId, result.Error?.Message ?? "Purchase failed"));
                        tcs.TrySetResult(false);
                    }
                    else
                    {
                        _eventBus.Publish(new OnPurchaseCompleted(productId));
                        tcs.TrySetResult(true);
                    }
                });
            });
            return await tcs.Task;
        }

        public async UniTask<bool> RestorePurchasesAsync()
        {
            if (!EnsureReady()) return false;
            var tcs = new UniTaskCompletionSource<bool>();
            _sdk.RestorePurchases((customerInfo, error) =>
            {
                tcs.TrySetResult(error == null);
            });
            return await tcs.Task;
        }

        public async UniTask<bool> HasEntitlementAsync(string entitlementId = EntitlementIds.Premium)
        {
            if (!EnsureReady()) return false;
            var tcs = new UniTaskCompletionSource<bool>();
            _sdk.GetCustomerInfo((customerInfo, error) =>
            {
                if (error != null) { tcs.TrySetResult(false); return; }
                bool active = customerInfo?.Entitlements?.Active?.ContainsKey(entitlementId) == true;
                tcs.TrySetResult(active);
            });
            return await tcs.Task;
        }

        private bool EnsureReady()
        {
            if (_isInitialized && _sdk != null) return true;
            Debug.LogError("[IAPManager] Not initialized.");
            return false;
        }

        private static Purchases.Package FindPackage(Purchases.Offerings offerings, string productId)
        {
            if (offerings?.All == null) return null;
            foreach (var offering in offerings.All.Values)
            {
                if (offering?.AvailablePackages == null) continue;
                foreach (var pkg in offering.AvailablePackages)
                    if (pkg?.StoreProduct?.Identifier == productId) return pkg;
            }
            return null;
        }
    }
}
