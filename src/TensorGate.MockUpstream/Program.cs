namespace TensorGate.MockUpstream;

public static class MockUpstreamHost
{
    public static void Run(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        MapEndpoints(app);
        app.Run();
    }

    public static void MapEndpoints(WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", role = "mock-upstream" }));

        SseMockEndpoints.Map(app);

        app.Map("{**path}", (HttpRequest request, string path) => Results.Ok(new
        {
            role = "mock-upstream",
            path = "/" + path,
            method = request.Method,
            provider = "openai-compatible-mock",
        }));
    }
}

public partial class Program
{
    public static void Main(string[] args) => MockUpstreamHost.Run(args);
}
