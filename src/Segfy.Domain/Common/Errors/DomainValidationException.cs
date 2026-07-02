namespace Segfy.Domain.Common.Errors;

public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message) { }

    public override string Code => "DOMAIN_VALIDATION";
}
