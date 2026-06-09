namespace TensorGate.Core.Classification;

/// <summary>
/// Synchronous classification entry point for extracted prompt bytes.
/// Operates over the UTF-8 prompt produced by
/// <see cref="Json.OpenAiJsonPromptExtractor"/> without further allocations.
/// </summary>
/// <remarks>
/// The Sprint 1 implementation is a placeholder (<see cref="AllowAllPromptClassifier"/>).
/// Sprint 2 replaces it with the INT8 ONNX MiniLM inference pipeline; the contract
/// stays synchronous so the decision can be made before the outbound request departs.
/// </remarks>
public interface IPromptClassifier
{
    /// <summary>
    /// Classifies the supplied UTF-8 prompt bytes.
    /// </summary>
    /// <param name="promptUtf8">The extracted prompt content, UTF-8 encoded.</param>
    /// <returns>The classification decision.</returns>
    PromptClassification Classify(ReadOnlySpan<byte> promptUtf8);
}
