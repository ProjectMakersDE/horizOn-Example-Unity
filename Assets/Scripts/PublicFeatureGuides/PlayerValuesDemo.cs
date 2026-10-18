using UnityEngine;
using PM.horizOn.Cloud.Core;
using PM.horizOn.Cloud.Manager;

// Existing SDK/project configuration and authenticated player session required.
// Call RunReviewedAction explicitly after reviewing this example and its data.
public sealed class PlayerValuesDemo : MonoBehaviour
{
    public async void RunReviewedAction()
    {
        var sessionUser = UserManager.Instance.CurrentUser;
        if (sessionUser == null || !sessionUser.IsValid()) return;
        var userId = UserManager.Instance.CurrentUser.UserId;
        var current = await PlayerValuesManager.Instance.GetState(userId);
        if (!current.IsSuccess) { Debug.LogWarning(current.ErrorCode); return; }
        foreach (var value in current.State.values) Debug.Log(value.key + "=" + value.balance);
        // This minimal first call reads authoritative balances, never a local reward delta.
        // For purchases and original retry, import PlayerValuesExample and its
        // PendingPlayerValuesPurchase helper from the official SDK samples.
        // Persist the complete original user/offer/operationId before one send.
        // An uncertain or expired original must never become a fresh purchase ID.
    }
}
