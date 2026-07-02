using Segfy.Application.Abstractions;
using Segfy.Application.DTOs;
using Segfy.Domain.Common.Errors;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.Abstractions;
using Segfy.Domain.Policies.ValueObjects;

namespace Segfy.Application.UseCases.Policies;

public sealed class UpdatePolicyUseCase(IPolicyRepository repo, IClock clock)
{
    private readonly IPolicyRepository _repo = repo;
    private readonly IClock _clock = clock;

    public async Task<Policy> ExecuteAsync(Guid id, UpdatePolicyInput input, CancellationToken ct)
    {
        var policy = await _repo.FindByIdAsync(id, ct)
            ?? throw new DomainNotFoundException($"Policy {id} not found.");

        if (!Enum.TryParse<PolicyStatus>(input.Status, ignoreCase: false, out var desiredStatus))
            throw new DomainValidationException(
                "Status must be one of: Ativa, Cancelada, Expirada.");

        var document = Document.Create(input.Document);
        var plate = LicensePlate.Create(input.LicensePlate);
        var premium = Money.Create(input.PremiumAmount);
        var coverage = CoveragePeriod.Create(input.CoverageStart, input.CoverageEnd);

        var isDetailsChange =
            document.Digits != policy.Document.Digits
            || plate.Value != policy.LicensePlate.Value
            || premium.Amount != policy.Premium.Amount
            || coverage.Start != policy.CoverageStart
            || coverage.End != policy.CoverageEnd;

        if (desiredStatus == PolicyStatus.Ativa
            && plate.Value != policy.LicensePlate.Value
            && await _repo.ExistsActiveByPlateAsync(plate.Value, id, ct))
        {
            throw new DomainValidationException(
                $"There is already an active policy for vehicle {plate.Value}.");
        }

        if (isDetailsChange)
            policy.UpdateDetails(document, plate, premium, coverage, _clock.UtcNow);

        if (desiredStatus != policy.Status)
            policy.ChangeStatus(desiredStatus, _clock.UtcNow, input.StatusReason);

        await _repo.UpdateAsync(policy, ct);
        return policy;
    }
}
