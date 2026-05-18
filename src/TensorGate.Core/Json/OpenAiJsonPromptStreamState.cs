using System.Text.Json;

namespace TensorGate.Core.Json;

/// <summary>
/// FSM state for incremental OpenAI-compatible JSON prompt extraction across chunks.
/// </summary>
public struct OpenAiJsonPromptStreamState
{
    internal JsonReaderState ReaderState;
    internal bool ReaderInitialized;
    internal ParserPhase Phase;
    internal int ObjectDepth;
    internal int ArrayDepth;
    internal bool InMessagesArray;
    internal bool PendingMessagesArray;
    internal bool CaptureNextString;
    internal int MessageObjectDepth;
    internal int CapturedLength;

    public void Reset()
    {
        ReaderState = default;
        ReaderInitialized = false;
        Phase = ParserPhase.Initial;
        ObjectDepth = 0;
        ArrayDepth = 0;
        InMessagesArray = false;
        PendingMessagesArray = false;
        CaptureNextString = false;
        MessageObjectDepth = -1;
        CapturedLength = 0;
    }
}

internal enum ParserPhase
{
    Initial,
    Streaming,
    Complete,
}
