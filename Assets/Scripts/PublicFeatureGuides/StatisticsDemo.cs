using PM.horizOn.Cloud.Objects.Network.Requests;
using UnityEngine;
using PM.horizOn.Cloud.Core;
using PM.horizOn.Cloud.Manager;

// Existing SDK/project configuration and authenticated player session required.
// Call RunReviewedAction explicitly after reviewing this example and its data.
// Read a snapshot first, then keep one operation ID and the original batch
// through retries. An expired or uncertain result requires reconciliation,
// never an automatic new-ID report.
// This explicit action reports the shown persisted batch.
public sealed class StatisticsDemo : MonoBehaviour
{
    public async void RunReviewedAction()
    {
        var sessionUser = UserManager.Instance.CurrentUser;
        if (sessionUser == null || !sessionUser.IsValid()) return;
        var manager = StatisticsManager.Instance;
        var snapshot = await manager.GetStatistics();
        if (snapshot == null) { Debug.LogWarning(manager.LastErrorCode); return; }
        string pendingKey = "horizOn_statistics_pending_" + snapshot.userId;
        var pending = PlayerPrefs.HasKey(pendingKey)
            ? JsonUtility.FromJson<UpdateStatisticsRequest>(PlayerPrefs.GetString(pendingKey))
            : new UpdateStatisticsRequest {
                userId = snapshot.userId, operationId = manager.CreateOperationId(),
                updates = new[] { new StatisticUpdate("kills", 3), new StatisticUpdate("best_score", 250) }
            };
        string originalJson = JsonUtility.ToJson(pending);
        PlayerPrefs.SetString(pendingKey, originalJson);
        PlayerPrefs.Save();
        var result = await manager.UpdateStatistics(pending.updates, pending.operationId);
        if (result == null) {
            // Keep the saved ID and batch. Reconcile OPERATION_EXPIRED before a new logical report.
            Debug.LogWarning(manager.LastErrorCode);
            return;
        }
        // A late success for O1 must never acknowledge a newer pending O2.
        if (PlayerPrefs.GetString(pendingKey, null) == originalJson) {
            PlayerPrefs.DeleteKey(pendingKey);
            PlayerPrefs.Save();
        }
        foreach (var counter in result.values) Debug.Log(counter.key + "=" + counter.value);
    }
}
