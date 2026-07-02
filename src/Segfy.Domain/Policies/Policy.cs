using Segfy.Domain.Common;
using Segfy.Domain.Common.Errors;
using Segfy.Domain.Policies.ValueObjects;

namespace Segfy.Domain.Policies;

public sealed class Policy : AggregateRoot
{
    private readonly List<PolicyStatusHistory> _statusHistory = new();

    private Policy() { }

    private Policy(
        Guid id,
        PolicyNumber number,
        Document document,
        LicensePlate licensePlate,
        Money premium,
        DateOnly coverageStart,
        DateOnly coverageEnd,
        PolicyStatus status,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        Number = number;
        Document = document;
        LicensePlate = licensePlate;
        Premium = premium;
        CoverageStart = coverageStart;
        CoverageEnd = coverageEnd;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public PolicyNumber Number { get; private set; } = null!;
    public Document Document { get; private set; } = null!;
    public LicensePlate LicensePlate { get; private set; } = null!;
    public Money Premium { get; private set; } = null!;
    public DateOnly CoverageStart { get; private set; }
    public DateOnly CoverageEnd { get; private set; }
    public PolicyStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<PolicyStatusHistory> StatusHistory => _statusHistory;

    public CoveragePeriod Coverage => CoveragePeriod.LoadTrusted(CoverageStart, CoverageEnd);

    public static Policy Create(
        PolicyNumber number,
        Document document,
        LicensePlate licensePlate,
        Money premium,
        CoveragePeriod coverage,
        DateTime nowUtc) =>
        new(Guid.NewGuid(), number, document, licensePlate, premium,
            coverage.Start, coverage.End, PolicyStatus.Ativa, nowUtc, nowUtc);

    public static Policy Load(
        Guid id,
        PolicyNumber number,
        Document document,
        LicensePlate licensePlate,
        Money premium,
        CoveragePeriod coverage,
        PolicyStatus status,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(id, number, document, licensePlate, premium,
            coverage.Start, coverage.End, status, createdAt, updatedAt);

    public void UpdateDetails(
        Document document,
        LicensePlate licensePlate,
        Money premium,
        CoveragePeriod coverage,
        DateTime nowUtc)
    {
        if (Status != PolicyStatus.Ativa)
            throw new DomainInvalidStateException(
                $"Policy details can only be updated while status is Ativa. Current status: {Status}.");

        Document = document;
        LicensePlate = licensePlate;
        Premium = premium;
        CoverageStart = coverage.Start;
        CoverageEnd = coverage.End;
        UpdatedAt = nowUtc;
    }

    public void ChangeStatus(PolicyStatus newStatus, DateTime nowUtc, string? reason = null)
    {
        if (!Status.CanTransitionTo(newStatus))
            throw new DomainInvalidStateException(
                $"Cannot transition policy status from {Status} to {newStatus}.");

        var previous = Status;
        Status = newStatus;
        UpdatedAt = nowUtc;
        _statusHistory.Add(new PolicyStatusHistory(
            Guid.NewGuid(), Id, previous, newStatus, reason, nowUtc));
    }
}
