# ADR-0003: INT8 ONNX CPU inference over GPU-dependent alternatives

- **Status:** Accepted (implementation scheduled for Sprint 2)
- **Date:** 2026-05-17
- **Deciders:** TensorGate maintainers

## Context

TensorGate classifies each outbound prompt as safe or malicious inline, within a
sub-50ms end-to-end budget, while deployed as a lightweight sidecar next to the
application it protects. The classification model (`all-MiniLM-L6-v2`, ~22.7M
parameters) must run wherever the sidecar runs — typically commodity CPU containers,
often without an attached GPU.

The options span a hardware/runtime spectrum: GPU-accelerated inference (PyTorch/
CUDA or ONNX Runtime CUDA EP), FP32 CPU inference, or statically-quantized INT8 CPU
inference via ONNX Runtime.

## Decision

We will run classification as **INT8 statically-quantized ONNX** models on **CPU**
via **ONNX Runtime**, targeting AVX-512 VNNI where available.

The sidecar ships no GPU dependency. The quantized MiniLM weights compress to
~23 MB, sized to stay resident in L3 cache, yielding a target inference latency of
8–12ms per forward pass.

## Consequences

### Positive

- The sidecar runs on any CPU container — no GPU scheduling, no CUDA/driver matrix,
  no device plugins — which keeps deployment cost and operational surface low.
- INT8 + VNNI hits the latency budget on commodity hardware; ~23 MB weights stay in
  L3, avoiding memory-bandwidth stalls.
- A single ONNX artifact is portable across the runtimes ONNX Runtime supports.

### Negative / Trade-offs

- INT8 quantization introduces a small accuracy delta versus FP32. This must be
  measured against the adversarial validation suite (HarmBench) and the quantization
  recipe tuned (per-channel, calibration set) to keep recall acceptable.
- Throughput per instance is bounded by CPU; very high request volumes scale
  horizontally (more sidecars) rather than vertically (a bigger GPU).
- Requires an offline quantization/calibration step in the model build pipeline.

## Alternatives Considered

- **GPU inference (CUDA EP / PyTorch)** — lowest latency at scale, but forces a GPU
  onto every sidecar, exploding deployment cost and operational complexity for a
  22.7M-parameter model that does not need it. Rejected for a CPU-sidecar product.
- **FP32 CPU inference** — simplest numerically, but ~4× the memory footprint and
  latency of INT8, breaking L3 residency and the latency budget. Rejected on perf.
