using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Segfy.Api.IntegrationTests;

public sealed class PoliciesEndpointsTests : IClassFixture<SegfyWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PoliciesEndpointsTests(SegfyWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private sealed record CreatePayload(
        string Document, string LicensePlate, decimal PremiumAmount,
        DateOnly CoverageStart, DateOnly CoverageEnd);

    private sealed record UpdatePayload(
        string Document, string LicensePlate, decimal PremiumAmount,
        DateOnly CoverageStart, DateOnly CoverageEnd, string Status, string? StatusReason);

    private sealed record PolicyDto(
        Guid Id, string Number, string Document, string LicensePlate,
        decimal PremiumAmount, DateOnly CoverageStart, DateOnly CoverageEnd,
        string Status, DateTime CreatedAt, DateTime UpdatedAt);

    private sealed record PolicyListDto(List<PolicyDto> Data);

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
    }

    [Fact]
    public async Task CreatePolicy_Then_GetById_RoundTrip()
    {
        var payload = new CreatePayload(
            "52998224725", "ABC1234", 249.90m,
            new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31));

        var post = await _client.PostAsJsonAsync("/api/v1/policies", payload);
        post.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await post.Content.ReadFromJsonAsync<PolicyDto>();
        created.Should().NotBeNull();
        created!.Number.Should().StartWith("SEG-").And.HaveLength(13);
        created.Status.Should().Be("Ativa");
        created.Document.Should().Be("52998224725");
        created.LicensePlate.Should().Be("ABC1234");

        var get = await _client.GetAsync($"/api/v1/policies/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await get.Content.ReadFromJsonAsync<PolicyDto>();
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
        fetched.PremiumAmount.Should().Be(249.90m);
    }

    [Fact]
    public async Task PostWithInvalidDocument_Returns400WithDomainValidationCode()
    {
        var payload = new CreatePayload(
            "00000000000", "DEF2G34", 100m,
            new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31));

        var response = await _client.PostAsJsonAsync("/api/v1/policies", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("DOMAIN_VALIDATION");
    }

    [Fact]
    public async Task CreateTwiceSamePlate_SecondCallIsRejected()
    {
        var first = new CreatePayload(
            "39053344705", "GHI5678", 150m,
            new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31));

        (await _client.PostAsJsonAsync("/api/v1/policies", first))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var second = first with { Document = "11144477735" };
        var response = await _client.PostAsJsonAsync("/api/v1/policies", second);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("already an active policy");
    }

    [Fact]
    public async Task CancelPolicy_RecordsHistory()
    {
        var payload = new CreatePayload(
            "52998224725", "JKL9012", 199.90m,
            new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31));

        var post = await _client.PostAsJsonAsync("/api/v1/policies", payload);
        var created = (await post.Content.ReadFromJsonAsync<PolicyDto>())!;

        var update = new UpdatePayload(
            payload.Document, payload.LicensePlate, payload.PremiumAmount,
            payload.CoverageStart, payload.CoverageEnd,
            "Cancelada", "customer request");

        var put = await _client.PutAsJsonAsync($"/api/v1/policies/{created.Id}", update);
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await _client.GetAsync($"/api/v1/policies/{created.Id}/history");
        history.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await history.Content.ReadAsStringAsync();
        body.Should().Contain("Ativa").And.Contain("Cancelada").And.Contain("customer request");
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404WithNotFoundCode()
    {
        var response = await _client.GetAsync($"/api/v1/policies/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("NOT_FOUND");
    }

    [Fact]
    public async Task UpdatePolicy_ValidDetailsChange_Returns200WithNewValues()
    {
        var payload = new CreatePayload(
            "11144477735", "STU4B56", 100.00m,
            new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31));
        var post = await _client.PostAsJsonAsync("/api/v1/policies", payload);
        var created = (await post.Content.ReadFromJsonAsync<PolicyDto>())!;

        var update = new UpdatePayload(
            payload.Document, payload.LicensePlate, 123.45m,
            payload.CoverageStart, payload.CoverageEnd, "Ativa", null);
        var put = await _client.PutAsJsonAsync($"/api/v1/policies/{created.Id}", update);

        put.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await put.Content.ReadFromJsonAsync<PolicyDto>())!;
        updated.PremiumAmount.Should().Be(123.45m);
        updated.Status.Should().Be("Ativa");
    }

    [Fact]
    public async Task UpdateCancelledPolicyBackToAtiva_Returns422InvalidState()
    {
        var payload = new CreatePayload(
            "39053344705", "VWX7C89", 150.00m,
            new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31));
        var post = await _client.PostAsJsonAsync("/api/v1/policies", payload);
        var created = (await post.Content.ReadFromJsonAsync<PolicyDto>())!;

        var cancel = new UpdatePayload(
            payload.Document, payload.LicensePlate, payload.PremiumAmount,
            payload.CoverageStart, payload.CoverageEnd, "Cancelada", "test");
        (await _client.PutAsJsonAsync($"/api/v1/policies/{created.Id}", cancel))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var reactivate = cancel with { Status = "Ativa", StatusReason = null };
        var response = await _client.PutAsJsonAsync($"/api/v1/policies/{created.Id}", reactivate);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("INVALID_STATE");
    }

    [Fact]
    public async Task DeletePolicy_Returns204_ThenGetReturns404()
    {
        var payload = new CreatePayload(
            "52998224725", "NOP1234", 199.90m,
            new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31));
        var post = await _client.PostAsJsonAsync("/api/v1/policies", payload);
        var created = (await post.Content.ReadFromJsonAsync<PolicyDto>())!;

        var delete = await _client.DeleteAsync($"/api/v1/policies/{created.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _client.GetAsync($"/api/v1/policies/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExpiringEndpoint_IncludesPolicyEndingWithinWindow()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var payload = new CreatePayload(
            "06990590000123", "KLM9012", 150.00m,
            today.AddDays(-10), today.AddDays(10));
        var post = await _client.PostAsJsonAsync("/api/v1/policies", payload);
        post.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await post.Content.ReadFromJsonAsync<PolicyDto>())!;

        var response = await _client.GetAsync("/api/v1/policies/expiring");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<PolicyListDto>())!;
        body.Data.Should().Contain(p => p.Id == created.Id);
    }

    [Fact]
    public async Task ListPolicies_FilterAtivaSortByPremiumAsc_ReturnsOrderedActives()
    {
        var cheap = new CreatePayload(
            "11222333000181", "TUV5678", 50.00m,
            new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31));
        var pricey = cheap with { Document = "11144477735", LicensePlate = "QRS1234", PremiumAmount = 999.99m };
        (await _client.PostAsJsonAsync("/api/v1/policies", cheap))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        (await _client.PostAsJsonAsync("/api/v1/policies", pricey))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await _client.GetAsync(
            "/api/v1/policies?status=Ativa&sortBy=premium&sortDir=asc&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<PolicyListDto>())!;
        body.Data.Should().OnlyContain(p => p.Status == "Ativa");
        body.Data.Select(p => p.PremiumAmount).Should().BeInAscendingOrder();
        body.Data.Select(p => p.LicensePlate).Should().Contain(new[] { "TUV5678", "QRS1234" });
    }
}
