using FluentAssertions;
using Microsoft.Extensions.Options;
using Segfy.Application.Configuration;
using Segfy.Application.DTOs;
using Segfy.Application.Tests.Fakes;
using Segfy.Application.UseCases.Policies;
using Segfy.Domain.Policies;
using Xunit;

namespace Segfy.Application.Tests.UseCases;

public sealed class GetExpiringPoliciesUseCaseTests
{
    private static IOptions<SegfyOptions> Options(int windowDays = 30) =>
        Microsoft.Extensions.Options.Options.Create(new SegfyOptions { ExpiringWindowDays = windowDays });

    private static readonly string[] Plates = { "ABC1234", "DEF2G34", "GHI5678", "JKL9012", "MNO3456" };
    private static int _plateIndex;

    private static async Task<Policy> CreateAsync(
        InMemoryPolicyRepository repo,
        FakeClock clock,
        DateOnly start,
        DateOnly end)
    {
        var seq = new FakePolicyNumberSequence();
        var create = new CreatePolicyUseCase(repo, seq, clock);
        var plate = Plates[_plateIndex++ % Plates.Length];
        return await create.ExecuteAsync(
            new CreatePolicyInput("52998224725", plate, 199.90m, start, end),
            CancellationToken.None);
    }

    [Fact]
    public async Task Execute_ReturnsAtivaPoliciesWithinWindow()
    {
        var repo = new InMemoryPolicyRepository();
        var clock = new FakeClock();
        var today = clock.TodayUtc;
        await CreateAsync(repo, clock, today.AddMonths(-1), today.AddDays(5));
        await CreateAsync(repo, clock, today.AddMonths(-1), today.AddDays(25));
        var useCase = new GetExpiringPoliciesUseCase(repo, clock, Options());

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(p => p.CoverageEnd).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Execute_IgnoresCanceladaAndExpirada()
    {
        var repo = new InMemoryPolicyRepository();
        var clock = new FakeClock();
        var today = clock.TodayUtc;
        var cancelled = await CreateAsync(repo, clock, today.AddMonths(-1), today.AddDays(10));
        cancelled.ChangeStatus(PolicyStatus.Cancelada, clock.UtcNow);
        var expired = await CreateAsync(repo, clock, today.AddMonths(-1), today.AddDays(15));
        expired.ChangeStatus(PolicyStatus.Expirada, clock.UtcNow);
        var useCase = new GetExpiringPoliciesUseCase(repo, clock, Options());

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_IgnoresBeyondWindow()
    {
        var repo = new InMemoryPolicyRepository();
        var clock = new FakeClock();
        var today = clock.TodayUtc;
        await CreateAsync(repo, clock, today.AddMonths(-1), today.AddDays(45));
        var useCase = new GetExpiringPoliciesUseCase(repo, clock, Options());

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_EmptyRepository_ReturnsEmpty()
    {
        var repo = new InMemoryPolicyRepository();
        var clock = new FakeClock();
        var useCase = new GetExpiringPoliciesUseCase(repo, clock, Options());

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }
}
