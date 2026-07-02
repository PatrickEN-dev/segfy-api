namespace Segfy.Application.Abstractions;

public interface IPolicyNumberSequence
{
    Task<int> NextForYearAsync(int year, CancellationToken ct);
}
