using System.Text;
using TensorGate.Core.Streaming;

namespace TensorGate.Tests.Streaming;

public sealed class SseRollingWindowTests
{
    [Fact]
    public void LooksLikeSsePayload_DetectsDataPrefix()
    {
        var window = new SseRollingWindow();
        window.Append(Encoding.UTF8.GetBytes("data: {\"a\":1}"));
        Assert.True(window.LooksLikeSsePayload());
    }

    [Fact]
    public void Append_SlidesWindowWhenOverflowing()
    {
        var window = new SseRollingWindow(capacity: 8);
        window.Append(Encoding.UTF8.GetBytes("data: abc"));
        window.Append(Encoding.UTF8.GetBytes("defghijklmnop"));
        Assert.Equal(8, window.Length);
        Assert.Equal("ijklmnop", Encoding.UTF8.GetString(window.Snapshot));
    }
}
