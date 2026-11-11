using PM.horizOn.Cloud.Objects.Network.Responses;
using UnityEngine;
using PM.horizOn.Cloud.Core;
using PM.horizOn.Cloud.Manager;

// Existing SDK/project configuration and authenticated player session required.
// Call RunReviewedAction explicitly after reviewing this example and its data.
// Use the current player's authenticated session. Retry an uncertain claim
// with the same entry ID. Replace value state from the server; never apply
// a reward delta twice.
public sealed class InboxDemo : MonoBehaviour
{
    public async void RunReviewedAction()
    {
        var sessionUser = UserManager.Instance.CurrentUser;
        if (sessionUser == null || !sessionUser.IsValid()) return;
        var userId = UserManager.Instance.CurrentUser.UserId;
        var manager = InboxManager.Instance;
        var loaded = await manager.GetInbox(userId, since: null);
        if (!loaded.IsSuccess) { Debug.LogWarning(loaded.ErrorCode); return; }
        foreach (var entry in loaded.Data.entries) Debug.Log(entry.textKey);
        // The manager retains full cached entries when the wire response is unchanged.
        // Poll again only after loaded.Data.pollAfterSeconds, and stop with the game lifecycle.

        bool claimInFlight = false;
        async System.Threading.Tasks.Task ClaimSelectedReward(string entryId)
        {
            if (claimInFlight) return;
            const string projectId = "YOUR_PROJECT_ID";
            string pendingKey = "horizOn_inbox_claim_" + projectId + "_" + userId;
            string pending = PlayerPrefs.GetString(pendingKey, "");
            if (pending.Length > 0 && pending != entryId) return; // Resolve the original claim first.
            if (pending.Length == 0)
            {
                var current = manager.CurrentInbox; // Authorized for the current project/session.
                if (current == null || current.userId != userId) return;
                var selected = System.Array.Find(current.entries, entry => entry.id == entryId);
                if (selected == null || selected.claimed || selected.attachment?.values == null ||
                    selected.attachment.values.Length == 0) return;
            }
            // Previously saved IDs remain replayable even after their visible state changes.
            PlayerPrefs.SetString(pendingKey, entryId);
            PlayerPrefs.Save();
            if (PlayerPrefs.GetString(pendingKey) != entryId) return;
            claimInFlight = true;
            try
            {
                var claimed = await manager.Claim(userId, entryId);
                if (!claimed.IsSuccess) { Debug.LogWarning(claimed.ErrorCode); return; }
                // Display claimed.Data.inbox and replace values with claimed.Data.playerValues.
                // The SDK refreshes its cache. Never add reward deltas yourself.
                if (PlayerPrefs.GetString(pendingKey, "") == entryId)
                {
                    PlayerPrefs.DeleteKey(pendingKey);
                    PlayerPrefs.Save();
                }
            }
            finally { claimInFlight = false; }
        }
        // Invoke ClaimSelectedReward only from an explicit player action.
        // Retry the saved entryId after a lost response; no replacement grant or new action ID.
        // Other deliberate actions: await manager.MarkRead(userId, entryId);
        // await manager.Remove(userId, entryId); // Protects unclaimed rewards.
    }
}
