using UnityEngine;
using PM.horizOn.Cloud.Manager;

public sealed class ItemsDemo : MonoBehaviour
{
    // Initialize/connect and restore the existing player in the Quickstart first.
    public async void ReadInventory()
    {
        var user = UserManager.Instance.CurrentUser;
        if (user == null || !user.IsValid()) return;
        var result = await ItemsManager.Instance.GetInventory(user.UserId);
        if (!result.IsSuccess) { Debug.LogWarning(result.ErrorCode); return; }
        foreach (var item in result.Data.instances)
            Debug.Log($"{item.instanceId}: {item.itemKey}");
        // Keep result.Data.revision for a separately reviewed mutation.
    }

    // Wire a separate explicit action; no automatic catalog traversal or poll.
    public async void ReadCatalogPage()
    {
        var user = UserManager.Instance.CurrentUser;
        if (user == null || !user.IsValid()) return;
        var result = await ItemsManager.Instance.GetCatalog(user.UserId, limit: 1);
        if (!result.IsSuccess) { Debug.LogWarning(result.ErrorCode); return; }
        Debug.Log($"Catalog revision: {result.Data.revision}");
        // Explicit Next keeps result.Data.revision and nextAfterKey unchanged.
    }
}
// SDK ItemsExample/ItemsSampleStore persist original instanceId/body/revision before writes.
// Keep the original Player Values operationId for purchase recovery.
// The initial success may include item references; retained replay may return empty applied/items.
// Read the current inventory separately. Never reconstruct earlier item references.
// A failed refresh never authorizes a replacement purchase or a local item grant.
