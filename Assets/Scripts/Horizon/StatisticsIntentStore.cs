using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using PM.horizOn.Cloud.Objects.Network.Requests;
using PM.horizOn.Cloud.Objects.Network.Responses;
using UnityEngine;

namespace SeagullStorm
{
    [Serializable]
    public sealed class StatisticsIntentState
    {
        public string userId;
        public long rounds, kills, bestScore;
        public bool hasPending;
        public UpdateStatisticsRequest pending;
    }

    /// <summary>Bounded storage: one immutable request plus the aggregate of later completed runs.</summary>
    public sealed class StatisticsIntentStore
    {
        private const long Max = 9007199254740991;
        private readonly string _key, _user;
        private readonly bool _migrated;
        internal string StorageKey => _key;
        public static StatisticsIntentStore Current() => new StatisticsIntentStore(
            PM.horizOn.Cloud.Core.HorizonApp.Network.GetActiveHost(),
            PM.horizOn.Cloud.Core.HorizonApp.Network.GetApiKey(), HorizonManager.Instance.GetUserId(),
            knownHosts: PM.horizOn.Cloud.Core.HorizonConfig.Load()?.Hosts);

        public StatisticsIntentStore(string host, string projectKey, string user, string prefix = "seagull.statistics.", string[] knownHosts = null)
        {
            if (string.IsNullOrEmpty(user)) throw new InvalidOperationException("A signed-in owner is required");
            _user = user;
            _key = DurableIntentScope.Key(projectKey, user, prefix);
            _migrated = DurableIntentScope.Migrate(_key, host, projectKey, user, prefix, knownHosts);
        }

        internal static string ScopeKey(string host, string projectKey, string user, string prefix)
        {
            return DurableIntentScope.Key(projectKey, user, prefix);
        }

        public bool TryRead(out StatisticsIntentState state)
        {
            state = null;
            if (!_migrated) return false;
            if (!PlayerPrefs.HasKey(_key)) { state = new StatisticsIntentState { userId = _user }; return true; }
            try
            {
                var value = JsonUtility.FromJson<StatisticsIntentState>(PlayerPrefs.GetString(_key));
                if (value == null || value.userId != _user || !Safe(value.rounds) || !Safe(value.kills) || !Safe(value.bestScore)) return false;
                // JsonUtility materializes null nested serializable classes as empty objects.
                // An explicit discriminator keeps an empty queue distinct from corrupt intent data.
                if (value.hasPending)
                {
                    var request = value.pending;
                    if (request == null || request.userId != _user || string.IsNullOrEmpty(request.operationId) || request.updates?.Length != 3) return false;
                    string[] keys = { "rounds", "kills", "best_score" };
                    for (int i = 0; i < keys.Length; i++)
                        if (request.updates[i] == null || request.updates[i].key != keys[i] || !Safe(request.updates[i].value)) return false;
                    if (request.updates[0].value < 1) return false;
                }
                else
                {
                    if (value.pending != null && (!string.IsNullOrEmpty(value.pending.userId) ||
                        !string.IsNullOrEmpty(value.pending.operationId) || value.pending.updates?.Length > 0)) return false;
                    value.pending = null;
                }
                state = value;
                return true;
            }
            catch { return false; }
        }

        private static bool Safe(long value) => value >= 0 && value <= Max;
        internal static string ReportError(StatisticsIntentState state, StatisticDefinition[] definitions)
        {
            if (state == null || definitions == null) return "Read the server snapshot first.";
            string[] keys = { "rounds", "kills", "best_score" }, modes = { "sum", "sum", "max" };
            long[] values = { state.rounds, state.kills, state.bestScore };
            for (int i = 0; i < keys.Length; i++)
            {
                var definition = definitions.FirstOrDefault(d => d.key == keys[i] && d.mode == modes[i]);
                if (definition == null) return "Configure rounds=sum, kills=sum and best_score=max, then read again. Queued runs are retained.";
                if (definition.HasMaxPerUpdate && values[i] > definition.maxPerUpdate)
                    return keys[i] + " queued batch exceeds maxPerUpdate. Reconcile the aggregate or adjust project bounds; no original request has been reserved.";
            }
            return null;
        }
        private StatisticsIntentState Read() => TryRead(out var state) ? state : throw new InvalidOperationException("Unreadable statistics intent is protected");
        private void Save(StatisticsIntentState state) { PlayerPrefs.SetString(_key, JsonUtility.ToJson(state)); PlayerPrefs.Save(); }

        public void CompleteRun(long kills, long score)
        {
            var state = Read();
            if (!Safe(kills) || !Safe(score) || state.rounds == Max || kills > Max - state.kills)
                throw new InvalidOperationException("Statistics aggregate exceeds the safe integer range");
            state.rounds++;
            state.kills += kills;
            state.bestScore = Math.Max(state.bestScore, score);
            Save(state);
        }

        public UpdateStatisticsRequest Reserve(string operationId)
        {
            var state = Read();
            if (state.pending != null) throw new InvalidOperationException("An unresolved original batch already exists");
            if (state.rounds < 1 || string.IsNullOrEmpty(operationId)) throw new InvalidOperationException("No completed run to report");
            state.pending = new UpdateStatisticsRequest { userId = _user, operationId = operationId, updates = new[] {
                new StatisticUpdate("rounds", state.rounds), new StatisticUpdate("kills", state.kills), new StatisticUpdate("best_score", state.bestScore) } };
            state.hasPending = true;
            state.rounds = state.kills = state.bestScore = 0;
            Save(state);
            return state.pending;
        }

        public void Confirm(string operationId)
        {
            var state = Read();
            if (state.pending?.operationId != operationId) throw new InvalidOperationException("Another response cannot clear the original batch");
            state.pending = null;
            state.hasPending = false;
            Save(state);
        }
    }
}
