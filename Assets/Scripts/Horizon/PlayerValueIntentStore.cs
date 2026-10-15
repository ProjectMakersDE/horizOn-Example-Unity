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
    }

    /// <summary>One durable, immutable action per project and player; never stores credentials.</summary>
    public sealed class PlayerValueIntentStore
    {
        private readonly string _key, _user;
        internal string StorageKey => _key;

        public PlayerValueIntentStore(string host, string projectKey, string user)
        {
            _user = user;
            using var hash = SHA256.Create();
            _key = "seagull.player-values.pending." + BitConverter.ToString(hash.ComputeHash(
                Encoding.UTF8.GetBytes(host + "|" + projectKey + "|" + user))).Replace("-", "");
        }

        public bool TryRead(out PlayerValueIntent intent)
        {
            intent = null;
            if (!PlayerPrefs.HasKey(_key)) return true;
            try
            {
                var value = JsonUtility.FromJson<PlayerValueIntent>(PlayerPrefs.GetString(_key));
                if (value == null || value.userId != _user || string.IsNullOrWhiteSpace(value.operationId) ||
                    (value.kind != "purchase" && value.kind != "adjust") ||
                    (value.kind == "purchase" ? string.IsNullOrWhiteSpace(value.offerKey) :
                        string.IsNullOrWhiteSpace(value.valueKey) || value.amount != 1 || value.reason != "example-explicit-adjustment")) return false;
                intent = value;
                return true;
            }
            catch { return false; }
        }

        public void Reserve(PlayerValueIntent intent)
        {
            if (PlayerPrefs.HasKey(_key)) throw new InvalidOperationException("A pending intent already exists");
            if (intent == null || intent.userId != _user) throw new InvalidOperationException("Intent owner mismatch");
            PlayerPrefs.SetString(_key, JsonUtility.ToJson(intent));
            PlayerPrefs.Save();
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
