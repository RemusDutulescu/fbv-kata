using System.Runtime.CompilerServices;
using FunBooksAndVideos.Application.Processing;
using FunBooksAndVideos.Infrastructure.Temporal;
using Temporalio.Common;
using Temporalio.Worker;
using static FunBooksAndVideos.IntegrationTests.WorkflowTesting;

namespace FunBooksAndVideos.IntegrationTests;

public sealed class ReplayTests(AppFixture fixture)
{
    private const string HistoryFile = "purchase-order-workflow.json";

    /// <summary>
    /// The determinism guard. The committed history was recorded by an earlier build of the workflow; if a change
    /// would make an order already in flight replay differently, this fails in CI instead of in production.
    /// </summary>
    [Fact]
    public async Task The_workflow_still_replays_its_recorded_history()
    {
        var json = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Histories", HistoryFile),
            TestContext.Current.CancellationToken);

        var replayer = new WorkflowReplayer(
            new WorkflowReplayerOptions { DataConverter = TemporalServiceCollectionExtensions.TemporalDataConverter }
                .AddWorkflow<PurchaseOrderWorkflow>());

        await replayer.ReplayWorkflowAsync(
            WorkflowHistory.FromJson("purchase-order-workflow", json),
            cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>Re-records the reference history after a deliberate, versioned change. Explicit: it only runs on demand.</summary>
    [Fact(Explicit = true)]
    public async Task Record_the_reference_history()
    {
        var activities = new ScriptedActivities(Activation(), Slip());
        activities.ActivationGate.SetResult();
        activities.SlipGate.SetResult();

        await RunWorkerAsync(fixture.Temporal, activities, async taskQueue =>
        {
            var handle = await SubmitAsync(fixture.Temporal, OrderSubmission.From(NewOrder()), taskQueue);
            await handle.GetResultAsync();

            var history = await handle.FetchHistoryAsync();
            await File.WriteAllTextAsync(SourcePath(), history.ToJson(), TestContext.Current.CancellationToken);
        });
    }

    private static string SourcePath([CallerFilePath] string thisFile = "") =>
        Path.Combine(Path.GetDirectoryName(thisFile)!, "Histories", HistoryFile);
}
