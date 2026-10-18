using UnityEngine;
using PM.horizOn.Cloud.Manager;

public sealed class MatchesDemo : MonoBehaviour
{
    // Initialize/connect and restore the existing player in the Quickstart first.
    // Invoke explicitly. The SDK also enforces the returned polling cadence.
    public async void ReadMyMatches()
    {
        var user = UserManager.Instance.CurrentUser;
        if (user == null || !user.IsValid()) return;
        var result = await MatchesManager.Instance.GetMatches(user.UserId);
        if (!result.IsSuccess) { Debug.LogWarning(result.ErrorCode); return; }
        Debug.Log($"Ledger revision: {result.Data.revision}");
        foreach (var match in result.Data.recent)
            Debug.Log($"Match {match.id}: {match.status}");
        // Recent may include an active match after this player's forfeit.
        // Respect result.Data.pollAfterSeconds; no background poll or automatic timeout.
    }
}
// Full two-player recipe: SDK Examples/MatchesExample and MatchesSampleStore.
// ReadLedger/ReadMatch -> SaveCreateAfterLedgerReview/SaveActionAfterMatchReview.
// ApplySavedIntent sends one durable original body/revision. Unknown create is reconciled.
// Never replace a pending original with the revision from a later read.
// ReportResult uses the original finalStateRevision/result bytes/sorted winnerIds.
// Ordinary consensus is not trusted victory proof. Ranked runs use Validated Actions.
