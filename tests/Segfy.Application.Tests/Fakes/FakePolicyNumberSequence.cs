using Segfy.Application.Abstractions;

namespace Segfy.Application.Tests.Fakes;

public sealed class FakePolicyNumberSequence : IPolicyNumberSequence
{
    private readonly Dictionary<int, int> _counters = new();

    public Task<int> NextForYearAsync(int year, CancellationToken ct)
    {
        if (!_counters.TryGetValue(year, out var current))
            current = 0;
        current++;
        _counters[year] = current;
        return Task.FromResult(current);
    }
}
