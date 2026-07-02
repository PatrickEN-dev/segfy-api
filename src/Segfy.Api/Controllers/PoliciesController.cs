using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Segfy.Api.Contracts;
using Segfy.Api.Presenters;
using Segfy.Application.Abstractions;
using Segfy.Application.Configuration;
using Segfy.Application.DTOs;
using Segfy.Application.UseCases.Policies;
using Segfy.Domain.Policies;
using Segfy.Domain.Policies.Abstractions;

namespace Segfy.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class PoliciesController : ControllerBase
{
    private readonly IValidator<CreatePolicyRequest> _createValidator;
    private readonly IValidator<UpdatePolicyRequest> _updateValidator;
    private readonly CreatePolicyUseCase _create;
    private readonly GetPolicyByIdUseCase _get;
    private readonly ListPoliciesUseCase _list;
    private readonly UpdatePolicyUseCase _update;
    private readonly DeletePolicyUseCase _delete;
    private readonly GetExpiringPoliciesUseCase _expiring;
    private readonly GetPolicyStatusHistoryUseCase _history;
    private readonly IClock _clock;
    private readonly IOptions<SegfyOptions> _options;

    public PoliciesController(
        IValidator<CreatePolicyRequest> createValidator,
        IValidator<UpdatePolicyRequest> updateValidator,
        CreatePolicyUseCase create,
        GetPolicyByIdUseCase get,
        ListPoliciesUseCase list,
        UpdatePolicyUseCase update,
        DeletePolicyUseCase delete,
        GetExpiringPoliciesUseCase expiring,
        GetPolicyStatusHistoryUseCase history,
        IClock clock,
        IOptions<SegfyOptions> options)
    {
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _create = create;
        _get = get;
        _list = list;
        _update = update;
        _delete = delete;
        _expiring = expiring;
        _history = history;
        _clock = clock;
        _options = options;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PolicyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePolicyRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var input = new CreatePolicyInput(
            request.Document,
            request.LicensePlate,
            request.PremiumAmount,
            request.CoverageStart,
            request.CoverageEnd);

        var policy = await _create.ExecuteAsync(input, ct);
        var response = PolicyPresenter.ToResponse(policy);
        return CreatedAtAction(nameof(GetById), new { id = policy.Id }, response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var policy = await _get.ExecuteAsync(id, ct);
        return Ok(PolicyPresenter.ToResponse(policy));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedPoliciesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? document = null,
        [FromQuery] string? licensePlate = null,
        [FromQuery] string? number = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        CancellationToken ct = default)
    {
        var statusFilter = ParseStatus(status);
        var input = new ListPoliciesInput(
            page,
            pageSize,
            statusFilter,
            document,
            licensePlate,
            number,
            ParseSortField(sortBy),
            ParseSortDir(sortDir));

        var result = await _list.ExecuteAsync(input, ct);
        var totalPages = result.PageSize <= 0
            ? 0
            : (int)Math.Ceiling(result.Total / (double)result.PageSize);
        var meta = new PageMeta(result.Page, result.PageSize, result.Total, totalPages);
        return Ok(new PaginatedPoliciesResponse(PolicyPresenter.ToResponseList(result.Data), meta));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdatePolicyRequest request,
        CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var input = new UpdatePolicyInput(
            request.Document,
            request.LicensePlate,
            request.PremiumAmount,
            request.CoverageStart,
            request.CoverageEnd,
            request.Status,
            request.StatusReason);

        var policy = await _update.ExecuteAsync(id, input, ct);
        return Ok(PolicyPresenter.ToResponse(policy));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        await _delete.ExecuteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("expiring")]
    [ProducesResponseType(typeof(ExpiringPoliciesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Expiring(CancellationToken ct)
    {
        var policies = await _expiring.ExecuteAsync(ct);
        var meta = new ExpiringMeta(_options.Value.ExpiringWindowDays, _clock.TodayUtc);
        return Ok(new ExpiringPoliciesResponse(PolicyPresenter.ToResponseList(policies), meta));
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(StatusHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> History([FromRoute] Guid id, CancellationToken ct)
    {
        var entries = await _history.ExecuteAsync(id, ct);
        var data = entries.Select(PolicyPresenter.ToHistoryEntry).ToList();
        return Ok(new StatusHistoryResponse(data));
    }

    private static PolicyStatus? ParseStatus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (!Enum.TryParse<PolicyStatus>(raw, ignoreCase: false, out var s))
            throw new FluentValidation.ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    "status", "Status must be one of: Ativa, Cancelada, Expirada.")
            });
        return s;
    }

    private static PolicySortField ParseSortField(string? raw) =>
        raw?.Trim().ToLowerInvariant() switch
        {
            null or "" or "createdat" => PolicySortField.CreatedAt,
            "coverageend" => PolicySortField.CoverageEnd,
            "premium" => PolicySortField.Premium,
            _ => throw new FluentValidation.ValidationException(new[]
                {
                    new FluentValidation.Results.ValidationFailure(
                        "sortBy", "sortBy must be one of: createdAt, coverageEnd, premium."),
                }),
        };

    private static SortDirection ParseSortDir(string? raw) =>
        raw?.Trim().ToLowerInvariant() switch
        {
            null or "" or "desc" => SortDirection.Desc,
            "asc" => SortDirection.Asc,
            _ => throw new FluentValidation.ValidationException(new[]
                {
                    new FluentValidation.Results.ValidationFailure(
                        "sortDir", "sortDir must be one of: asc, desc."),
                }),
        };
}
