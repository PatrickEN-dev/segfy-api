using FluentAssertions;
using Segfy.Application.DTOs;
using Segfy.Application.Tests.Fakes;
using Segfy.Application.UseCases.Policies;
using Segfy.Domain.Common.Errors;
using Segfy.Domain.Policies;
using Xunit;

namespace Segfy.Application.Tests.UseCases;

public sealed class UpdatePolicyUseCaseTests
{
    private static async Task<(InMemoryPolicyRepository Repo, FakeClock Clock, Guid PolicyId)> SeedOneAsync()
    {
        var repo = new InMemoryPolicyRepository();
        var clock = new FakeClock();
        var seq = new FakePolicyNumberSequence();
        var create = new CreatePolicyUseCase(repo, seq, clock);
        var policy = await create.ExecuteAsync(
            new CreatePolicyInput("52998224725", "ABC1234", 199.90m,
                new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30)),
            CancellationToken.None);
        return (repo, clock, policy.Id);
    }

    private static UpdatePolicyInput Input(
        string document = "52998224725",
        string plate = "ABC1234",
        decimal premium = 199.90m,
        DateOnly? start = null,
        DateOnly? end = null,
        PolicyStatus status = PolicyStatus.Ativa,
        string? reason = null) =>
        new(document, plate, premium,
            start ?? new DateOnly(2026, 7, 1),
            end ?? new DateOnly(2027, 6, 30),
            status, reason);

    [Fact]
    public async Task Execute_ValidUpdate_UpdatesFields()
    {
        var (repo, clock, id) = await SeedOneAsync();
        var useCase = new UpdatePolicyUseCase(repo, clock);

        var updated = await useCase.ExecuteAsync(
            id,
            Input(document: "39053344705", plate: "DEF2G34", premium: 249.50m,
                end: new DateOnly(2027, 12, 31)),
            CancellationToken.None);

        updated.Document.Digits.Should().Be("39053344705");
        updated.LicensePlate.Value.Should().Be("DEF2G34");
        updated.Premium.Amount.Should().Be(249.50m);
        updated.CoverageEnd.Should().Be(new DateOnly(2027, 12, 31));
    }

    [Fact]
    public async Task Execute_UnknownId_ThrowsNotFound()
    {
        var repo = new InMemoryPolicyRepository();
        var clock = new FakeClock();
        var useCase = new UpdatePolicyUseCase(repo, clock);

        var act = async () => await useCase.ExecuteAsync(
            Guid.NewGuid(), Input(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainNotFoundException>();
    }

    [Fact]
    public async Task Execute_InvalidStatusTransition_ThrowsInvalidState()
    {
        var (repo, clock, id) = await SeedOneAsync();
        var useCase = new UpdatePolicyUseCase(repo, clock);
        await useCase.ExecuteAsync(
            id, Input(status: PolicyStatus.Cancelada, reason: "customer request"),
            CancellationToken.None);

        var act = async () => await useCase.ExecuteAsync(
            id, Input(status: PolicyStatus.Expirada), CancellationToken.None);

        await act.Should().ThrowAsync<DomainInvalidStateException>();
    }

    [Fact]
    public async Task Execute_ChangeStatusWithReason_RecordsHistory()
    {
        var (repo, clock, id) = await SeedOneAsync();
        var useCase = new UpdatePolicyUseCase(repo, clock);

        await useCase.ExecuteAsync(
            id, Input(status: PolicyStatus.Cancelada, reason: "non-payment"),
            CancellationToken.None);

        var policy = await repo.FindByIdAsync(id, CancellationToken.None);
        policy!.StatusHistory.Should().ContainSingle();
        policy.StatusHistory[0].FromStatus.Should().Be(PolicyStatus.Ativa);
        policy.StatusHistory[0].ToStatus.Should().Be(PolicyStatus.Cancelada);
        policy.StatusHistory[0].Reason.Should().Be("non-payment");
    }

    [Fact]
    public async Task Execute_BlankStatusReason_StoresNullInHistory()
    {
        var (repo, clock, id) = await SeedOneAsync();
        var useCase = new UpdatePolicyUseCase(repo, clock);

        await useCase.ExecuteAsync(
            id, Input(status: PolicyStatus.Cancelada, reason: "   "),
            CancellationToken.None);

        var policy = await repo.FindByIdAsync(id, CancellationToken.None);
        policy!.StatusHistory[0].Reason.Should().BeNull();
    }

    [Fact]
    public async Task Execute_PaddedStatusReason_IsTrimmed()
    {
        var (repo, clock, id) = await SeedOneAsync();
        var useCase = new UpdatePolicyUseCase(repo, clock);

        await useCase.ExecuteAsync(
            id, Input(status: PolicyStatus.Cancelada, reason: "  late payment  "),
            CancellationToken.None);

        var policy = await repo.FindByIdAsync(id, CancellationToken.None);
        policy!.StatusHistory[0].Reason.Should().Be("late payment");
    }

    [Fact]
    public async Task Execute_ChangingCoverageEndToPast_ThrowsDomainValidation()
    {
        var (repo, clock, id) = await SeedOneAsync();
        var useCase = new UpdatePolicyUseCase(repo, clock);

        // FakeClock today is 2026-07-01; moving the end date to before that must fail.
        var act = async () => await useCase.ExecuteAsync(
            id, Input(start: new DateOnly(2025, 1, 1), end: new DateOnly(2026, 6, 15)),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("*earlier than today*");
    }

    [Fact]
    public async Task Execute_UpdateDetailsOnCancelledPolicy_ThrowsInvalidState()
    {
        var (repo, clock, id) = await SeedOneAsync();
        var useCase = new UpdatePolicyUseCase(repo, clock);
        await useCase.ExecuteAsync(
            id, Input(status: PolicyStatus.Cancelada), CancellationToken.None);

        var act = async () => await useCase.ExecuteAsync(
            id,
            Input(document: "39053344705", plate: "DEF2G34", premium: 249.50m,
                end: new DateOnly(2027, 12, 31), status: PolicyStatus.Cancelada),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainInvalidStateException>();
    }
}
