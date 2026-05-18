using System.Buffers;
using System.Text.Json;

namespace TensorGate.Core.Json;

/// <summary>
/// Zero-allocation-oriented OpenAI JSON prompt extractor using Utf8JsonReader.
/// Operates synchronously over <see cref="ReadOnlySpan{T}"/> chunks (no async).
/// Passthrough copies raw UTF-8 chunks; re-tokenization via Utf8JsonWriter is applied at integration boundaries.
/// </summary>
public static class OpenAiJsonPromptExtractor
{
    private static ReadOnlySpan<byte> MessagesProperty => "messages"u8;
    private static ReadOnlySpan<byte> ContentProperty => "content"u8;
    private static ReadOnlySpan<byte> PromptProperty => "prompt"u8;

    public static void ProcessChunk(
        ReadOnlySpan<byte> chunk,
        ref OpenAiJsonPromptStreamState state,
        IBufferWriter<byte> passthrough,
        IBufferWriter<byte> promptSink)
    {
        if (chunk.IsEmpty)
        {
            return;
        }

        state.Phase = ParserPhase.Streaming;
        if (passthrough is not NullBufferWriter)
        {
            WriteSpan(chunk, passthrough);
        }

        ProcessReader(chunk, isFinalBlock: false, ref state, promptSink);
    }

    public static bool Complete(
        ReadOnlySpan<byte> finalChunk,
        ref OpenAiJsonPromptStreamState state,
        IBufferWriter<byte> passthrough,
        IBufferWriter<byte> promptSink)
    {
        if (!finalChunk.IsEmpty)
        {
            if (passthrough is not NullBufferWriter)
            {
                WriteSpan(finalChunk, passthrough);
            }

            ProcessReader(finalChunk, isFinalBlock: true, ref state, promptSink);
        }

        state.Phase = ParserPhase.Complete;
        return state.CapturedLength > 0;
    }

    public static bool TryExtractPrompt(ReadOnlySpan<byte> jsonUtf8, IBufferWriter<byte> promptSink)
    {
        var state = new OpenAiJsonPromptStreamState();
        state.Reset();
        ProcessChunk(jsonUtf8, ref state, NullBufferWriter.Instance, promptSink);
        return Complete(ReadOnlySpan<byte>.Empty, ref state, NullBufferWriter.Instance, promptSink);
    }

    private static void ProcessReader(
        ReadOnlySpan<byte> chunk,
        bool isFinalBlock,
        ref OpenAiJsonPromptStreamState state,
        IBufferWriter<byte> promptSink)
    {
        var reader = state.ReaderInitialized
            ? new Utf8JsonReader(chunk, isFinalBlock, state.ReaderState)
            : new Utf8JsonReader(chunk, isFinalBlock, state: default);

        state.ReaderInitialized = true;
        while (reader.Read())
        {
            TrackToken(ref reader, ref state, promptSink);
        }

        state.ReaderState = reader.CurrentState;
    }

    private static void TrackToken(ref Utf8JsonReader reader, ref OpenAiJsonPromptStreamState state, IBufferWriter<byte> promptSink)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                state.ObjectDepth++;
                if (state.InMessagesArray && state.ArrayDepth > 0)
                {
                    state.MessageObjectDepth = state.ObjectDepth;
                }

                break;
            case JsonTokenType.EndObject:
                if (state.ObjectDepth == state.MessageObjectDepth)
                {
                    state.MessageObjectDepth = -1;
                }

                state.ObjectDepth--;
                state.CaptureNextString = false;
                break;
            case JsonTokenType.StartArray:
                state.ArrayDepth++;
                if (state.PendingMessagesArray && state.ObjectDepth == 1)
                {
                    state.InMessagesArray = true;
                    state.PendingMessagesArray = false;
                }

                break;
            case JsonTokenType.EndArray:
                state.ArrayDepth--;
                if (state.ArrayDepth == 0)
                {
                    state.InMessagesArray = false;
                }

                break;
            case JsonTokenType.PropertyName:
                if (PropertyNameEquals(reader, MessagesProperty) && state.ObjectDepth == 1)
                {
                    state.PendingMessagesArray = true;
                }

                if (PropertyNameEquals(reader, PromptProperty) && state.ObjectDepth == 1)
                {
                    state.CaptureNextString = true;
                }

                if (PropertyNameEquals(reader, ContentProperty)
                    && state.InMessagesArray
                    && state.MessageObjectDepth == state.ObjectDepth)
                {
                    state.CaptureNextString = true;
                }

                break;
            case JsonTokenType.String:
                if (state.CaptureNextString)
                {
                    AppendStringValue(reader, promptSink);
                    state.CapturedLength++;
                    state.CaptureNextString = false;
                }

                break;
        }
    }

    private static bool PropertyNameEquals(Utf8JsonReader reader, ReadOnlySpan<byte> expected) =>
        reader.ValueTextEquals(expected);

    private static void AppendStringValue(Utf8JsonReader reader, IBufferWriter<byte> sink)
    {
        if (reader.HasValueSequence)
        {
            foreach (var segment in reader.ValueSequence)
            {
                WriteSpan(segment.Span, sink);
            }

            return;
        }

        WriteSpan(reader.ValueSpan, sink);
    }

    private static void WriteSpan(ReadOnlySpan<byte> value, IBufferWriter<byte> sink)
    {
        if (value.IsEmpty)
        {
            return;
        }

        var dest = sink.GetSpan(value.Length);
        value.CopyTo(dest);
        sink.Advance(value.Length);
    }

    private sealed class NullBufferWriter : IBufferWriter<byte>
    {
        public static readonly NullBufferWriter Instance = new();

        public void Advance(int count)
        {
        }

        public Memory<byte> GetMemory(int sizeHint = 0) => Memory<byte>.Empty;

        public Span<byte> GetSpan(int sizeHint = 0) => Span<byte>.Empty;
    }
}
