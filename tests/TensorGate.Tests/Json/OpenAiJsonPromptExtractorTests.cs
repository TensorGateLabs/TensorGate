using System.Buffers;
using System.Text;
using TensorGate.Core.Json;

namespace TensorGate.Tests.Json;

public sealed class OpenAiJsonPromptExtractorTests
{
    [Fact]
    public void TryExtractPrompt_ChatCompletionsSchema_ReturnsUserContent()
    {
        const string json = """
            {
              "model": "gpt-4",
              "messages": [
                { "role": "system", "content": "You are helpful." },
                { "role": "user", "content": "Explain zero allocation." }
              ]
            }
            """;

        var sink = new ArrayBufferWriter<byte>();
        var ok = OpenAiJsonPromptExtractor.TryExtractPrompt(Encoding.UTF8.GetBytes(json), sink);
        Assert.True(ok);
        var text = Encoding.UTF8.GetString(sink.WrittenSpan);
        Assert.Contains("You are helpful.", text, StringComparison.Ordinal);
        Assert.Contains("Explain zero allocation.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TryExtractPrompt_LegacyPromptField_Works()
    {
        const string json = """{ "model": "davinci", "prompt": "Summarize this." }""";
        var sink = new ArrayBufferWriter<byte>();
        Assert.True(OpenAiJsonPromptExtractor.TryExtractPrompt(Encoding.UTF8.GetBytes(json), sink));
        Assert.Equal("Summarize this.", Encoding.UTF8.GetString(sink.WrittenSpan));
    }

    [Fact]
    public void ChunkedInput_ReassemblesPromptAcrossBuffers()
    {
        var part1 = """{ "messages": [ """u8;
        var part2 = """{ "role": "user", "content": "chunked-stream" } ] }"""u8;
        var state = new OpenAiJsonPromptStreamState();
        state.Reset();
        var passthrough = new ArrayBufferWriter<byte>();
        var sink = new ArrayBufferWriter<byte>();

        OpenAiJsonPromptExtractor.ProcessChunk(part1, ref state, passthrough, sink);
        Assert.True(OpenAiJsonPromptExtractor.Complete(part2, ref state, passthrough, sink));

        Assert.Equal("chunked-stream", Encoding.UTF8.GetString(sink.WrittenSpan));
        Assert.True(passthrough.WrittenCount > 0);
    }

    [Fact]
    public void EmptyPrompt_AllowsEmptyCapture()
    {
        const string json = """{ "messages": [ { "role": "user", "content": "" } ] }""";
        var sink = new ArrayBufferWriter<byte>();
        Assert.True(OpenAiJsonPromptExtractor.TryExtractPrompt(Encoding.UTF8.GetBytes(json), sink));
        Assert.Equal(string.Empty, Encoding.UTF8.GetString(sink.WrittenSpan));
    }

    [Fact]
    public void LargePayload_OneMegabyte_ExtractsWithoutThrowing()
    {
        var content = new string('x', 32_000);
        var json = $$"""
            { "messages": [ { "role": "user", "content": "{{content}}" } ] }
            """;
        var bytes = Encoding.UTF8.GetBytes(json);
        var sink = new ArrayBufferWriter<byte>(bytes.Length);
        Assert.True(OpenAiJsonPromptExtractor.TryExtractPrompt(bytes, sink));
        Assert.Contains("xxxx", Encoding.UTF8.GetString(sink.WrittenSpan), StringComparison.Ordinal);
    }

    [Fact]
    public void HotPath_SingleChunkExtract_DoesNotAllocateOnSteadyState()
    {
        const string json = """{ "messages": [ { "role": "user", "content": "steady" } ] }""";
        var bytes = Encoding.UTF8.GetBytes(json);
        var sink = new ArrayBufferWriter<byte>(64);
        var state = new OpenAiJsonPromptStreamState();
        state.Reset();

        OpenAiJsonPromptExtractor.TryExtractPrompt(bytes, sink);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1_000; i++)
        {
            sink.Clear();
            state.Reset();
            OpenAiJsonPromptExtractor.TryExtractPrompt(bytes, sink);
        }

        long after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(before, after);
    }
}
