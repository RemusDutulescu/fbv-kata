using FunBooksAndVideos.Application.Processing;
using FunBooksAndVideos.Application.Submission;
using FunBooksAndVideos.Domain;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.Infrastructure.Temporal;
using Microsoft.Extensions.Options;
using Temporalio.Client;
using Temporalio.Testing;
using static FunBooksAndVideos.IntegrationTests.WorkflowTesting;

namespace FunBooksAndVideos.IntegrationTests;

public sealed class PurchaseOrderWorkflowTests(AppFixture fixture)
{
    [Fact]
    public async Task The_caller_hears_back_after_immediate_commands_and_before_deferred_ones()
    {
        var activities = new ScriptedActivities(Activation(), Slip());
        activities.ActivationGate.SetResult();

        await RunWorkerAsync(fixture.Temporal, activities, async taskQueue =>
        {
            var handle = await SubmitAsync(fixture.Temporal, OrderSubmission.From(NewOrder()), taskQueue);

            // Acknowledged while the slip is still held at its gate: the response never waits for deferred work.
            Assert.Equal(["AcceptOrder", "ActivateMembership"], activities.Calls);

            activities.SlipGate.SetResult();
            await handle.GetResultAsync();
            Assert.Equal(["AcceptOrder", "ActivateMembership", "GenerateShippingSlip", "CompleteOrder"], activities.Calls);
        });
    }

    [Fact]
    public async Task A_duplicate_submission_joins_the_running_workflow()
    {
        var activities = new ScriptedActivities(Activation(), Slip());
        activities.ActivationGate.SetResult();

        await RunWorkerAsync(fixture.Temporal, activities, async taskQueue =>
        {
            var submission = OrderSubmission.From(NewOrder());

            // The slip gate keeps the workflow running, so both calls meet the same execution.
            var handles = await Task.WhenAll(
                SubmitAsync(fixture.Temporal, submission, taskQueue),
                SubmitAsync(fixture.Temporal, submission, taskQueue));

            Assert.Single(activities.Calls, call => call == "AcceptOrder");
            Assert.Equal(handles[0].ResultRunId, handles[1].ResultRunId);

            activities.SlipGate.SetResult();
            await handles[0].GetResultAsync();
        });
    }

    [Theory]
    [InlineData(true, SubmissionOutcome.Pending)]
    [InlineData(false, SubmissionOutcome.Unconfirmed)]
    public async Task A_slow_acknowledgement_is_pending_only_once_the_order_is_stored(bool stored, SubmissionOutcome expected)
    {
        var activities = new ScriptedActivities(Activation());

        await RunWorkerAsync(fixture.Temporal, activities, async taskQueue =>
        {
            var order = NewOrder();
            if (stored) await StoreAsync(order);

            // The activation gate stays shut, so BR1's work can't finish within the second the caller waits.
            Assert.Equal(expected, await SubmitThroughOrderSubmitterAsync(order, taskQueue, TimeSpan.FromSeconds(1)));

            activities.ActivationGate.SetResult();
            await fixture.Temporal.GetWorkflowHandle(PurchaseOrderWorkflow.IdFor(order.Id)).GetResultAsync();
        });
    }

    [Theory]
    [InlineData(true, SubmissionOutcome.Replayed)]
    [InlineData(false, SubmissionOutcome.Conflict)]
    public async Task Once_the_workflow_has_finished_the_stored_order_decides(bool stored, SubmissionOutcome expected)
    {
        var activities = new ScriptedActivities(Activation());
        activities.ActivationGate.SetResult();

        await RunWorkerAsync(fixture.Temporal, activities, async taskQueue =>
        {
            var order = NewOrder();
            await (await SubmitAsync(fixture.Temporal, OrderSubmission.From(order), taskQueue)).GetResultAsync();

            // Temporal answers with the Submit update's original result; were it to refuse instead, both would be a
            // Conflict. Stored but still processing: a retry that read the order just before the original completed it.
            // Not stored: the database was wiped, so the id is spent.
            if (stored) await StoreAsync(order);

            Assert.Equal(expected, await SubmitThroughOrderSubmitterAsync(order, taskQueue, TimeSpan.FromSeconds(10)));
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_workflow_that_ended_without_acknowledging_is_never_run_again(bool stored)
    {
        var activities = new ScriptedActivities(Activation());

        await RunWorkerAsync(fixture.Temporal, activities, async taskQueue =>
        {
            var order = NewOrder();
            if (stored) await StoreAsync(order);
            var handle = await fixture.Temporal.StartWorkflowAsync(
                (PurchaseOrderWorkflow workflow) => workflow.RunAsync(OrderSubmission.From(order)),
                PurchaseOrderWorkflow.StartOptions(order.Id, taskQueue));
            await handle.TerminateAsync();

            // Run again, it would stall at the shut activation gate and time out instead. Stored or not, its memberships
            // were never activated, so the id is spent.
            Assert.Equal(SubmissionOutcome.Conflict, await SubmitThroughOrderSubmitterAsync(order, taskQueue, TimeSpan.FromSeconds(2)));
        });
    }

    [Fact]
    public async Task A_temporal_never_reached_leaves_a_new_order_unconfirmed()
    {
        // Lazy, as the API's client is: it connects on first use, and nothing listens on port 1.
        var unreachable = TemporalClient.CreateLazy(new("localhost:1") { DataConverter = TemporalServiceCollectionExtensions.TemporalDataConverter });

        Assert.Equal(
            SubmissionOutcome.Unconfirmed,
            await SubmitThroughOrderSubmitterAsync(NewOrder(), "no-workers", TimeSpan.FromSeconds(10), unreachable));
    }

    [Fact]
    public async Task A_temporal_lost_after_connecting_leaves_a_stored_order_pending()
    {
        // The client stops retrying after a second, not ten, so the test is quick; the exception is the same.
        var lost = await WorkflowEnvironment.StartLocalAsync(new()
        {
            DataConverter = TemporalServiceCollectionExtensions.TemporalDataConverter,
            RpcRetry = new() { MaxElapsedTime = TimeSpan.FromSeconds(1) },
        });
        await lost.ShutdownAsync();
        var order = NewOrder();
        await StoreAsync(order);

        Assert.Equal(
            SubmissionOutcome.Pending,
            await SubmitThroughOrderSubmitterAsync(order, "no-workers", TimeSpan.FromSeconds(10), lost.Client));
    }

    /// <summary>Stores the order as AcceptOrder would. The scripted activities store nothing themselves.</summary>
    private async Task StoreAsync(PurchaseOrder order)
    {
        await using var db = fixture.CreateDbContext();
        db.PurchaseOrders.Add(PurchaseOrderRecord.Accept(order, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<SubmissionOutcome> SubmitThroughOrderSubmitterAsync(
        PurchaseOrder order, string taskQueue, TimeSpan timeout, ITemporalClient? client = null)
    {
        await using var db = fixture.CreateDbContext();
        var temporal = Options.Create(new TemporalOptions
        {
            Address = fixture.Temporal.Connection.Options.TargetHost!,
            Namespace = fixture.Temporal.Options.Namespace,
            TaskQueue = taskQueue,
        });
        var submission = Options.Create(new SubmissionOptions { AcknowledgementTimeout = timeout });
        return await new OrderSubmitter(client ?? fixture.Temporal, db, temporal, submission).SubmitAsync(order, TestContext.Current.CancellationToken);
    }
}
