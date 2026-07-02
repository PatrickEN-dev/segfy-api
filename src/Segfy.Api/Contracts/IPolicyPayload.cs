namespace Segfy.Api.Contracts;

public interface IPolicyPayload
{
    string Document { get; }
    string LicensePlate { get; }
    decimal PremiumAmount { get; }
    DateOnly CoverageStart { get; }
    DateOnly CoverageEnd { get; }
}
