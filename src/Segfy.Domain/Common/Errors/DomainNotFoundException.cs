namespace Segfy.Domain.Common.Errors;

public sealed class DomainNotFoundException : DomainException
{
    public DomainNotFoundException(string message) : base(message) { }

    public override string Code => "NOT_FOUND";
}
