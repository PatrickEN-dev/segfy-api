using FluentValidation;
using Segfy.Api.Contracts;
using Segfy.Domain.Policies;

namespace Segfy.Api.Validators;

public sealed class UpdatePolicyRequestValidator : PolicyRequestValidatorBase<UpdatePolicyRequest>
{
    public UpdatePolicyRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(BeKnownStatus)
            .WithMessage("Status must be one of: Ativa, Cancelada, Expirada.");

        RuleFor(x => x.StatusReason)
            .MaximumLength(500)
            .When(x => x.StatusReason is not null);
    }

    private static bool BeKnownStatus(string? raw) =>
        !string.IsNullOrWhiteSpace(raw)
        && Enum.TryParse<PolicyStatus>(raw, ignoreCase: true, out var status)
        && Enum.IsDefined(status);
}
