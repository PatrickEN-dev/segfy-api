using FluentAssertions;
using Segfy.Application.DTOs;
using Segfy.Application.Tests.Fakes;
using Segfy.Application.UseCases.Policies;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.Abstractions;
using Xunit;

namespace Segfy.Application.Tests.UseCases;

public sealed class ListPoliciesUseCaseTests
{
    private static ListPoliciesInput DefaultInput(
        int page = 1,
        int pageSize = 20,
        PolicyStatus? status = null,
        string? document = null,
        string? licensePlate = null,
        string? number = null,
        PolicySortField sortBy = PolicySortField.CreatedAt,
        SortDirection sortDir = SortDirection.Desc) =>
        new(page, pageSize, status, document, licensePlate, number, sortBy, sortDir);

    private static readonly string[] SeedPlates = { "ABC1234", "DEF2G34", "GHI5678", "JKL9012", "MNO3456", "PQR7A89" };

    private static async Task SeedAsync(InMemoryPolicyRepository repo, int count)
    {
        var clock = new FakeClock();
        var seq = new FakePolicyNumberSequence();
        var create = new CreatePolicyUseCase(repo, seq, clock);
        for (var i = 0; i < count; i++)
        {
            await create.ExecuteAsync(
                new CreatePolicyInput("52998224725", SeedPlates[i % SeedPlates.Length], 199.90m,
                    new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30)),
                CancellationToken.None);
        }
    }

    [Fact]
    public async Task Execute_ReturnsPaginatedResult()
    {
        var repo = new InMemoryPolicyRepository();
        await SeedAsync(repo, 3);
        var useCase = new ListPoliciesUseCase(repo);

        var result = await useCase.ExecuteAsync(DefaultInput(), CancellationToken.None);

        result.Total.Should().Be(3);
        result.Data.Should().HaveCount(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Execute_EmptyRepository_ReturnsEmpty()
    {
        var repo = new InMemoryPolicyRepository();
        var useCase = new ListPoliciesUseCase(repo);

        var result = await useCase.ExecuteAsync(DefaultInput(), CancellationToken.None);

        result.Total.Should().Be(0);
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ClampsPageSizeAbove100()
    {
        var repo = new InMemoryPolicyRepository();
        var useCase = new ListPoliciesUseCase(repo);

        var result = await useCase.ExecuteAsync(DefaultInput(pageSize: 500), CancellationToken.None);

        result.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task Execute_ClampsPageBelow1()
    {
        var repo = new InMemoryPolicyRepository();
        var useCase = new ListPoliciesUseCase(repo);

        var result = await useCase.ExecuteAsync(DefaultInput(page: 0), CancellationToken.None);

        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task Execute_FilterByStatus_ReturnsOnlyMatching()
    {
        var repo = new InMemoryPolicyRepository();
        await SeedAsync(repo, 2);
        var clock = new FakeClock();
        (await repo.FindByIdAsync(repo.Snapshot[0].Id, CancellationToken.None))!
            .ChangeStatus(PolicyStatus.Cancelada, clock.UtcNow);
        var useCase = new ListPoliciesUseCase(repo);

        var result = await useCase.ExecuteAsync(
            DefaultInput(status: PolicyStatus.Cancelada), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Data.Should().OnlyContain(p => p.Status == PolicyStatus.Cancelada);
    }
}
