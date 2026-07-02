namespace Segfy.Domain.Common.Errors;

public sealed class DomainInvalidStateException : DomainException
{
    public DomainInvalidStateException(string message) : base(message) { }

    public override string Code => "INVALID_STATE";
}
