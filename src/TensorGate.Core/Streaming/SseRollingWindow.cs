namespace TensorGate.Core.Streaming;

/// <summary>
/// Fixed-size forward-only peek buffer for classifying SSE streams without rewinding.
/// </summary>
public sealed class SseRollingWindow
{
    private readonly byte[] _buffer;
    private int _length;

    public SseRollingWindow(int capacity = 64)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _buffer = new byte[capacity];
    }

    public int Length => _length;

    public ReadOnlySpan<byte> Snapshot => _buffer.AsSpan(0, _length);

    public void Append(ReadOnlySpan<byte> chunk)
    {
        if (chunk.IsEmpty)
        {
            return;
        }

        if (chunk.Length >= _buffer.Length)
        {
            chunk.Slice(chunk.Length - _buffer.Length).CopyTo(_buffer);
            _length = _buffer.Length;
            return;
        }

        var overflow = (_length + chunk.Length) - _buffer.Length;
        if (overflow > 0)
        {
            _buffer.AsSpan(overflow, _length - overflow).CopyTo(_buffer);
            _length -= overflow;
        }

        chunk.CopyTo(_buffer.AsSpan(_length));
        _length += chunk.Length;
    }

    /// <summary>
    /// Heuristic: SSE payloads commonly begin with "data:", "event:", or ":" (comment).
    /// </summary>
    public bool LooksLikeSsePayload()
    {
        if (_length == 0)
        {
            return false;
        }

        var text = System.Text.Encoding.UTF8.GetString(_buffer, 0, _length);
        return text.StartsWith("data:", StringComparison.Ordinal)
            || text.StartsWith("event:", StringComparison.Ordinal)
            || text.StartsWith(':')
            || text.StartsWith("id:", StringComparison.Ordinal)
            || text.StartsWith("retry:", StringComparison.Ordinal);
    }
}
