using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using TensorGate.Core.Streaming;
using Yarp.ReverseProxy.Configuration;

namespace TensorGate.Tests.Streaming;

public sealed class SseProxyStreamingTests
{

    [Fact]
    public async Task Proxy_ForwardsSseChunks_WithoutBufferingEntireResponse()
    {
        await using var fixture = await CreateFixtureAsync();
        using var client = fixture.Proxy.CreateClient();

        using var response = await client.GetAsync(
            "/v1/test/sse",
            HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();
        Assert.True(SseMediaTypes.IsEventStream(response.Content.Headers.ContentType?.MediaType));

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var firstAt = await ReadFirstDataLineAsync(reader);
        var secondAt = await ReadNextDataLineAsync(reader);

        var gapMs = (secondAt - firstAt).TotalMilliseconds;
        Assert.True(gapMs >= 25, $"Expected chunked delivery, gap was {gapMs:F1}ms");
    }

    [Fact]
    public async Task Proxy_ChatCompletionsStreamRoute_ForwardsEventStream()
    {
        await using var fixture = await CreateFixtureAsync();
        using var client = fixture.Proxy.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = new StringContent("{\"stream\":true}", Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(SseMediaTypes.EventStream));

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        Assert.True(SseMediaTypes.IsEventStream(response.Content.Headers.ContentType?.MediaType));
    }

    [Fact]
    public void Proxy_ConfiguresAllowResponseBufferingFalse()
    {
        using var factory = new WebApplicationFactory<TensorGate.Proxy.Program>();
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IProxyConfigProvider>();
        var cluster = Assert.Single(provider.GetConfig().Clusters, c => c.ClusterId == "openai_compatible");

        Assert.NotNull(cluster.HttpRequest);
        Assert.False(cluster.HttpRequest.AllowResponseBuffering);
    }

    private static async Task<Fixture> CreateFixtureAsync()
    {
        var upstreamHost = await KestrelUpstreamHost.StartAsync();

        var proxy = new WebApplicationFactory<TensorGate.Proxy.Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ReverseProxy:Clusters:openai_compatible:Destinations:mock:Address"] = upstreamHost.BaseAddress,
                });
            });
        });

        _ = await proxy.CreateClient().GetAsync("/health");
        return new Fixture(upstreamHost, proxy);
    }

    private static async Task<DateTimeOffset> ReadFirstDataLineAsync(StreamReader reader)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line is null)
            {
                throw new InvalidOperationException("Stream ended before first SSE data line.");
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                return DateTimeOffset.UtcNow;
            }
        }
    }

    private static async Task<DateTimeOffset> ReadNextDataLineAsync(StreamReader reader)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(5))
        {
            var line = await reader.ReadLineAsync();
            if (line is not null && line.StartsWith("data:", StringComparison.Ordinal))
            {
                return DateTimeOffset.UtcNow;
            }
        }

        throw new InvalidOperationException("Timed out waiting for second SSE data line.");
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public Fixture(KestrelUpstreamHost upstream, WebApplicationFactory<TensorGate.Proxy.Program> proxy)
        {
            Upstream = upstream;
            Proxy = proxy;
        }

        public KestrelUpstreamHost Upstream { get; }
        public WebApplicationFactory<TensorGate.Proxy.Program> Proxy { get; }

        public async ValueTask DisposeAsync()
        {
            await Proxy.DisposeAsync();
            await Upstream.DisposeAsync();
        }
    }
}
