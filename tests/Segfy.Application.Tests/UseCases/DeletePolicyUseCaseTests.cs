using FluentAssertions;
using Segfy.Application.DTOs;
using Segfy.Application.Tests.Fakes;
using Segfy.Application.UseCases.Policies;
using Segfy.Domain.Common.Errors;
using Xunit;

namespace Segfy.Application.Tests.UseCases;

public sealed class DeletePolicyUseCaseTests
{
    [Fact]
    public async Task Execute_ExistingId_RemovesPolicy()
    {
        var repo = new InMemoryPolicyRepository();
        var clock = new FakeClock();
        var seq = new FakePolicyNumberSequence();
        var create = new CreatePolicyUseCase(repo, seq, clock);
        var policy = await create.ExecuteAsync(
            new CreatePolicyInput("52998224725", "ABC1234", 199.90m,
                new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30)),
            CancellationToken.None);

        var useCase = new DeletePolicyUseCase(repo);

        await useCase.ExecuteAsync(policy.Id, CancellationToken.None);

        repo.Snapshot.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_UnknownId_ThrowsNotFound()
    {
        var repo = new InMemoryPolicyRepository();
        var useCase = new DeletePolicyUseCase(repo);

        var act = async () => await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainNotFoundException>();
    }
}
