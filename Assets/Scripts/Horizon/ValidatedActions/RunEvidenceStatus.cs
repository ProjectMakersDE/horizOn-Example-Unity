namespace SeagullStorm
{
    /// <summary>Evidence belongs to a ticket; synchronous failures and older callbacks cannot become pending again.</summary>
    public sealed class RunEvidenceStatus
    {
        public string RunId { get; private set; }
        public string Status { get; private set; } = "No evidence requested";
        private bool _resolved;
        public void Begin(string runId)
        {
            RunId = runId;
            Status = "No evidence requested";
            _resolved = false;
        }
        public void Requested(bool required)
        {
            if (!_resolved) Status = required ? "Evidence upload pending" : "No evidence requested";
        }
        public void Resolve(string runId, string status)
        {
            if (string.IsNullOrEmpty(RunId) || runId != RunId) return;
            Status = status;
            _resolved = true;
        }
    }
}
