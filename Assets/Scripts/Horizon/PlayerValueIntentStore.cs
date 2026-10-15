using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SeagullStorm
{
    [Serializable]
    public sealed class PlayerValueIntent
    {
        public string userId, kind, offerKey, valueKey, reason, operationId;
        public long amount;
        public int attemptSeries;
    }

    /// <summary>One durable, immutable action per project and player; never stores credentials.</summary>
    public sealed class PlayerValueIntentStore
    {
        private readonly string _key, _user;
        private readonly bool _migrated;
        internal string StorageKey => _key;

        public PlayerValueIntentStore(string host, string projectKey, string user, string[] knownHosts = null)
        {
            _user = user;
            const string prefix = "seagull.player-values.pending.";
            _key = DurableIntentScope.Key(projectKey, user, prefix);
            _migrated = DurableIntentScope.Migrate(_key, host, projectKey, user, prefix, knownHosts);
        }

        public bool TryRead(out PlayerValueIntent intent)
        {
            intent = null;
            if (!_migrated) return false;
            if (!PlayerPrefs.HasKey(_key)) return true;
            try
            {
                var value = JsonUtility.FromJson<PlayerValueIntent>(PlayerPrefs.GetString(_key));
                if (value == null || value.userId != _user || string.IsNullOrWhiteSpace(value.operationId) || value.attemptSeries < 0 ||
                    (value.kind != "purchase" && value.kind != "adjust") ||
                    (value.kind == "purchase" ? string.IsNullOrWhiteSpace(value.offerKey) :
                        string.IsNullOrWhiteSpace(value.valueKey) || value.amount != 1 || value.reason != "example-explicit-adjustment")) return false;
                intent = value;
                // Older example versions had no transport evidence. Treat their saved actions as previously attempted.
                if (!PlayerPrefs.GetString(_key).Contains("\"attemptSeries\"")) intent.attemptSeries = 1;
                return true;
            }
            catch { return false; }
        }

        public void Reserve(PlayerValueIntent intent)
        {
            if (!TryRead(out var pending) || pending != null) throw new InvalidOperationException("A pending intent already exists or requires reconciliation");
            if (intent == null || intent.userId != _user) throw new InvalidOperationException("Intent owner mismatch");
            PlayerPrefs.SetString(_key, JsonUtility.ToJson(intent));
            PlayerPrefs.Save();
        }

        public int MarkAttempt(string operationId)
        {
            if (!TryRead(out var intent) || intent?.operationId != operationId || intent.attemptSeries == int.MaxValue)
                throw new InvalidOperationException("Pending attempt is protected");
            intent.attemptSeries++;
            PlayerPrefs.SetString(_key, JsonUtility.ToJson(intent)); PlayerPrefs.Save();
            return intent.attemptSeries;
        }

        public bool RejectFirstUnbooked(string operationId, int series, int transportAttempts, string errorCode, bool authoritativeState)
        {
            if (!authoritativeState || series != 1 || transportAttempts != 1 ||
                (errorCode != "ADJUST_NOT_ALLOWED" && errorCode != "INSUFFICIENT_BALANCE")) return false;
            if (!TryRead(out var intent) || intent?.operationId != operationId || intent.attemptSeries != 1) return false;
            Confirm(operationId);
            return true;
        }

        public void Confirm(string operationId)
        {
            if (!TryRead(out var intent) || intent?.operationId != operationId)
                throw new InvalidOperationException("Pending intent changed; preserve it for reconciliation");
            PlayerPrefs.DeleteKey(_key);
            PlayerPrefs.Save();
        }
    }
}
