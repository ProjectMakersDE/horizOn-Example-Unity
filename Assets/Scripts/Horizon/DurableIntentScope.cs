using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SeagullStorm
{
    /// <summary>Ownership is project and player. Regional hosts only fence in-flight requests.</summary>
    internal static class DurableIntentScope
    {
        internal static string Key(string project, string user, string prefix) => Hash(project + "|" + user, prefix);
        internal static string LegacyKey(string host, string project, string user, string prefix) => Hash(host + "|" + project + "|" + user, prefix);
        private static string Hash(string value, string prefix)
        {
            using var hash = SHA256.Create();
            return prefix + BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
        }

        // Save the original bytes before deleting legacy entries. Conflicts protect every copy.
        internal static bool Migrate(string stable, string host, string project, string user, string prefix, string[] hosts)
        {
            var legacy = (hosts ?? Array.Empty<string>()).Concat(new[] { host }).Where(h => !string.IsNullOrEmpty(h))
                .Select(h => LegacyKey(h, project, user, prefix)).Distinct().Where(PlayerPrefs.HasKey).ToArray();
            if (legacy.Length == 0) return true;
            var copies = legacy.Concat(new[] { stable }).Where(PlayerPrefs.HasKey).Select(PlayerPrefs.GetString).Distinct().ToArray();
            if (copies.Length > 1) return false;
            if (copies.Length == 0) return true;
            PlayerPrefs.SetString(stable, copies[0]); PlayerPrefs.Save();
            foreach (string key in legacy) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            return true;
        }
    }
}
