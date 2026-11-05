using FunBooksAndVideos.Domain;
using FunBooksAndVideos.Domain.Fulfilment;
using FunBooksAndVideos.Domain.Rules;

namespace FunBooksAndVideos.Api.Observability;

/// <summary>
/// Decorator: each evaluation of the wrapped rule becomes a span named after the rule, tagged with the commands it
/// produced. Rules and the processor know nothing about tracing, and a rule registered through AddRule gets it for free.
/// </summary>
public sealed class TracedRule(IPurchaseOrderRule inner) : IPurchaseOrderRule
{
    public string Name => inner.Name;

    public IEnumerable<FulfilmentCommand> Evaluate(PurchaseOrder order)
    {
        using var span = Telemetry.Source.StartActivity(inner.Name);

        // Materialised inside the span, so the span covers the evaluation rather than ending before it starts.
        var commands = inner.Evaluate(order).ToArray();
        span?.SetTag("rule.commands", string.Join(", ", commands.Select(command => command.GetType().Name)));
        return commands;
    }
}
