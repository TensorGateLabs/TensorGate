namespace TensorGate.Core.Classification;

/// <summary>
/// Outcome of classifying an extracted prompt payload.
/// </summary>
public enum PromptClassification
{
    /// <summary>The prompt is permitted and the request may be forwarded upstream.</summary>
    Allow,

    /// <summary>The prompt is rejected and the request must be blocked before it leaves the proxy.</summary>
    Block,
}
