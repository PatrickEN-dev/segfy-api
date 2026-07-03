using FluentAssertions;
using Segfy.Domain.Common.Errors;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.ValueObjects;
using Xunit;

namespace Segfy.Domain.Tests.Policies;

public sealed class PolicyTests
{
    private static Policy NewActivePolicy(DateTime? now = null)
    {
        var nowUtc = now ?? new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        return Policy.Create(
            PolicyNumber.Create(2026, 1),
            Document.Create("52998224725"),
            LicensePlate.Create("ABC1234"),
            Money.Create(199.90m),
            CoveragePeriod.Create(new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30)),
            nowUtc);
    }

    [Fact]
    public void Create_SetsStatusAtiva()
    {
        var policy = NewActivePolicy();
        policy.Status.Should().Be(PolicyStatus.Ativa);
    }

    [Fact]
    public void Create_SetsCreatedAndUpdatedAtToNowUtc()
    {
        var now = new DateTime(2026, 7, 1, 12, 30, 45, DateTimeKind.Utc);
        var policy = NewActivePolicy(now);
        policy.CreatedAt.Should().Be(now);
        policy.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void ChangeStatus_AtivaToCancelada_Succeeds()
    {
        var policy = NewActivePolicy();
        var later = policy.CreatedAt.AddDays(1);
        policy.ChangeStatus(PolicyStatus.Cancelada, later);
        policy.Status.Should().Be(PolicyStatus.Cancelada);
        policy.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void ChangeStatus_AtivaToExpirada_Succeeds()
    {
        var policy = NewActivePolicy();
        var later = policy.CreatedAt.AddDays(1);
        policy.ChangeStatus(PolicyStatus.Expirada, later);
        policy.Status.Should().Be(PolicyStatus.Expirada);
    }

    [Fact]
    public void ChangeStatus_CanceladaToAtiva_ThrowsInvalidState()
    {
        var policy = NewActivePolicy();
        policy.ChangeStatus(PolicyStatus.Cancelada, policy.CreatedAt.AddDays(1));
        var act = () => policy.ChangeStatus(PolicyStatus.Ativa, policy.CreatedAt.AddDays(2));
        act.Should().Throw<DomainInvalidStateException>();
    }

    [Fact]
    public void ChangeStatus_CanceladaToExpirada_ThrowsInvalidState()
    {
        var policy = NewActivePolicy();
        policy.ChangeStatus(PolicyStatus.Cancelada, policy.CreatedAt.AddDays(1));
        var act = () => policy.ChangeStatus(PolicyStatus.Expirada, policy.CreatedAt.AddDays(2));
        act.Should().Throw<DomainInvalidStateException>();
    }

    [Fact]
    public void ChangeStatus_ExpiradaToAtiva_ThrowsInvalidState()
    {
        var policy = NewActivePolicy();
        policy.ChangeStatus(PolicyStatus.Expirada, policy.CreatedAt.AddDays(1));
        var act = () => policy.ChangeStatus(PolicyStatus.Ativa, policy.CreatedAt.AddDays(2));
        act.Should().Throw<DomainInvalidStateException>();
    }

    [Fact]
    public void ChangeStatus_AtivaToAtiva_ThrowsInvalidState()
    {
        var policy = NewActivePolicy();
        var act = () => policy.ChangeStatus(PolicyStatus.Ativa, policy.CreatedAt.AddDays(1));
        act.Should().Throw<DomainInvalidStateException>();
    }

    [Fact]
    public void UpdateDetails_UpdatesFieldsAndUpdatedAt()
    {
        var policy = NewActivePolicy();
        var later = policy.CreatedAt.AddDays(1);

        policy.UpdateDetails(
            Document.Create("39053344705"),
            LicensePlate.Create("DEF2G34"),
            Money.Create(249.50m),
            CoveragePeriod.Create(new DateOnly(2026, 7, 1), new DateOnly(2027, 12, 31)),
            later);

        policy.Document.Digits.Should().Be("39053344705");
        policy.LicensePlate.Value.Should().Be("DEF2G34");
        policy.Premium.Amount.Should().Be(249.50m);
        policy.CoverageEnd.Should().Be(new DateOnly(2027, 12, 31));
        policy.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void UpdateDetails_OnCancelledPolicy_ThrowsInvalidState()
    {
        var policy = NewActivePolicy();
        policy.ChangeStatus(PolicyStatus.Cancelada, policy.CreatedAt.AddDays(1));

        var act = () => policy.UpdateDetails(
            Document.Create("39053344705"),
            LicensePlate.Create("DEF2G34"),
            Money.Create(249.50m),
            CoveragePeriod.Create(new DateOnly(2026, 7, 1), new DateOnly(2027, 12, 31)),
            policy.CreatedAt.AddDays(2));

        act.Should().Throw<DomainInvalidStateException>();
    }

    [Fact]
    public void UpdateDetails_OnExpiredPolicy_ThrowsInvalidState()
    {
        var policy = NewActivePolicy();
        policy.ChangeStatus(PolicyStatus.Expirada, policy.CreatedAt.AddDays(1));

        var act = () => policy.UpdateDetails(
            Document.Create("39053344705"),
            LicensePlate.Create("DEF2G34"),
            Money.Create(249.50m),
            CoveragePeriod.Create(new DateOnly(2026, 7, 1), new DateOnly(2027, 12, 31)),
            policy.CreatedAt.AddDays(2));

        act.Should().Throw<DomainInvalidStateException>();
    }

    [Fact]
    public void ChangeStatus_WithReason_AppendsHistoryEntry()
    {
        var policy = NewActivePolicy();
        var later = policy.CreatedAt.AddDays(1);
        policy.ChangeStatus(PolicyStatus.Cancelada, later, "non-payment");

        policy.StatusHistory.Should().ContainSingle();
        var entry = policy.StatusHistory[0];
        entry.FromStatus.Should().Be(PolicyStatus.Ativa);
        entry.ToStatus.Should().Be(PolicyStatus.Cancelada);
        entry.Reason.Should().Be("non-payment");
        entry.ChangedAt.Should().Be(later);
    }
}
