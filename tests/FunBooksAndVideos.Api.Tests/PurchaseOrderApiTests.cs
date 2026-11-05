using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using FunBooksAndVideos.Api.Endpoints;
using FunBooksAndVideos.Api.Processing;
using FunBooksAndVideos.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FunBooksAndVideos.Api.Tests;

/// <summary>The public contract on the wire: raw JSON in, status codes and JSON out, through the whole application.</summary>
public sealed class PurchaseOrderApiTests(AppFixture fixture)
{
    private const string Orders = "/v1/purchase-orders";

    private readonly HttpClient _http = fixture.App.CreateClient();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Submitting_an_order_answers_201_with_its_membership_already_active()
    {
        var order = AnOrder();

        var response = await _http.PostAsJsonAsync(Orders, order, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"{Orders}/{order["id"]}", response.Headers.Location?.OriginalString);
        // BR1: active by the time the caller hears back.
        var memberships = await _http.GetFromJsonAsync<JsonObject>($"/v1/customers/{order["customerId"]}/memberships", Ct);
        Assert.Equal("bookClub", (string?)Assert.Single(memberships!["clubs"]!.AsArray()));
    }

    [Fact]
    public async Task A_fulfilled_order_shows_its_shipping_slip()
    {
        var order = AnOrder();
        await _http.PostAsJsonAsync(Orders, order, Ct);
        await fixture.Temporal.GetWorkflowHandle(PurchaseOrderWorkflow.IdFor(new PurchaseOrderId((string)order["id"]!))).GetResultAsync();

        var stored = await _http.GetFromJsonAsync<JsonObject>($"{Orders}/{order["id"]}", Ct);

        Assert.Equal("completed", (string?)stored!["status"]);
        // BR2: the book ships; the video and the membership don't.
        var shipped = Assert.Single(stored["shippingSlip"]!["items"]!.AsArray());
        Assert.Equal("The Girl on the train", (string?)shipped!["title"]);
    }

