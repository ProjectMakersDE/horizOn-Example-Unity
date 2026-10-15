using System;
using System.Threading.Tasks;
using UnityEngine;

namespace SeagullStorm
{
    /// <summary>Persist identity recovery before invoking SDK code that may clear an invalid cached token.</summary>
    internal static class GuestIdentityGuard
    {
        internal static string Key(string project) => DurableIntentScope.Key(project, "", "seagull.guest-recovery.");
        internal static bool Blocked(string project) => PlayerPrefs.HasKey(Key(project));
        internal static void Protect(string project, bool hasCachedToken)
        { if (hasCachedToken) { PlayerPrefs.SetString(Key(project), "Existing anonymous identity requires recovery"); PlayerPrefs.Save(); } }
        internal static async Task<bool> Run(string project, bool hasCachedToken, Func<Task<bool>> restore, Func<Task<bool>> create = null)
        {
            Protect(project, hasCachedToken);
            if (!hasCachedToken && (Blocked(project) || create == null)) return false;
            PlayerPrefs.SetString(Key(project), "Anonymous identity attempt unresolved"); PlayerPrefs.Save();
            bool success = await (hasCachedToken ? restore() : create());
            if (success) { PlayerPrefs.DeleteKey(Key(project)); PlayerPrefs.Save(); }
            return success;
        }
    }
}
