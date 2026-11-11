using UnityEngine;
using PM.horizOn.Cloud.Manager;

// Existing imported SDK and authenticated player session required.
// Call these methods explicitly from a player action; nothing runs on startup.
public sealed class ChatDemo : MonoBehaviour
{
    public async void ReadChannelSnapshot(string channelId)
    {
        var user = UserManager.Instance.CurrentUser;
        if (user == null || !user.IsValid()) return;
        var result = await ChatManager.Instance.SyncChannel(user.UserId, channelId);
        if (!result.IsSuccess) { Debug.LogWarning(result.ErrorCode); return; }
        Debug.Log("Snapshot epoch: " + result.Data.channel.epoch);
        foreach (var message in result.Data.messages) Debug.Log(message.text);
        // This is the small authorized server window, not the full device history.
        // A raw snapshot does not authorize or populate a managed realtime feed.
        // WatchChannel waits for confirmed subscription; revocation hides the feed.
    }

    public void ReadDeviceHistory(string channelId, string before = null)
    {
        var user = UserManager.Instance.CurrentUser;
        if (user == null || !user.IsValid()) return;
        var page = ChatManager.Instance.ReadLocalHistory(channelId, before, 50);
        if (!page.IsSuccess) { Debug.LogWarning(page.ErrorCode); return; }
        foreach (var message in page.Data.messages) Debug.Log(message.text);
        // Continue only on an explicit action using page.Data.nextCursor.
        // Local delivered history can outlive the small current server window.
        // For send/report, import the SDK ChatExample and preserve its original
        // operationId, issuedAt, channel and text before one explicit send.
        // An uncertain outcome never permits a fresh identity or implicit retry.
    }
}
