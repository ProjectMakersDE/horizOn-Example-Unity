using System.Linq;
using UnityEngine;
using PM.horizOn.Cloud.Core;
using PM.horizOn.Cloud.Manager;

// Existing SDK/project configuration and authenticated player session required.
// Call RunReviewedAction explicitly after reviewing this example and its data.
// Rewards are credited immediately with the unlock. If credit fails, the
// entire action is rolled back.
// This explicit action unlocks the shown configured achievement key.
public sealed class AchievementsDemo : MonoBehaviour
{
    public async void RunReviewedAction()
    {
        var sessionUser = UserManager.Instance.CurrentUser;
        if (sessionUser == null || !sessionUser.IsValid()) return;
        var userId = UserManager.Instance.CurrentUser.UserId;
        var manager = AchievementsManager.Instance;
        var loaded = await manager.GetAchievements(userId);
        if (!loaded.IsSuccess) { Debug.LogWarning(loaded.ErrorCode); return; }
        foreach (var progress in loaded.Data.progress)
            Debug.Log(progress.key + ": " + progress.rewardState);

        // Configure a manual first_steps definition, then call this on the matching game action.
        var unlocked = await manager.Unlock(userId, "first_steps");
        if (!unlocked.IsSuccess) { Debug.LogWarning(unlocked.ErrorCode); return; }
        // Repeat the same key after timeout; never create a replacement reward identity.
        // rewardState is complete, unlockedAt is null, and deliveryResults is empty.
        // Values were credited atomically. Read PlayerValues again when showing the balance.
        // No Inbox claim or local value addition. Refresh achievements after statistics updates.
    }
}
