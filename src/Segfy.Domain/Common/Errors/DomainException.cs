namespace Segfy.Domain.Common.Errors;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }

    public abstract string Code { get; }
}
