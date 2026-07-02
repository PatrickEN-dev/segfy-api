using FluentValidation;
using Segfy.Api.Contracts;

namespace Segfy.Api.Validators;

public sealed class CreatePolicyRequestValidator : AbstractValidator<CreatePolicyRequest>
{
    public CreatePolicyRequestValidator()
    {
        RuleFor(x => x.Document).NotEmpty().MaximumLength(20);
        RuleFor(x => x.LicensePlate).NotEmpty().MaximumLength(10);
        RuleFor(x => x.PremiumAmount).GreaterThan(0);
        RuleFor(x => x.CoverageStart)
            .GreaterThan(default(DateOnly))
            .WithMessage("CoverageStart is required.");
        RuleFor(x => x.CoverageEnd)
            .GreaterThan(default(DateOnly))
            .WithMessage("CoverageEnd is required.");
        RuleFor(x => x.CoverageEnd)
            .Must((req, end) => end > req.CoverageStart)
            .WithMessage("CoverageEnd must be greater than CoverageStart.");
    }
}
