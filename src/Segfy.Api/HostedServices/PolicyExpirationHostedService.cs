using Microsoft.Extensions.Options;
using Segfy.Application.Configuration;
using Segfy.Application.UseCases.Policies;

namespace Segfy.Api.HostedServices;

public sealed class PolicyExpirationHostedService : BackgroundService
{
    private static readonly Action<ILogger, int, Exception?> LogExpired =
        LoggerMessage.Define<int>(LogLevel.Information,
            new EventId(1, nameof(PolicyExpirationHostedService)),
            "Auto-expired {Count} policies whose coverage ended.");

    private static readonly Action<ILogger, Exception> LogFailure =
        LoggerMessage.Define(LogLevel.Error,
            new EventId(2, nameof(PolicyExpirationHostedService)),
            "Auto-expiration job failed.");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<SegfyOptions> _options;
    private readonly ILogger<PolicyExpirationHostedService> _logger;

    public PolicyExpirationHostedService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<SegfyOptions> options,
        ILogger<PolicyExpirationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_options.CurrentValue.AutoExpirationEnabled)
                    await RunOnceAsync(stoppingToken);

                var delay = TimeSpan.FromSeconds(_options.CurrentValue.AutoExpirationIntervalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path.
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ExpirePoliciesBatchUseCase>();
            var expired = await useCase.ExecuteAsync(ct);
            if (expired > 0)
                LogExpired(_logger, expired, null);
        }
#pragma warning disable CA1031 // Background job must not crash on domain errors.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogFailure(_logger, ex);
        }
    }
}
