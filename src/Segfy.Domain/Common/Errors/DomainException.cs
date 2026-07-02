namespace Segfy.Domain.Common.Errors;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    public abstract string Code { get; }
}

public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message) { }
    public override string Code => "DOMAIN_VALIDATION";
}

public sealed class DomainInvalidStateException : DomainException
{
    public DomainInvalidStateException(string message) : base(message) { }
    public override string Code => "INVALID_STATE";
}

public sealed class DomainNotFoundException : DomainException
{
    public DomainNotFoundException(string message) : base(message) { }
    public override string Code => "NOT_FOUND";
}
