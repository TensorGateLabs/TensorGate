# ADR-0004: `RefCountDisposable` over `Task.Delay` for model lifecycle

- **Status:** Accepted (implementation scheduled for Sprint 3)
- **Date:** 2026-05-17
- **Deciders:** TensorGate maintainers

## Context

TensorGate supports zero-downtime hot-swapping of the classification model so weights
can be updated without restarting the sidecar or dropping traffic. The hazard is the
classic read/reclaim race: in-flight requests may still be executing a forward pass on
the *old* `InferenceSession` at the moment a *new* one is swapped in. Disposing the old
session while a request is mid-inference on it causes access violations in the
unmanaged ONNX Runtime layer.

A naive approach is to swap the reference and then `await Task.Delay(grace)` before
disposing the old session, assuming all in-flight requests finish within the grace
window. This is a guess: it is simultaneously unsafe (a slow request can exceed the
delay) and wasteful (it pins the old session — and its ~23 MB of weights — for the
full delay even when no requests are using it).

## Decision

We will manage `InferenceSession` lifetime with an atomic, lock-free
**reference-counting** disposal pattern (`RefCountDisposable`). Each request that
begins a forward pass takes a counted lease on the current session; the lease is
released when the pass completes. A hot-swap atomically publishes the new session and
drops the swap-time reference on the old one. The old session is disposed
**deterministically** by whichever party drops the final reference — the last
in-flight request or the swap itself — using `Interlocked`/CAS, with no locks on the
hot path.

## Consequences

### Positive

- Disposal is correct by construction: the underlying session is freed only after the
  last lease is released, eliminating the use-after-free against unmanaged memory.
- No arbitrary grace period — old weights are reclaimed the instant they are no longer
  referenced, not after a fixed timeout, minimizing the double-buffered memory window.
- Lock-free leasing keeps the inference hot path free of contention.

### Negative / Trade-offs

- Reference-counting concurrency code is subtle (CAS loops, memory ordering) and is a
  documented in-scope area for the security policy; it demands stress tests and careful
  review of the acquire/release ordering.
- Every inference call pays a small interlocked acquire/release cost, though this is
  negligible relative to the forward pass itself.

## Alternatives Considered

- **`Task.Delay` grace period before dispose** — simple, but unsafe for requests slower
  than the delay and wasteful of memory for the full window. Rejected on correctness.
- **A read/write lock around the session** — correct, but serializes inference behind a
  lock and adds contention on the hot path under high concurrency. Rejected on latency.
- **Never dispose (leak old sessions)** — trivially race-free but leaks ~23 MB per swap
  and unmanaged handles. Rejected on resource exhaustion.
