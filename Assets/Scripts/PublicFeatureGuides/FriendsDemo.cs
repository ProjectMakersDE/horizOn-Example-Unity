using UnityEngine;
using PM.horizOn.Cloud.Core;
using PM.horizOn.Cloud.Manager;

// Existing SDK/project configuration and authenticated player session required.
// Call RunReviewedAction explicitly after reviewing this example and its data.
// After timeout or conflict, refresh and reconcile the intent. Never
// automatically retry with a new revision.
public sealed class FriendsDemo : MonoBehaviour
{
    public async void RunReviewedAction()
    {
        var sessionUser = UserManager.Instance.CurrentUser;
        if (sessionUser == null || !sessionUser.IsValid()) return;
        var userId = UserManager.Instance.CurrentUser.UserId;
        var manager = FriendsManager.Instance;
        var loaded = await manager.GetFriends(userId);
        if (!loaded.IsSuccess) { Debug.LogWarning(loaded.ErrorCode); return; }
        Debug.Log(loaded.Data.friendCode); // Public identifier, not a credential.
        foreach (var friend in loaded.Data.friends)
            Debug.Log(friend.display.name + ": " + (friend.lastActiveAt ?? "unknown"));

        // Invoke only after the player explicitly reviews this snapshot and chooses an action.
        async System.Threading.Tasks.Task ApplyReviewed(string action, string targetUserId,
            long expectedRevision, string requestId = null)
        {
            var result = action switch {
                "request" => await manager.SendRequest(userId, targetUserId, expectedRevision),
                "accept" => await manager.AcceptRequest(userId, targetUserId, expectedRevision, requestId),
                "decline" => await manager.DeclineRequest(userId, targetUserId, expectedRevision, requestId),
                "cancel" => await manager.CancelRequest(userId, targetUserId, expectedRevision, requestId),
                "remove" => await manager.RemoveFriend(userId, targetUserId, expectedRevision),
                "block" => await manager.BlockPlayer(userId, targetUserId, expectedRevision),
                "unblock" => await manager.UnblockPlayer(userId, targetUserId, expectedRevision),
                _ => throw new System.ArgumentException("Unknown action")
            };
            if (!result.IsSuccess) {
                Debug.LogWarning(result.ErrorCode); // Timeout/409: reload and reconcile; do not repeat automatically.
                return;
            }
            Debug.Log(result.Data.revision); // Replace the whole displayed snapshot.
        }
        // Accept/decline: original incoming requestId. Cancel: original outgoing requestId.
        // Never replace expectedRevision on an uncertain retry. A new request is a new identity.
        // FindPlayer(userId, exactUuidOrHCode) never searches names or emails.
        // Last login is nullable, never an online flag. No hidden polling timer.
    }
}
