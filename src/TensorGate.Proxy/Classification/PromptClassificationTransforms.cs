using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using TensorGate.Core.Classification;
using TensorGate.Core.Json;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace TensorGate.Proxy.Classification;

/// <summary>
/// Registers a YARP request transform that synchronously inspects outbound
/// OpenAI-compatible prompts and either forwards them upstream or blocks them.
/// </summary>
/// <remarks>
/// The transform only attaches to routes opted in via the
/// <see cref="ClassifyMetadataKey"/> metadata flag, so non-matching routes keep
/// their zero-overhead pass-through behaviour. The request body is buffered once,
/// the prompt is extracted via the <see cref="OpenAiJsonPromptExtractor"/> finite
/// state machine, and the decision is taken before the request leaves the proxy.
/// </remarks>
public static class PromptClassificationTransforms
{
    /// <summary>Route metadata key that opts a route into prompt classification.</summary>
    public const string ClassifyMetadataKey = "TensorGate.Classify";

    private static readonly byte[] BlockedResponseBody = Encoding.UTF8.GetBytes(
        "{\"error\":{\"type\":\"prompt_blocked\"," +
        "\"code\":\"tensorgate_prompt_blocked\"," +
        "\"message\":\"Request blocked by TensorGate prompt classification.\"}}");

    /// <summary>
    /// Adds the prompt classification request transform to opted-in routes.
    /// </summary>
    public static IReverseProxyBuilder AddPromptClassificationTransforms(this IReverseProxyBuilder builder)
    {
        return builder.AddTransforms(static context =>
        {
            if (!ClassificationEnabled(context.Route))
            {
                return;
            }

            context.AddRequestTransform(static transformContext => InterceptAsync(transformContext));
        });
    }

    internal static bool ClassificationEnabled(RouteConfig route) =>
        route.Metadata is { } metadata
        && metadata.TryGetValue(ClassifyMetadataKey, out var value)
        && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    private static async ValueTask InterceptAsync(RequestTransformContext transformContext)
    {
        var http = transformContext.HttpContext;

        if (!CarriesJsonBody(http))
        {
            // No inspectable prompt body (e.g. GET /v1/models): forward untouched.
            return;
        }

        var body = await BufferRequestBodyAsync(http.Request, http.RequestAborted).ConfigureAwait(false);

        var promptSink = new ArrayBufferWriter<byte>();
        OpenAiJsonPromptExtractor.TryExtractPrompt(body, promptSink);

        var classifier = http.RequestServices.GetRequiredService<IPromptClassifier>();
        if (classifier.Classify(promptSink.WrittenSpan) == PromptClassification.Block)
        {
            await WriteBlockedResponseAsync(http).ConfigureAwait(false);
            return;
        }

        // Allowed: the body stream was consumed during inspection, so rewind it with the
        // buffered bytes and let YARP forward the original payload. Replacing the outgoing
        // ProxyRequest.Content is rejected by YARP ("Replacing the YARP outgoing request
        // HttpContent is not supported") — the request body stream is the supported seam.
        http.Request.Body = new MemoryStream(body);
    }

    private static bool CarriesJsonBody(HttpContext http)
    {
        var canHaveBody = http.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody ?? false;
        if (!canHaveBody)
        {
            return false;
        }

        var contentType = http.Request.ContentType;
        return !string.IsNullOrEmpty(contentType)
            && contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static async ValueTask<byte[]> BufferRequestBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await request.Body.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    private static async ValueTask WriteBlockedResponseAsync(HttpContext http)
    {
        // Setting a non-200 status and writing the body short-circuits YARP forwarding.
        var response = http.Response;
        response.StatusCode = StatusCodes.Status403Forbidden;
        response.ContentType = "application/json";
        response.ContentLength = BlockedResponseBody.Length;
        await response.Body.WriteAsync(BlockedResponseBody).ConfigureAwait(false);
    }
}
