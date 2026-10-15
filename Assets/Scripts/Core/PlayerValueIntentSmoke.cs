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
            var value = new PlayerValueIntent { userId = user, kind = "purchase", offerKey = "energy-pack", operationId = "1792065600000:" + Guid.NewGuid().ToString("D") };
            try
            {
                store.Reserve(value);
                var restored = new PlayerValueIntentStore("fixture.invalid", "project-a", user);
                if (!restored.TryRead(out var read) || read.operationId != value.operationId || read.offerKey != value.offerKey)
                    throw new Exception("Restart lost the original action");
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
                restored.Confirm(value.operationId);
                if (!restored.TryRead(out read) || read != null) throw new Exception("Confirmed action was not cleared");
            }
            finally { PlayerPrefs.DeleteKey(store.StorageKey); PlayerPrefs.DeleteKey(other.StorageKey); PlayerPrefs.Save(); }
        }
    }
}
