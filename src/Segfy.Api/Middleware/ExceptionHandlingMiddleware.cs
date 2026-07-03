using FluentValidation;
using Segfy.Api.Contracts;
using Segfy.Domain.Common.Errors;

namespace Segfy.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private static readonly Action<ILogger, Exception> LogUnhandled =
        LoggerMessage.Define(LogLevel.Error, new EventId(1, nameof(ExceptionHandlingMiddleware)),
            "Unhandled exception.");

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (ValidationException ex)
        {
            var details = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            await WriteError(ctx, StatusCodes.Status400BadRequest, "VALIDATION_ERROR",
                "One or more validation errors occurred.", details);
        }
        catch (DomainNotFoundException ex)
        {
            await WriteError(ctx, StatusCodes.Status404NotFound, ex.Code, ex.Message);
        }
        catch (DomainInvalidStateException ex)
        {
            await WriteError(ctx, StatusCodes.Status422UnprocessableEntity, ex.Code, ex.Message);
        }
        catch (DomainValidationException ex)
        {
            await WriteError(ctx, StatusCodes.Status400BadRequest, ex.Code, ex.Message);
        }
#pragma warning disable CA1031 
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogUnhandled(_logger, ex);
            await WriteError(ctx, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR",
                "An unexpected error occurred.");
        }
    }

    private static Task WriteError(
        HttpContext ctx,
        int status,
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? details = null)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        var body = new ErrorResponse(new ErrorBody(code, message, ctx.TraceIdentifier, details));
        return ctx.Response.WriteAsJsonAsync(body);
    }
}
