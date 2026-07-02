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

        if (!Enum.TryParse<PolicyStatus>(input.Status, ignoreCase: true, out var desiredStatus)
            || !Enum.IsDefined(desiredStatus))
        {
            throw new DomainValidationException(
                "Status must be one of: Ativa, Cancelada, Expirada.");
        }

        var document = Document.Create(input.Document);
        var plate = LicensePlate.Create(input.LicensePlate);
        var premium = Money.Create(input.PremiumAmount);
        var coverage = CoveragePeriod.Create(input.CoverageStart, input.CoverageEnd);

        var plateChanged = plate.Value != policy.LicensePlate.Value;
        var isDetailsChange =
            document.Digits != policy.Document.Digits
            || plateChanged
            || premium.Amount != policy.Premium.Amount
            || coverage.Start != policy.CoverageStart
            || coverage.End != policy.CoverageEnd;

        // Check plate uniqueness whenever the resulting state will be Ativa AND either
        // the plate is changing OR the policy is (re)entering the Ativa state from a
        // different one. The DB partial-unique index is the ultimate guard, but this
        // gives the caller a clean DOMAIN_VALIDATION response instead of a raw conflict.
        var wasActive = policy.Status == PolicyStatus.Ativa;
        var willBeActive = desiredStatus == PolicyStatus.Ativa;
        var needsPlateUniquenessCheck = willBeActive && (plateChanged || !wasActive);

        if (needsPlateUniquenessCheck
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
