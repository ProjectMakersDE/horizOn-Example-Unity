using System;
using UnityEngine;

namespace SeagullStorm
{
    public static class StatisticsIntentSmoke
    {
        public static void Check()
        {
            string user = "smoke-" + Guid.NewGuid().ToString("N");
            var store = new StatisticsIntentStore("fixture.invalid", "project-a", user);
            var other = new StatisticsIntentStore("fixture.invalid", "project-b", user);
            string operation = "1792065600000:" + Guid.NewGuid().ToString("D");
            try
            {
                store.CompleteRun(3, 250); store.CompleteRun(2, 200);
                if (!store.TryRead(out var aggregate)) throw new Exception("Queued sessions became unreadable");
                var bounded = new[] {
                    new PM.horizOn.Cloud.Objects.Network.Responses.StatisticDefinition { key = "rounds", mode = "sum", maxPerUpdate = 1 },
                    new PM.horizOn.Cloud.Objects.Network.Responses.StatisticDefinition { key = "kills", mode = "sum" },
                    new PM.horizOn.Cloud.Objects.Network.Responses.StatisticDefinition { key = "best_score", mode = "max" } };
                if (StatisticsIntentStore.ReportError(aggregate, bounded)?.StartsWith("rounds queued batch exceeds maxPerUpdate") != true || aggregate.pending != null)
                    throw new Exception("Known report bound was not checked before reservation");
                var request = store.Reserve(operation);
                if (request.updates[0].value != 2 || request.updates[1].value != 5 || request.updates[2].value != 250)
                    throw new Exception("Completed sessions were not batched exactly");
                store.CompleteRun(7, 400);
                var restored = new StatisticsIntentStore("other-region.invalid", "project-a", user);
                if (restored.StorageKey != store.StorageKey) throw new Exception("A regional host change split the original statistics batch");
                if (!restored.TryRead(out var read) || read.pending.operationId != operation || read.pending.updates[1].value != 5 ||
                    read.rounds != 1 || read.kills != 7 || read.bestScore != 400) throw new Exception("Restart changed the original request or lost later runs");
                if (!other.TryRead(out var unrelated) || unrelated.pending != null || unrelated.rounds != 0) throw new Exception("Project intent leaked");
                bool blocked = false;
                try { restored.Reserve("new-operation"); } catch (InvalidOperationException) { blocked = true; }
                if (!blocked) throw new Exception("A new request replaced an unresolved batch");
                blocked = false;
                try { restored.Confirm("different-operation"); } catch (InvalidOperationException) { blocked = true; }
                if (!blocked) throw new Exception("A late unrelated response cleared the original batch");
                string saved = PlayerPrefs.GetString(store.StorageKey);
                PlayerPrefs.SetString(store.StorageKey, saved.Replace("\"hasPending\":true", "\"hasPending\":false"));
                if (restored.TryRead(out _)) throw new Exception("Conflicting discriminator discarded a populated original batch");
                PlayerPrefs.SetString(store.StorageKey, "{invalid");
                if (restored.TryRead(out _)) throw new Exception("Unreadable storage was accepted");
                blocked = false;
                try { restored.CompleteRun(1, 1); } catch (InvalidOperationException) { blocked = true; }
                if (!blocked) throw new Exception("Unreadable original intent was overwritten");
                PlayerPrefs.SetString(store.StorageKey, saved);
                restored.Confirm(operation);
                if (!restored.TryRead(out read) || read.pending != null || read.rounds != 1 || read.kills != 7) throw new Exception("Confirmation lost later queued runs");
                var next = restored.Reserve("1792065600001:" + Guid.NewGuid().ToString("D"));
                if (next.updates[0].value != 1 || next.updates[1].value != 7 || next.updates[2].value != 400) throw new Exception("Next batch duplicated earlier runs");
                Debug.Log("[STATISTICS SMOKE] PASS durable batch, later aggregation, restart, project isolation, unchanged original ID and corrupt-storage protection");
            }
            finally { PlayerPrefs.DeleteKey(store.StorageKey); PlayerPrefs.DeleteKey(other.StorageKey); PlayerPrefs.Save(); }
        }
    }
}
