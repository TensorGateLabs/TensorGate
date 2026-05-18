using TensorGate.MockUpstream;

namespace TensorGate.Tests.Streaming;

internal sealed class KestrelUpstreamHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private KestrelUpstreamHost(WebApplication app, string baseAddress)
    {
        _app = app;
        BaseAddress = baseAddress.EndsWith('/') ? baseAddress : baseAddress + "/";
    }

    public string BaseAddress { get; }

    public static async Task<KestrelUpstreamHost> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        MockUpstreamHost.MapEndpoints(app);
        await app.StartAsync().ConfigureAwait(false);

        var address = app.Urls.First();
        return new KestrelUpstreamHost(app, address);
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync().ConfigureAwait(false);
}
