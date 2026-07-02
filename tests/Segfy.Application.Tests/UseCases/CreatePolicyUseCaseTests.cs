using FluentAssertions;
using Segfy.Application.DTOs;
using Segfy.Application.Tests.Fakes;
using Segfy.Application.UseCases.Policies;
using Segfy.Domain.Common.Errors;
using Segfy.Domain.Policies;
using Xunit;

namespace Segfy.Application.Tests.UseCases;

public sealed class CreatePolicyUseCaseTests
{
    private static CreatePolicyInput ValidInput() => new(
        "52998224725",
        "ABC1234",
        199.90m,
        new DateOnly(2026, 7, 1),
        new DateOnly(2027, 6, 30));

    [Fact]
    public async Task Execute_WithValidInput_ReturnsPolicyWithGeneratedNumber()
    {
        var repo = new InMemoryPolicyRepository();
        var clock = new FakeClock();
        var seq = new FakePolicyNumberSequence();
        var useCase = new CreatePolicyUseCase(repo, seq, clock);

        var policy = await useCase.ExecuteAsync(ValidInput(), CancellationToken.None);

        policy.Number.Value.Should().Be("SEG-2026-0001");
        policy.Status.Should().Be(PolicyStatus.Ativa);
        repo.Snapshot.Should().ContainSingle();
    }

    [Fact]
    public async Task Execute_WithInvalidDocument_ThrowsDomainValidation()
    {
        var repo = new InMemoryPolicyRepository();
        var clock = new FakeClock();
        var seq = new FakePolicyNumberSequence();
        var useCase = new CreatePolicyUseCase(repo, seq, clock);
        var invalid = ValidInput() with { Document = "00000000000" };

        var act = async () => await useCase.ExecuteAsync(invalid, CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task Execute_TwiceSameYear_IncrementsSequential()
    {
        var repo = new InMemoryPolicyRepository();
        var clock = new FakeClock();
        var seq = new FakePolicyNumberSequence();
        var useCase = new CreatePolicyUseCase(repo, seq, clock);

        var first = await useCase.ExecuteAsync(ValidInput(), CancellationToken.None);
        var second = await useCase.ExecuteAsync(
            ValidInput() with { LicensePlate = "DEF2G34" }, CancellationToken.None);

        first.Number.Value.Should().Be("SEG-2026-0001");
        second.Number.Value.Should().Be("SEG-2026-0002");
    }

    [Fact]
    public async Task Execute_DuplicateActivePlate_ThrowsDomainValidation()
    {
        var repo = new InMemoryPolicyRepository();
        var clock = new FakeClock();
        var seq = new FakePolicyNumberSequence();
        var useCase = new CreatePolicyUseCase(repo, seq, clock);
        await useCase.ExecuteAsync(ValidInput(), CancellationToken.None);

        var act = async () => await useCase.ExecuteAsync(
            ValidInput() with { Document = "39053344705" }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("There is already an active policy*");
    }
}
