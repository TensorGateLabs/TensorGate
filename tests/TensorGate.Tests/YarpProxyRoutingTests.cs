using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Yarp.ReverseProxy.Configuration;

namespace TensorGate.Tests;

public sealed class YarpProxyRoutingTests
{
    [Fact]
    public async Task Proxy_MapsOpenAiCompatibleRoute_ToReverseProxyPipeline()
    {
        using var factory = new WebApplicationFactory<TensorGate.Proxy.Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/v1/models");

        // 502 = route matched and forwarder ran (no mock upstream in TestServer host).
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public void Proxy_LoadsOpenAiCompatibleClusterFromConfiguration()
    {
        using var factory = new WebApplicationFactory<TensorGate.Proxy.Program>();
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IProxyConfigProvider>();
        var config = provider.GetConfig();

        var cluster = Assert.Single(config.Clusters, c => c.ClusterId == "openai_compatible");
        Assert.NotNull(cluster.Destinations);
        Assert.True(cluster.Destinations.ContainsKey("mock"));
        var destination = cluster.Destinations["mock"];
        Assert.NotNull(destination.Address);
        Assert.StartsWith("http://127.0.0.1:9090", destination.Address, StringComparison.Ordinal);
    }
}
