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

        var document = Document.Create(input.Document);
        var plate = LicensePlate.Create(input.LicensePlate);
        var premium = Money.Create(input.PremiumAmount);
        var coverage = CoveragePeriod.Create(input.CoverageStart, input.CoverageEnd);

        // Only enforced when the end date is actually being moved, so unrelated
        // edits on a policy whose coverage already lapsed are not blocked.
        if (coverage.End != policy.CoverageEnd && coverage.End < _clock.TodayUtc)
            throw new DomainValidationException("CoverageEnd cannot be earlier than today.");

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
        var willBeActive = input.Status == PolicyStatus.Ativa;

        if (willBeActive
            && (plateChanged || !wasActive)
            && await _repo.ExistsActiveByPlateAsync(plate.Value, id, ct))
        {
            throw new DomainValidationException(
                $"There is already an active policy for vehicle {plate.Value}.");
        }

        if (isDetailsChange)
            policy.UpdateDetails(document, plate, premium, coverage, _clock.UtcNow);

        if (input.Status != policy.Status)
        {
            var reason = string.IsNullOrWhiteSpace(input.StatusReason)
                ? null
                : input.StatusReason.Trim();
            policy.ChangeStatus(input.Status, _clock.UtcNow, reason);
        }

        await _repo.UpdateAsync(policy, ct);
        return policy;
    }
}
