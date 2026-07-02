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

    [Fact]
    public async Task Execute_ValidUpdate_UpdatesFields()
    {
        var (repo, clock, id) = await SeedOneAsync();
        var useCase = new UpdatePolicyUseCase(repo, clock);

        var updated = await useCase.ExecuteAsync(
            id,
            new UpdatePolicyInput("39053344705", "DEF2G34", 249.50m,
                new DateOnly(2026, 7, 1), new DateOnly(2027, 12, 31), "Ativa", null),
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
            Guid.NewGuid(),
            new UpdatePolicyInput("52998224725", "ABC1234", 199.90m,
                new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "Ativa", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainNotFoundException>();
    }

    [Fact]
    public async Task Execute_InvalidStatusTransition_ThrowsInvalidState()
    {
        var (repo, clock, id) = await SeedOneAsync();
        var useCase = new UpdatePolicyUseCase(repo, clock);
        await useCase.ExecuteAsync(
            id,
            new UpdatePolicyInput("52998224725", "ABC1234", 199.90m,
                new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30),
                nameof(PolicyStatus.Cancelada), "customer request"),
            CancellationToken.None);

        var act = async () => await useCase.ExecuteAsync(
            id,
            new UpdatePolicyInput("52998224725", "ABC1234", 199.90m,
                new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30),
                nameof(PolicyStatus.Expirada), null),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainInvalidStateException>();
    }

    [Fact]
    public async Task Execute_ChangeStatusWithReason_RecordsHistory()
    {
        var (repo, clock, id) = await SeedOneAsync();
        var useCase = new UpdatePolicyUseCase(repo, clock);

        await useCase.ExecuteAsync(
            id,
            new UpdatePolicyInput("52998224725", "ABC1234", 199.90m,
                new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30),
                nameof(PolicyStatus.Cancelada), "non-payment"),
            CancellationToken.None);

        var policy = await repo.FindByIdAsync(id, CancellationToken.None);
        policy!.StatusHistory.Should().ContainSingle();
        policy.StatusHistory[0].FromStatus.Should().Be(PolicyStatus.Ativa);
        policy.StatusHistory[0].ToStatus.Should().Be(PolicyStatus.Cancelada);
        policy.StatusHistory[0].Reason.Should().Be("non-payment");
    }

    [Fact]
    public async Task Execute_UpdateDetailsOnCancelledPolicy_ThrowsInvalidState()
    {
        var (repo, clock, id) = await SeedOneAsync();
        var useCase = new UpdatePolicyUseCase(repo, clock);
        await useCase.ExecuteAsync(
            id,
            new UpdatePolicyInput("52998224725", "ABC1234", 199.90m,
                new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30),
                nameof(PolicyStatus.Cancelada), null),
            CancellationToken.None);

        var act = async () => await useCase.ExecuteAsync(
            id,
            new UpdatePolicyInput("39053344705", "DEF2G34", 249.50m,
                new DateOnly(2026, 7, 1), new DateOnly(2027, 12, 31),
                nameof(PolicyStatus.Cancelada), null),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainInvalidStateException>();
    }
}
