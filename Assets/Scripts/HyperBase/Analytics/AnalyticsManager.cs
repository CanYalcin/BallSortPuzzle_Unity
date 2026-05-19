using System;
using System.Collections.Generic;
using HyperBase.Core;
using UnityEngine;
using VContainer;

namespace HyperBase.Analytics
{
    /// <summary>
    /// Fan-out analytics hub. Registers providers and auto-wires EventBus events
    /// so game logic never imports analytics directly.
    /// </summary>
    public class AnalyticsManager
    {
        private readonly List<IAnalyticsProvider> _providers = new();
        private readonly EventBus _events;

        [Inject]
        public AnalyticsManager(EventBus events) => _events = events;

        public void RegisterProvider(IAnalyticsProvider p)
        {
            p.Initialize();
            _providers.Add(p);
            Debug.Log($"[Analytics] Registered: {p.GetType().Name}");
        }

public void SubscribeToEvents()
        {
            // Core level events
            _events.Subscribe<OnLevelStarted>    (e => LogLevelStart   (e.LevelIndex));
            _events.Subscribe<OnLevelCompleted>  (e => LogLevelComplete(e.LevelIndex, e.CompletionTime, 0));
            _events.Subscribe<OnLevelFailed>     (e => LogLevelFail    (e.LevelIndex, 0f));

            // Ad events
            _events.Subscribe<OnAdShown>          (e => LogAdShown    (e.AdType.ToString(), "auto"));
            _events.Subscribe<OnAdCompleted>      (e => LogAdCompleted(e.AdType.ToString(), "auto", e.Success));

            // IAP
            _events.Subscribe<OnPurchaseCompleted>(e => LogPurchase(e.ProductId, 0, "USD"));

            // Boost events
            _events.Subscribe<SortPuzzle.OnBoostUsed>    (e => LogEvent("boost_used",
                new Dictionary<string, object> { { "type", e.Type.ToString() }, { "remaining", e.Remaining } }));
            _events.Subscribe<SortPuzzle.OnBoostGranted> (e => LogEvent("boost_granted",
                new Dictionary<string, object> { { "type", e.Type.ToString() }, { "count", e.CountGranted }, { "total", e.NewTotal } }));

            // Gold events
            _events.Subscribe<SortPuzzle.OnGoldChanged>  (e =>
            {
                if (e.Delta > 0) LogCurrencyEarned("gold", e.Delta, e.Source);
                else             LogCurrencySpent ("gold", -e.Delta, e.Source);
            });

            // Daily challenge events
            _events.Subscribe<SortPuzzle.OnDailyChallengeStarted>   (e => LogEvent("daily_challenge_started",
                new Dictionary<string, object> { { "date_key", e.DateKey }, { "streak_before", e.StreakBefore } }));
            _events.Subscribe<SortPuzzle.OnDailyChallengeCompleted>(e => LogEvent("daily_challenge_completed",
                new Dictionary<string, object> { { "day_index", e.DayIndex }, { "streak", e.NewStreakDays }, { "gold", e.GoldEarned } }));
            _events.Subscribe<SortPuzzle.OnStreakMilestoneReached> (e => LogEvent("streak_milestone_reached",
                new Dictionary<string, object> { { "streak_day", e.StreakDay } }));
            _events.Subscribe<SortPuzzle.OnStreakBroken>            (e => LogEvent("streak_broken",
                new Dictionary<string, object> { { "previous_streak", e.PreviousStreak } }));
        }

        public void LogLevelStart    (int idx)                                    => Fan(p => p.LogLevelStart(idx));
        public void LogLevelComplete (int idx, float dur, int soft)               => Fan(p => p.LogLevelComplete(idx, dur, soft));
        public void LogLevelFail     (int idx, float dur)                         => Fan(p => p.LogLevelFail(idx, dur));
        public void LogAdShown       (string t, string pl)                        => Fan(p => p.LogAdShown(t, pl));
        public void LogAdCompleted   (string t, string pl, bool r)                => Fan(p => p.LogAdCompleted(t, pl, r));
        public void LogPurchase      (string id, double price, string cur)        => Fan(p => p.LogPurchase(id, price, cur));
        public void LogCurrencyEarned(string t, int amt, string src)              => Fan(p => p.LogCurrencyEarned(t, amt, src));
        public void LogCurrencySpent (string t, int amt, string snk)              => Fan(p => p.LogCurrencySpent(t, amt, snk));
        public void LogEvent         (string name, Dictionary<string, object> ps) => Fan(p => p.LogEvent(name, ps));
        public void SetUserProperty  (string name, string val)                    => Fan(p => p.SetUserProperty(name, val));

        private void Fan(Action<IAnalyticsProvider> action)
        {
            foreach (var p in _providers)
            {
                try   { action(p); }
                catch (Exception e) { Debug.LogError($"[Analytics] Error in {p.GetType().Name}: {e.Message}"); }
            }
        }
    }
}
