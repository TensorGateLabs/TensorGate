namespace TensorGate.Core.Streaming;

public static class SseMediaTypes
{
    public const string EventStream = "text/event-stream";

    public static bool IsEventStream(string? contentType) =>
        contentType is not null &&
        contentType.StartsWith(EventStream, StringComparison.OrdinalIgnoreCase);
}
