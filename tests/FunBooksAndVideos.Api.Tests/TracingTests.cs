using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using FunBooksAndVideos.Api.Observability;

namespace FunBooksAndVideos.Api.Tests;

/// <summary>The trace context survives the trip through Temporal: the rules run on a worker, inside the caller's trace.</summary>
public sealed class TracingTests(AppFixture fixture)
{
    [Fact]
    public async Task One_trace_runs_from_the_request_through_the_workflow_to_each_rule()
    {
        var traceId = ActivityTraceId.CreateRandom();
        var rulesInTrace = new ConcurrentQueue<string>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Telemetry.Source.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = span =>
            {
                if (span.TraceId == traceId) rulesInTrace.Enqueue(span.DisplayName);
            },
        };
        ActivitySource.AddActivityListener(listener);

        // The caller's own trace, as a mobile app or gateway would send it.
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/purchase-orders")
        {
            Content = JsonContent.Create(PurchaseOrderApiTests.AnOrder()),
        };
        request.Headers.Add("traceparent", $"00-{traceId}-{ActivitySpanId.CreateRandom()}-01");
        var response = await fixture.App.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(["BR1 membership activation", "BR2 shipping slip"], rulesInTrace.Order());
    }
}
