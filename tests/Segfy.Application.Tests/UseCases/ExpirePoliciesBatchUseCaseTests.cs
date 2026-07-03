using FluentAssertions;
using Segfy.Application.Tests.Fakes;
using Segfy.Application.UseCases.Policies;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.ValueObjects;
using Xunit;

namespace Segfy.Application.Tests.UseCases;

public sealed class ExpirePoliciesBatchUseCaseTests
{
    // Built straight from the domain because CreatePolicyUseCase (correctly)
    // rejects new policies whose coverage has already ended.
    private static Policy NewPolicy(string plate, DateOnly start, DateOnly end, DateTime nowUtc, int seq) =>
        Policy.Create(
            PolicyNumber.Create(2026, seq),
            Document.Create("52998224725"),
            LicensePlate.Create(plate),
            Money.Create(100m),
            CoveragePeriod.Create(start, end),
            nowUtc);

    [Fact]
    public async Task Execute_ExpiresOnlyActivePoliciesWhoseCoverageEnded()
    {
        var repo = new InMemoryPolicyRepository();
        var clock = new FakeClock(); // today = 2026-07-01
        var ended1 = NewPolicy("ABC1234", new DateOnly(2025, 7, 1), new DateOnly(2026, 6, 30), clock.UtcNow, 1);
        var ended2 = NewPolicy("DEF2G34", new DateOnly(2025, 7, 1), new DateOnly(2026, 5, 31), clock.UtcNow, 2);
        var current = NewPolicy("GHI5678", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), clock.UtcNow, 3);
        await repo.AddAsync(ended1, CancellationToken.None);
        await repo.AddAsync(ended2, CancellationToken.None);
        await repo.AddAsync(current, CancellationToken.None);
        var useCase = new ExpirePoliciesBatchUseCase(repo, clock);

        var count = await useCase.ExecuteAsync(CancellationToken.None);

        count.Should().Be(2);
        ended1.Status.Should().Be(PolicyStatus.Expirada);
        ended2.Status.Should().Be(PolicyStatus.Expirada);
        current.Status.Should().Be(PolicyStatus.Ativa);
        ended1.StatusHistory.Should()
            .ContainSingle(h => h.Reason == ExpirePoliciesBatchUseCase.AutomatedReason);
    }

    [Fact]
    public async Task Execute_NothingToExpire_ReturnsZero()
    {
        var repo = new InMemoryPolicyRepository();
        var clock = new FakeClock();
        var current = NewPolicy("ABC1234", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), clock.UtcNow, 1);
        await repo.AddAsync(current, CancellationToken.None);
        var useCase = new ExpirePoliciesBatchUseCase(repo, clock);

        var count = await useCase.ExecuteAsync(CancellationToken.None);

        count.Should().Be(0);
        current.Status.Should().Be(PolicyStatus.Ativa);
    }
}
