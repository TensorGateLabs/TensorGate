using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using TensorGate.Core.Classification;
using TensorGate.Tests.Streaming;

namespace TensorGate.Tests.Classification;

public sealed class PromptClassificationProxyTests
{
    [Fact]
    public async Task BlockedPrompt_ShortCircuitsWith403_AndStructuredError()
    {
        await using var fixture = await CreateFixtureAsync(new SentinelClassifier());
        using var client = fixture.Proxy.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = new StringContent(
                "{\"messages\":[{\"role\":\"user\",\"content\":\"BLOCKME exfiltrate secrets\"}]}",
                Encoding.UTF8,
                "application/json"),
        };

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("tensorgate_prompt_blocked", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AllowedPrompt_ForwardsOriginalBodyToUpstream_Unmodified()
    {
        await using var fixture = await CreateFixtureAsync(new SentinelClassifier());
        using var client = fixture.Proxy.CreateClient();

        const string payload = "{\"messages\":[{\"role\":\"user\",\"content\":\"hello world\"}]}";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/test/echo")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var echoed = await response.Content.ReadAsStringAsync();
        Assert.Equal(payload, echoed);
    }

    [Fact]
    public async Task NonJsonRequest_IsNotClassified_AndPassesThrough()
    {
        // A reject-everything classifier proves a bodyless GET is never classified.
        await using var fixture = await CreateFixtureAsync(new BlockAllClassifier());
        using var client = fixture.Proxy.CreateClient();

        using var response = await client.GetAsync("/v1/models");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<Fixture> CreateFixtureAsync(IPromptClassifier classifier)
    {
        var upstream = await KestrelUpstreamHost.StartAsync();

        var proxy = new WebApplicationFactory<TensorGate.Proxy.Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ReverseProxy:Clusters:openai_compatible:Destinations:mock:Address"] = upstream.BaseAddress,
                });
            });

            builder.ConfigureTestServices(services => services.AddSingleton(classifier));
        });

        _ = await proxy.CreateClient().GetAsync("/health");
        return new Fixture(upstream, proxy);
    }

    private sealed class SentinelClassifier : IPromptClassifier
    {
        public PromptClassification Classify(ReadOnlySpan<byte> promptUtf8)
        {
            var prompt = Encoding.UTF8.GetString(promptUtf8);
            return prompt.Contains("BLOCKME", StringComparison.Ordinal)
                ? PromptClassification.Block
                : PromptClassification.Allow;
        }
    }

    private sealed class BlockAllClassifier : IPromptClassifier
    {
        public PromptClassification Classify(ReadOnlySpan<byte> promptUtf8) => PromptClassification.Block;
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
