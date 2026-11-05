using FunBooksAndVideos.Infrastructure;
using FunBooksAndVideos.Infrastructure.Temporal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FunBooksAndVideos.IntegrationTests;

public sealed class RequiredOptionsTests
{
    [Fact]
    public void A_missing_section_stops_the_app()
    {
        var error = Assert.Throws<InvalidOperationException>(() => AddTemporalOptions(new()));
        Assert.Equal("Section 'Temporal' not found in configuration.", error.Message);
    }

    [Fact]
    public void A_missing_value_stops_the_app()
    {
        var error = Assert.Throws<InvalidOperationException>(() => AddTemporalOptions(new()
        {
            ["Temporal:Address"] = "localhost:7233",
            ["Temporal:TaskQueue"] = "purchase-orders",
        }));
        Assert.Equal("Section 'Temporal' is invalid. The Namespace field is required.", error.Message);
    }

    private static TemporalOptions AddTemporalOptions(Dictionary<string, string?> settings) =>
        new ServiceCollection().AddRequiredOptions<TemporalOptions>(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), TemporalOptions.Section);
}
