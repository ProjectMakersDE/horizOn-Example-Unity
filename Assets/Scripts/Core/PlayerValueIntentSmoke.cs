using System;
using UnityEngine;

namespace SeagullStorm
{
    /// <summary>Invoked only by the opt-in smoke player, using fresh, exclusively owned fixture keys.</summary>
    public static class PlayerValueIntentSmoke
    {
        public static void Check()
        {
            string user = "smoke-" + Guid.NewGuid().ToString("N");
            var store = new PlayerValueIntentStore("fixture.invalid", "project-a", user);
            var other = new PlayerValueIntentStore("fixture.invalid", "project-b", user);
            string legacy = DurableIntentScope.LegacyKey("old-region.invalid", "project-a", user, "seagull.player-values.pending.");
            string conflict = DurableIntentScope.LegacyKey("other-region.invalid", "project-a", user, "seagull.player-values.pending.");
            string guestProject = "fixture-project-" + user;
            var value = new PlayerValueIntent { userId = user, kind = "purchase", offerKey = "energy-pack", operationId = "1792065600000:" + Guid.NewGuid().ToString("D") };
            try
            {
                store.Reserve(value);
                PlayerPrefs.SetString(legacy, PlayerPrefs.GetString(store.StorageKey)); PlayerPrefs.DeleteKey(store.StorageKey); PlayerPrefs.Save();
                var restored = new PlayerValueIntentStore("new-region.invalid", "project-a", user, new[] { "old-region.invalid", "new-region.invalid" });
                if (!restored.TryRead(out var read) || read.operationId != value.operationId || read.offerKey != value.offerKey)
                    throw new Exception("Restart lost the original action");
                if (PlayerPrefs.HasKey(legacy) || restored.StorageKey != store.StorageKey) throw new Exception("Regional host split the pending action");
                if (!other.TryRead(out var unrelated) || unrelated != null) throw new Exception("Project intent leaked");
                bool blocked = false;
                try { restored.Reserve(value); } catch (InvalidOperationException) { blocked = true; }
                if (!blocked) throw new Exception("A new action overwrote a pending intent");
                blocked = false;
                try { restored.Confirm("different-operation"); } catch (InvalidOperationException) { blocked = true; }
                if (!blocked || !restored.TryRead(out read) || read.operationId != value.operationId)
                    throw new Exception("Another response cleared the original action");
                string original = PlayerPrefs.GetString(store.StorageKey);
                PlayerPrefs.SetString(store.StorageKey, "{invalid");
                if (restored.TryRead(out _)) throw new Exception("Unreadable intent was accepted");
                blocked = false;
                try { restored.Reserve(value); } catch (InvalidOperationException) { blocked = true; }
                if (!blocked) throw new Exception("Unreadable intent was overwritten");
                PlayerPrefs.SetString(store.StorageKey, original);
                int first = restored.MarkAttempt(value.operationId);
                if (restored.RejectFirstUnbooked(value.operationId, first, 2, "INSUFFICIENT_BALANCE", true)) throw new Exception("Internal retries released an uncertain original action");
                if (restored.RejectFirstUnbooked(value.operationId, first, 1, "OPERATION_EXPIRED", true)) throw new Exception("Expired operation was released");
                int retry = restored.MarkAttempt(value.operationId);
                if (restored.RejectFirstUnbooked(value.operationId, retry, 1, "INSUFFICIENT_BALANCE", true)) throw new Exception("An earlier attempt was forgotten");
                restored.Confirm(value.operationId);
                if (!restored.TryRead(out read) || read != null) throw new Exception("Confirmed action was not cleared");
                value.operationId += "-fresh"; value.attemptSeries = 0; restored.Reserve(value);
                first = restored.MarkAttempt(value.operationId);
                if (!restored.RejectFirstUnbooked(value.operationId, first, 1, "ADJUST_NOT_ALLOWED", true) || !restored.TryRead(out read) || read != null)
                    throw new Exception("Confirmed first-attempt non-booking did not permit another explicit action");
                PlayerPrefs.SetString(legacy, original);
                PlayerPrefs.SetString(conflict, original.Replace("energy-pack", "another-offer")); PlayerPrefs.Save();
                var conflicting = new PlayerValueIntentStore("new-region.invalid", "project-a", user, new[] { "old-region.invalid", "other-region.invalid" });
                if (conflicting.TryRead(out _) || !PlayerPrefs.HasKey(legacy) || !PlayerPrefs.HasKey(conflict)) throw new Exception("Conflicting legacy actions were discarded");
                int signups = 0;
                Func<System.Threading.Tasks.Task<bool>> create = () => { signups++; return System.Threading.Tasks.Task.FromResult(true); };
                GuestIdentityGuard.Run(guestProject, true, () => System.Threading.Tasks.Task.FromResult(false), create).GetAwaiter().GetResult();
                bool switched = GuestIdentityGuard.Run(guestProject, false, () => System.Threading.Tasks.Task.FromResult(false), create).GetAwaiter().GetResult();
                if (switched || signups != 0) throw new Exception("A deleted invalid restore token allowed a replacement guest");
                GuestIdentityGuard.Run(guestProject, true, () => System.Threading.Tasks.Task.FromResult(true), create).GetAwaiter().GetResult();
                if (GuestIdentityGuard.Blocked(guestProject)) throw new Exception("Successful explicit identity recovery remained blocked");
                Debug.Log("[PLAYER VALUES INTENT SMOKE] PASS stable project/player ownership, regional migration, protected conflicts, persisted attempts and conservative non-booking evidence");
            }
            finally { PlayerPrefs.DeleteKey(store.StorageKey); PlayerPrefs.DeleteKey(other.StorageKey); PlayerPrefs.DeleteKey(legacy); PlayerPrefs.DeleteKey(conflict); PlayerPrefs.DeleteKey(GuestIdentityGuard.Key(guestProject)); PlayerPrefs.Save(); }
        }
    }
}
