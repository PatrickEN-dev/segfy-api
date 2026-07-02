namespace Segfy.Domain.Policies;

public sealed class PolicyStatusHistory
{
    private PolicyStatusHistory() { }

    public PolicyStatusHistory(
        Guid id,
        Guid policyId,
        PolicyStatus fromStatus,
        PolicyStatus toStatus,
        string? reason,
        DateTime changedAt)
    {
        Id = id;
        PolicyId = policyId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Reason = reason;
        ChangedAt = changedAt;
    }

    public Guid Id { get; private set; }
    public Guid PolicyId { get; private set; }
    public PolicyStatus FromStatus { get; private set; }
    public PolicyStatus ToStatus { get; private set; }
    public string? Reason { get; private set; }
    public DateTime ChangedAt { get; private set; }
}
