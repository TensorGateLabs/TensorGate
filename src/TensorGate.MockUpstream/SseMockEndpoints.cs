using System.Text;
using TensorGate.Core.Streaming;

namespace TensorGate.MockUpstream;

internal static class SseMockEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/test/sse", StreamSseChunks);
        app.MapPost("/v1/chat/completions", HandleChatCompletions);
    }

    private static async Task HandleChatCompletions(HttpRequest request, HttpResponse response, CancellationToken cancellationToken)
    {
        var streamRequested = request.Query.ContainsKey("stream")
            || await RequestBodyRequestsStreamAsync(request, cancellationToken).ConfigureAwait(false);

        if (!streamRequested)
        {
            await WriteJsonCompletionAsync(response, cancellationToken).ConfigureAwait(false);
            return;
        }

        await StreamSseChunks(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> RequestBodyRequestsStreamAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (!request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            return false;
        }

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return body.Contains("\"stream\":true", StringComparison.OrdinalIgnoreCase)
            || body.Contains("\"stream\": true", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteJsonCompletionAsync(HttpResponse response, CancellationToken cancellationToken)
    {
        response.ContentType = "application/json";
        await response.WriteAsync("{\"object\":\"chat.completion\",\"choices\":[]}", cancellationToken).ConfigureAwait(false);
    }

    private static async Task StreamSseChunks(HttpResponse response, CancellationToken cancellationToken)
    {
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = SseMediaTypes.EventStream;
        response.Headers.CacheControl = "no-cache";
        response.Headers.Connection = "keep-alive";

        for (var i = 0; i < 5; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var payload = $"data: {{\"index\":{i},\"token\":\"chunk-{i}\"}}\n\n";
            await response.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            await Task.Delay(40, cancellationToken).ConfigureAwait(false);
        }

        await response.WriteAsync("data: [DONE]\n\n", cancellationToken).ConfigureAwait(false);
        await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
