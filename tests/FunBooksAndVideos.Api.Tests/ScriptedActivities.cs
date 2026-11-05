using System.Collections.Concurrent;
using FunBooksAndVideos.Api.Processing;
using FunBooksAndVideos.Domain;
using FunBooksAndVideos.Domain.Fulfilment;
using Temporalio.Activities;

namespace FunBooksAndVideos.Api.Tests;

/// <summary>
/// Stands in for the real activities (same activity names) so the workflow's orchestration can be tested on its own.
/// Each fulfilment step waits at a gate the test opens, which makes the order of events observable.
/// </summary>
public sealed class ScriptedActivities(params FulfilmentCommand[] commands)
{
    private readonly ConcurrentQueue<string> _calls = new();

    public TaskCompletionSource ActivationGate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource SlipGate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IReadOnlyList<string> Calls => [.. _calls];

    [Activity]
    public Task<IReadOnlyList<FulfilmentCommand>> AcceptOrderAsync(OrderSubmission submission)
    {
        _calls.Enqueue("AcceptOrder");
        return Task.FromResult<IReadOnlyList<FulfilmentCommand>>(commands);
    }

    [Activity]
    public async Task ActivateMembershipAsync(ActivateMembership command)
    {
        await ActivationGate.Task.WaitAsync(ActivityExecutionContext.Current.CancellationToken);
        _calls.Enqueue("ActivateMembership");
    }

    [Activity]
    public async Task GenerateShippingSlipAsync(GenerateShippingSlip command)
    {
        await SlipGate.Task.WaitAsync(ActivityExecutionContext.Current.CancellationToken);
        _calls.Enqueue("GenerateShippingSlip");
    }

    [Activity]
    public Task CompleteOrderAsync(PurchaseOrderId id)
    {
        _calls.Enqueue("CompleteOrder");
        return Task.CompletedTask;
    }
}
