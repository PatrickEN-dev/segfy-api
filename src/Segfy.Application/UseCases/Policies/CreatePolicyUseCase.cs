using Segfy.Application.Abstractions;
using Segfy.Application.DTOs;
using Segfy.Domain.Common.Errors;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.Abstractions;
using Segfy.Domain.Policies.ValueObjects;

namespace Segfy.Application.UseCases.Policies;

public sealed class CreatePolicyUseCase(
    IPolicyRepository repo,
    IPolicyNumberSequence sequence,
    IClock clock)
{
    private readonly IPolicyRepository _repo = repo;
    private readonly IPolicyNumberSequence _sequence = sequence;
    private readonly IClock _clock = clock;

    public async Task<Policy> ExecuteAsync(CreatePolicyInput input, CancellationToken ct)
    {
        var document = Document.Create(input.Document);
        var plate = LicensePlate.Create(input.LicensePlate);
        var premium = Money.Create(input.PremiumAmount);
        var coverage = CoveragePeriod.Create(input.CoverageStart, input.CoverageEnd);

        if (await _repo.ExistsActiveByPlateAsync(plate.Value, excludePolicyId: null, ct))
            throw new DomainValidationException(
                $"There is already an active policy for vehicle {plate.Value}.");

        var year = _clock.UtcNow.Year;
        var seq = await _sequence.NextForYearAsync(year, ct);
        var number = PolicyNumber.Create(year, seq);

        var policy = Policy.Create(number, document, plate, premium, coverage, _clock.UtcNow);
        await _repo.AddAsync(policy, ct);
        return policy;
    }
}
