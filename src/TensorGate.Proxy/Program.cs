using TensorGate.Core;
using TensorGate.Proxy.Streaming;

namespace TensorGate.Proxy;

public static class ProxyHost
{
    public static void Run(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        Configure(builder);
        var app = builder.Build();
        MapEndpoints(app);
        app.Run();
    }

    internal static void Configure(WebApplicationBuilder builder)
    {
        builder.ConfigureTensorGateKestrel();

        builder.Services.AddHealthChecks();
        builder.Services
            .AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
            .AddSsePassthroughTransforms();
    }

    internal static void MapEndpoints(WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapGet("/", () => Results.Ok(new
        {
            service = TensorGateMetadata.ProxyServiceName,
            status = "running",
        }));
        app.MapReverseProxy();
    }
}

public partial class Program
{
    public static void Main(string[] args) => ProxyHost.Run(args);
}
