using FunBooksAndVideos.Api.Processing;
using Microsoft.Extensions.Options;
using static FunBooksAndVideos.Api.Tests.WorkflowTesting;

namespace FunBooksAndVideos.Api.Tests;

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

    [Fact]
    public async Task A_slow_acknowledgement_is_reported_as_pending()
    {
        var activities = new ScriptedActivities(Activation());

        await RunWorkerAsync(fixture.Temporal, activities, async taskQueue =>
        {
            await using var db = fixture.CreateDbContext();
            var options = Options.Create(new TemporalOptions { TaskQueue = taskQueue, AcknowledgementTimeout = TimeSpan.FromSeconds(1) });
            var order = NewOrder();

            // The activation gate stays shut, so BR1's work can't finish within the second the caller waits.
            var outcome = await new OrderSubmitter(fixture.Temporal, db, options).SubmitAsync(order, TestContext.Current.CancellationToken);

            Assert.IsType<SubmissionOutcome.Pending>(outcome);

            activities.ActivationGate.SetResult();
            await fixture.Temporal.GetWorkflowHandle(PurchaseOrderWorkflow.IdFor(order.Id)).GetResultAsync();
        });
    }
}
