using System;
using UnityEngine;
public static class SeagullEvidenceFixture
{
    public static void Check()
    {
        var status = new SeagullStorm.RunEvidenceStatus();
        status.Begin("A");
        status.Resolve("A", "preflight failed");
        status.Requested(true);
        if (status.Status != "preflight failed") throw new Exception("Synchronous failure was replaced by pending");
        status.Begin("B");
        status.Resolve("A", "old upload completed");
        if (status.Status != "No evidence requested") throw new Exception("Old callback contaminated new ticket");
        status.Requested(true);
        if (status.Status != "Evidence upload pending") throw new Exception("New ticket never entered pending");
        status.Resolve("B", "uploaded");
        status.Requested(true);
        if (status.Status != "uploaded") throw new Exception("Completion was replaced by pending");
        status.Begin(null);
        status.Resolve("B", "late upload");
        if (status.Status != "No evidence requested") throw new Exception("Reset accepted stale callback");
        Debug.Log("[EVIDENCE FIXTURE] PASS: preflight failure, stale callbacks, completion and reset");
    }
}
