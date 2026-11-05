using FunBooksAndVideos.Application.Processing;
using FunBooksAndVideos.Application.Submission;
using FunBooksAndVideos.Domain;
using FunBooksAndVideos.Domain.Fulfilment;
using Temporalio.Client;
using Temporalio.Worker;

namespace FunBooksAndVideos.IntegrationTests;

/// <summary>Shared helpers for driving <see cref="PurchaseOrderWorkflow"/> against scripted activities.</summary>
internal static class WorkflowTesting
{
    public static readonly CustomerId Customer = new("4567890");

    public static ActivateMembership Activation() => new(Customer, MembershipType.BookClub, Timing.Immediate);

    public static GenerateShippingSlip Slip() =>
        new(new PurchaseOrderId("3344656"), Customer, [new BookLine("The Girl on the train")], Timing.Deferred);

    public static PurchaseOrder NewOrder() => new(
        new PurchaseOrderId(Guid.NewGuid().ToString("N")),
        Customer,
        new Money(48.50m),
        [new BookLine("The Girl on the train"), new MembershipLine(MembershipType.BookClub)]);

    /// <summary>Runs a worker for the scripted activities on its own task queue for the duration of the test body.</summary>
    public static async Task RunWorkerAsync(ITemporalClient client, ScriptedActivities activities, Func<string, Task> test)
    {
        var taskQueue = $"workflow-tests-{Guid.NewGuid():N}";
        using var worker = new TemporalWorker(
            client,
            new TemporalWorkerOptions(taskQueue).AddWorkflow<PurchaseOrderWorkflow>().AddAllActivities(activities));

        await worker.ExecuteAsync(() => test(taskQueue), TestContext.Current.CancellationToken);
    }

    /// <summary>Starts the workflow and waits for its Submit update in one call, with the options <see cref="OrderSubmitter"/> uses.</summary>
    public static async Task<WorkflowHandle<PurchaseOrderWorkflow>> SubmitAsync(ITemporalClient client, OrderSubmission submission, string taskQueue)
    {
        var start = WithStartWorkflowOperation.Create(
            (PurchaseOrderWorkflow workflow) => workflow.RunAsync(submission),
            PurchaseOrderWorkflow.StartOptions(submission.Id, taskQueue));

        await client.ExecuteUpdateWithStartWorkflowAsync(
            (PurchaseOrderWorkflow workflow) => workflow.SubmitAsync(),
            new WorkflowUpdateWithStartOptions($"submit-{submission.Id}", start));

        return await start.GetHandleAsync();
    }
}
