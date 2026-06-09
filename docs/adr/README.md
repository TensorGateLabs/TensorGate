# Architectural Decision Records

This directory captures the significant architectural decisions for TensorGate
using the [Michael Nygard format](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
(Context → Decision → Consequences).

An ADR records *why* a decision was made, not just *what* was built. Once an ADR
is **Accepted** it is immutable — superseding decisions are recorded as new ADRs
that reference the ones they replace.

## Index

| ADR | Title | Status |
|:----|:------|:-------|
| [ADR-0001](0001-yarp-reverse-proxy.md) | YARP reverse proxy over custom Kestrel middleware | Accepted |
| [ADR-0002](0002-zero-allocation-utf8jsonreader.md) | Zero-allocation prompt extraction via `Utf8JsonReader` over `JsonDocument` | Accepted |
| [ADR-0003](0003-int8-onnx-cpu-inference.md) | INT8 ONNX CPU inference over GPU-dependent alternatives | Accepted |
| [ADR-0004](0004-refcountdisposable-model-lifecycle.md) | `RefCountDisposable` over `Task.Delay` for model lifecycle | Accepted |

## Creating a new ADR

1. Copy [`template.md`](template.md) to `NNNN-short-title.md`, using the next
   zero-padded number.
2. Fill in Context, Decision, and Consequences. Keep it concise — an ADR is a
   record, not a design doc.
3. Add a row to the index above.
4. Open a PR with a `docs(adr):` commit referencing the motivating issue.
