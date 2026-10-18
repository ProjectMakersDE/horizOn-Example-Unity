using UnityEngine;
using PM.horizOn.Cloud.Manager;
using PM.horizOn.Cloud.Objects.Network.Responses;

public sealed class SharedObjectsDemo : MonoBehaviour
{
    // Initialize/connect and restore the existing player in the Quickstart first.
    // Wire an explicit player action; do not add this read to a startup/poll loop.
    public async void ReadWorldMap()
    {
        var user = UserManager.Instance.CurrentUser;
        if (user == null || !user.IsValid()) return;
        var result = await SharedObjectsManager.Instance.QueryObjects(user.UserId, "bases", new SharedQuery {
            mode = SharedQueryMode.Rectangle,
            minX = -10, maxX = 10, minY = -10, maxY = 10, limit = 20
        });
        if (!result.IsSuccess) {
            Debug.LogWarning(result.ErrorCode);
            return;
        }
        foreach (var item in result.Data.objects) {
            byte[] bytes = item.data; // Opaque game bytes; validate your game's own format.
            Debug.Log($"Object {item.id}: {bytes.Length} bytes");
        }
        // Respect result.Data.pollAfterSeconds; no automatic poll or next-page loop.
        // Keep exact dataBase64 and typed search metadata unchanged on metadata edits.
        // Persist original target, bytes and If-Match revision before a single mutation.
        // Unknown CREATE: explicit GetRecoveryUsage/QueryRecoveryObjects; never resend.
        // Generic/defense/ranked VA starts share one start/submit lease; do not replace a pending run.
        // Trusted validation and durable ordinary Inbox delivery belong to the game backend.
    }
}
