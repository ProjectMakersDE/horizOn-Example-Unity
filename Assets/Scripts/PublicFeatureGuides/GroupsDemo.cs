using UnityEngine;
using PM.horizOn.Cloud.Manager;

public sealed class GroupsDemo : MonoBehaviour
{
    // Initialize/connect once and restore the existing player in the Quickstart first.
    // Invoke from a player action; no background refresh or membership mutation.
    public async void ReadMyGroups()
    {
        var user = UserManager.Instance.CurrentUser;
        if (user == null || !user.IsValid()) return;
        var result = await GroupsManager.Instance.GetGroups(user.UserId);
        if (!result.IsSuccess) { Debug.LogWarning(result.ErrorCode); return; }
        Debug.Log($"Player revision: {result.Data.revision}");
        foreach (var group in result.Data.groups)
            Debug.Log($"Group {group.id}: {group.myRole}, revision {group.revision}");
        // result.Data.limits contains all current limits; existing groups may exceed them.
        // GetMembers(userId, groupId, afterUserId: null, limit: 1) reads one page.
        // Keep its revision and nextAfterUserId. A changed revision requires a restart.
        // Pending createdAt may be absent/null; it does not mean the Unix epoch.
    }
}
// For writes use the SDK GroupsExample and its durable GroupsSampleStore.
// Preserve the exact project/player, body, expected revision and pending requestId.
// Unknown create: read/reconcile explicitly; never create with a replacement revision.
