namespace TensorGate.Core.Classification;

/// <summary>
/// Placeholder classifier that allows every prompt. It stands in for the Sprint 2
/// inference pipeline so the proxy interception path can be wired, exercised, and
/// tested before the model is integrated.
/// </summary>
public sealed class AllowAllPromptClassifier : IPromptClassifier
{
    /// <inheritdoc />
    public PromptClassification Classify(ReadOnlySpan<byte> promptUtf8) => PromptClassification.Allow;
}
