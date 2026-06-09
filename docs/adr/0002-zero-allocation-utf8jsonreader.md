# ADR-0002: Zero-allocation prompt extraction via `Utf8JsonReader` over `JsonDocument`

- **Status:** Accepted
- **Date:** 2026-05-17
- **Deciders:** TensorGate maintainers

## Context

Every classified request requires extracting the prompt text (the `messages[].content`
and/or `prompt` fields of an OpenAI-compatible JSON body) from the raw request bytes
so it can be handed to the classifier. This happens on the hot path, once per
outbound request, under a sub-50ms end-to-end latency budget and high concurrency.

The idiomatic .NET approach — `JsonDocument.Parse` or `JsonSerializer.Deserialize`
into POCOs — allocates: a parsed DOM, boxed values, and intermediate `string`
instances for every field, all of which become GC pressure that causes pause-time
jitter under load.

## Decision

We will extract prompts with a forward-only **`Utf8JsonReader`** finite-state
machine that operates directly over the buffered UTF-8 request bytes
(`OpenAiJsonPromptExtractor` + `OpenAiJsonPromptStreamState`), writing the extracted
prompt bytes into a caller-provided `IBufferWriter<byte>` without materializing
intermediate strings or a JSON DOM.

The classifier contract (`IPromptClassifier.Classify(ReadOnlySpan<byte>)`) is
defined over UTF-8 spans so the extracted bytes flow to inference without a further
copy or encoding round-trip.

## Consequences

### Positive

- No DOM and no per-field `string` allocations on the extraction path — the hot path
  stays allocation-free, verifiable with BenchmarkDotNet `[MemoryDiagnoser]`.
- Forward-only reading lets us stop as soon as the needed fields are found rather than
  parsing the whole document.
- Operating over `ReadOnlySpan<byte>` aligns the extractor, classifier, and (Sprint 2)
  tokenizer on a single zero-copy data representation.

### Negative / Trade-offs

- A hand-written `Utf8JsonReader` state machine is more complex and error-prone than
  `JsonSerializer`; it must be covered by thorough unit tests over malformed,
  partial, and adversarially-nested payloads.
- The extractor encodes knowledge of the OpenAI request schema. Schema variants must
  be added to the state machine explicitly rather than handled by a serializer.

## Alternatives Considered

- **`JsonDocument` / `JsonSerializer` POCOs** — simplest to write, but allocates a DOM
  and strings per request, defeating the zero-allocation goal. Rejected on GC pressure.
- **Regex / manual string scanning** — fragile against valid JSON variations
  (escaping, unicode, whitespace) and still allocates strings. Rejected on correctness.