    [Fact]
    public async Task Resubmitting_the_same_order_answers_200_with_that_order()
    {
        var order = AnOrder();
        await _http.PostAsJsonAsync(Orders, order, Ct);

        var response = await _http.PostAsJsonAsync(Orders, order, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((string?)order["id"], (string?)(await response.Content.ReadFromJsonAsync<JsonObject>(Ct))!["id"]);
    }

    [Fact]
    public async Task Reusing_an_id_for_a_different_order_answers_409()
    {
        var order = AnOrder();
        await _http.PostAsJsonAsync(Orders, order, Ct);
        order["total"] = 99.99m;

        await AssertProblemAsync(await _http.PostAsJsonAsync(Orders, order, Ct), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Simultaneous_orders_for_each_club_make_a_customer_premium()
    {
        var customerId = NewId();

        // Both race to create the same customer. The loser's activity fails on the conflict, and its retry builds on the winner.
        var responses = await Task.WhenAll(
            _http.PostAsJsonAsync(Orders, AnOrder(customerId, "bookClub"), Ct),
            _http.PostAsJsonAsync(Orders, AnOrder(customerId, "videoClub"), Ct));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        var memberships = await _http.GetFromJsonAsync<JsonObject>($"/v1/customers/{customerId}/memberships", Ct);

        Assert.True((bool)memberships!["premium"]!);
        Assert.Equal(["bookClub", "videoClub"], memberships["clubs"]!.AsArray().Select(club => (string?)club));
    }

    [Fact]
    public async Task Invalid_values_are_reported_together_by_field()
    {
        var order = AnOrder();
        order["id"] = "3344656\n";
        order["customerId"] = "";
        order["total"] = -1;
        order["items"]![1]!["title"] = new string('x', 201);

        var problem = await AssertProblemAsync(await _http.PostAsJsonAsync(Orders, order, Ct), HttpStatusCode.BadRequest);

        Assert.Equal(["customerId", "id", "items[1].title", "total"], problem["errors"]!.AsObject().Select(error => error.Key).Order());
    }

    [Theory]
    [InlineData("""{"id":"1","customerId":"c","total":1,"items":[{"title":"Dune"}]}""", "$.items[0]")]
    [InlineData("""{"id":"1","customerId":"c","total":1,"items":[{"type":"magazine","title":"Dune"}]}""", "$.items[0]")]
    [InlineData("""{"id":"1","customerId":"c","total":1,"items":[{"type":"book","title":"Dune"}],"colour":"red"}""", "$.colour")]
    [InlineData("""{"id":"1","customerId":"c","items":[{"type":"book","title":"Dune"}]}""", "$")]
    [InlineData("""{"id":"1","customerId":"c","total":"1","items":[{"type":"book","title":"Dune"}]}""", "$.total")]
    [InlineData("""{"id":"1","id":"2","customerId":"c","total":1,"items":[{"type":"book","title":"Dune"}]}""", "$.id")]
    [InlineData("""{"id":"1","customerId":"c","total":1,"items":[{"type":"book","title":null}]}""", "$.items[0].title")]
    [InlineData("""{"id":"1","customerId":"c","total":1,"items":[{"type":"membership"}]}""", "$.items[0]")]
    [InlineData("""{"id":"1","customerId":"c","total":1,"items":[{"type":"membership","membership":1}]}""", "$.items[0].membership")]
    [InlineData("""{"id":"1","customerId":"c","total":1,"items":[{"type":"membership","membership":"gold"}]}""", "$.items[0].membership")]
    [InlineData("not json", "$")]
    public async Task A_body_that_breaks_the_contract_answers_400_saying_where(string body, string path)
    {
        var response = await _http.PostAsync(Orders, new StringContent(body, Encoding.UTF8, "application/json"), Ct);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(path, (string?)problem["path"]);
    }

    [Fact]
    public async Task A_line_may_name_its_type_after_its_other_fields()
    {
        var order = AnOrder();
        order["items"] = JsonNode.Parse("""[{ "title": "Dune", "type": "book" }]""");

        Assert.Equal(HttpStatusCode.Created, (await _http.PostAsJsonAsync(Orders, order, Ct)).StatusCode);
    }

    [Fact]
    public async Task An_unknown_order_answers_404() =>
        await AssertProblemAsync(await _http.GetAsync($"{Orders}/{NewId()}", Ct), HttpStatusCode.NotFound);

    [Fact]
    public async Task A_customer_this_service_has_never_seen_belongs_to_no_club()
    {
        var memberships = await _http.GetFromJsonAsync<JsonObject>($"/v1/customers/{NewId()}/memberships", Ct);

        Assert.Empty(memberships!["clubs"]!.AsArray());
        Assert.False((bool)memberships["premium"]!);
    }

    [Fact]
    public void A_slow_acknowledgement_answers_202_with_where_to_look()
    {
        // Provoking this end to end needs a stalled worker; the mapping is pure, so test it directly.
        var result = PurchaseOrderEndpoints.Respond(SubmissionOutcome.Pending, new PurchaseOrderId("3344656"), stored: null);

        Assert.Equal($"{Orders}/3344656", Assert.IsType<Accepted>(result.Result).Location);
    }

    [Fact]
    public void An_unconfirmed_submission_answers_503_so_the_client_resubmits()
    {
        var result = PurchaseOrderEndpoints.Respond(SubmissionOutcome.Unconfirmed, new PurchaseOrderId("3344656"), stored: null);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task The_api_document_lists_every_submission_outcome()
    {
        var document = await _http.GetFromJsonAsync<JsonObject>("/openapi/v1.json", Ct);

        var responses = document!["paths"]![Orders]!["post"]!["responses"]!.AsObject();
        Assert.Equal(["200", "201", "202", "400", "409", "429", "503"], responses.Select(response => response.Key).Order());
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_checks_answer_healthy(string path) =>
        Assert.Equal("Healthy", await _http.GetStringAsync(path, Ct));

    internal static JsonObject AnOrder(string? customerId = null, string membership = "bookClub") => JsonNode.Parse($$"""
        {
          "id": "{{NewId()}}",
          "customerId": "{{customerId ?? NewId()}}",
          "total": 48.50,
          "items": [
            { "type": "video", "title": "Comprehensive First Aid Training" },
            { "type": "book", "title": "The Girl on the train" },
            { "type": "membership", "membership": "{{membership}}" }
          ]
        }
        """)!.AsObject();

    private static string NewId() => Guid.NewGuid().ToString("N");

    private static async Task<JsonObject> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return (await response.Content.ReadFromJsonAsync<JsonObject>(Ct))!;
    }
}
