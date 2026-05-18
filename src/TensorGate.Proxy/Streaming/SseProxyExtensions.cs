using System.Net.Http.Headers;
using TensorGate.Core.Streaming;
using Yarp.ReverseProxy.Transforms;

namespace TensorGate.Proxy.Streaming;

public static class SseProxyExtensions
{
    public const string SsePassthroughItemKey = "TensorGate.SsePassthrough";

    public static IReverseProxyBuilder AddSsePassthroughTransforms(this IReverseProxyBuilder builder)
    {
        return builder.AddTransforms(static transformContext =>
        {
            transformContext.AddResponseTransform(async context =>
            {
                var headers = context.ProxyResponse?.Content.Headers;
                var contentType = headers?.ContentType?.MediaType;
                if (!SseMediaTypes.IsEventStream(contentType))
                {
                    return;
                }

                var http = context.HttpContext;
                http.Items[SsePassthroughItemKey] = true;

                var response = http.Response;
                response.Headers.CacheControl = "no-cache";
                response.Headers.Connection = "keep-alive";
                response.Headers.TryAdd("X-Accel-Buffering", "no");

                if (headers?.ContentType is MediaTypeHeaderValue upstreamType)
                {
                    response.ContentType = upstreamType.ToString();
                }

                await Task.CompletedTask;
            });
        });
    }

    public static WebApplicationBuilder ConfigureTensorGateKestrel(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            // Allow long-lived SSE streams without minimum bytes/sec aborts.
            options.Limits.MinResponseDataRate = null;
        });

        return builder;
    }
}
