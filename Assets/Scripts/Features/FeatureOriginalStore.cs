using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;

namespace SeagullStorm
{
    /// <summary>Small caller-owned originals for Friends, Inbox, Achievements and Chat. No result history.</summary>
    public sealed class FeatureOriginalStore : IDisposable
    {
        [Serializable] private sealed class Envelope { public string owner, payloadBase64, sha256; }
        private readonly string _path, _owner;
        private readonly FileStream _lease;
        private string _original;
        public FeatureOriginalStore(string projectKey, string userId, string feature, string storageDirectory = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new NotSupportedException("This desktop feature lab needs durable native files before sending. No request was sent.");
#else
            if (string.IsNullOrEmpty(projectKey) || string.IsNullOrEmpty(userId)) throw new ArgumentException("Authenticated project/player required");
            _owner = Hash(projectKey + "|" + userId + "|" + feature);
            string directory = Path.Combine(storageDirectory ?? Application.persistentDataPath, "seagull-feature-originals");
            Directory.CreateDirectory(directory); _path = Path.Combine(directory, _owner + ".json");
            _lease = new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            try { if (File.Exists(_path)) Read(); }
            catch { _lease.Dispose(); throw; }
#endif
        }
        public string Read()
        {
            if (!File.Exists(_path)) return null;
            string raw;
            using (var stream = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            { stream.Flush(true); using (var reader = new StreamReader(stream, Encoding.UTF8)) raw = reader.ReadToEnd(); }
            var envelope = JsonUtility.FromJson<Envelope>(raw);
            if (envelope == null || envelope.owner != _owner || envelope.payloadBase64 == null)
                throw new IOException("Original is unknown/corrupt; preserve it for recovery");
            string payload;
            try { payload = new UTF8Encoding(false, true).GetString(Convert.FromBase64String(envelope.payloadBase64)); }
            catch (Exception) { throw new IOException("Original encoding is unknown; preserve it for recovery"); }
            if (envelope.sha256 != Hash(payload)) throw new IOException("Original hash differs; preserve it for recovery");
            _original = raw; return payload;
        }
        public void SaveNew(string payload)
        {
            if (File.Exists(_path)) throw new IOException("Reconcile the saved original before another action");
            string raw = JsonUtility.ToJson(new Envelope { owner = _owner, payloadBase64 = Convert.ToBase64String(new UTF8Encoding(false, true).GetBytes(payload)), sha256 = Hash(payload) });
            using (var stream = new FileStream(_path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { var bytes = new UTF8Encoding(false, true).GetBytes(raw); stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (Read() != payload) throw new IOException("Original could not be verified; no request was sent");
        }
        public void Confirm(string payload)
        {
            if (Read() != payload || File.ReadAllText(_path, Encoding.UTF8) != _original) throw new IOException("Original changed; preserve it");
            File.Delete(_path); // Successful identity only. No new complete archive is created.
        }
        private static string Hash(string value)
        { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", ""); }
        public void Dispose() => _lease?.Dispose(); // Stable lock inode is deliberately retained.
    }
}
