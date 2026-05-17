# TensorGate

**Production-grade ASP.NET Core middleware for AI safety** — a zero-allocation
YARP reverse proxy with local ONNX inference for real-time LLM payload
inspection, prompt injection detection, and semantic sanitization.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![NIST AI RMF](https://img.shields.io/badge/NIST-AI%20600--1%20RMF-green.svg)](https://www.nist.gov/artificial-intelligence)

---

## Overview

TensorGate is an out-of-process containerized sidecar that intercepts,
evaluates, and sanitizes Large Language Model (LLM) traffic in real time.
It sits between your application and upstream LLM providers as a YARP
reverse proxy, running local INT8-quantized ONNX classification models
to detect prompt injections and adversarial payloads within a strict
sub-50ms latency budget on pure CPU hardware.

### Key Design Principles

- **Zero-Allocation Pipeline** — From raw HTTP bytes to ONNX tensor evaluation,
  the hot path avoids managed heap allocations using `Span<T>`,
  `ArrayPool<T>`, and `Utf8JsonReader`/`Utf8JsonWriter` to eliminate GC pauses
  under high concurrency.
- **CPU-Only Inference** — INT8 statically quantized `all-MiniLM-L6-v2` achieves 8–12ms classification latency via AVX-512 VNNI, fitting entirely within L3 cache (~23 MB).
- **SSE Stream Preservation** — Transparent forwarding of `text/event-stream` responses without buffering, maintaining real-time token streaming from upstream providers.
- **Lock-Free Hot Reload** — Atomic reference-counted model swapping via `RefCountDisposable` pattern enables zero-downtime weight updates without race conditions or access violations.
- **NIST AI RMF Alignment** — Architecture maps directly to the Govern, Map, Measure, and Manage pillars of NIST AI 600-1.

## Architecture

```text
┌─────────────┐     ┌──────────────────────────────────────────┐     ┌──────────────┐
│  Application │────▶│              TensorGate Sidecar           │────▶│  LLM Provider │
│  (Internal)  │◀────│                                          │◀────│  (Upstream)   │
└─────────────┘     │  ┌────────┐  ┌───────────┐  ┌─────────┐ │     └──────────────┘
                    │  │  YARP   │─▶│ Tokenizer │─▶│  ONNX   │ │
                    │  │ Proxy   │  │ (Zero-    │  │ Runtime │ │
                    │  │         │  │  Alloc)   │  │ (INT8)  │ │
                    │  └────────┘  └───────────┘  └─────────┘ │
                    └──────────────────────────────────────────┘
```

### Pipeline Flow

1. **Network Interception** — YARP captures outbound LLM API traffic via `AddRequestTransform`
2. **Zero-Alloc JSON Parsing** — `Utf8JsonReader` state machine extracts prompt fields directly from the byte stream
3. **Tokenization** — `Microsoft.ML.Tokenizers` (BertTokenizer/WordPiece) encodes over `ReadOnlySpan<char>` without intermediate string allocations
4. **Tensor Binding** — `ArrayPool<long>` leased buffers are pinned and bound to `OrtValue.CreateTensorValueFromMemory`
5. **Classification** — Single forward pass through INT8 MiniLM yields Safe/Malicious probability in 8–12ms
6. **Decision Gate** — Malicious payloads are blocked synchronously; safe payloads stream through unmodified

## Technology Stack

| Layer | Technology | Purpose |
|:------|:-----------|:--------|
| Reverse Proxy | [YARP](https://github.com/microsoft/reverse-proxy) | Traffic interception and SSE stream forwarding |
| JSON Processing | `Utf8JsonReader` / `Utf8JsonWriter` | Zero-allocation payload parsing |
| Tokenization | [Microsoft.ML.Tokenizers](https://github.com/dotnet/machinelearning) | Allocation-free BPE/WordPiece encoding |
| Inference | [ONNX Runtime](https://github.com/microsoft/onnxruntime) | INT8 quantized CPU inference |
| Model | [all-MiniLM-L6-v2](https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2) | Sequence classification (22.7M params) |
| Concurrency | `Interlocked` / `Volatile` / CAS loops | Lock-free reference counting |
| Validation | [HarmBench](https://github.com/centerforaisafety/HarmBench) | Adversarial red-team evaluation |

## Performance Targets

| Metric | Target | Mechanism |
|:-------|:-------|:----------|
| End-to-end latency | < 50ms | INT8 quantization + AVX-512 VNNI |
| Inference latency | 8–12.3ms | Static quantization, L3 cache residency |
| Heap allocations | 0 bytes on hot path | `Span<T>`, `ArrayPool<T>`, `Utf8JsonReader` |
| Model memory | ~23 MB | INT8 weight compression |
| Model hot-reload | Zero downtime | Atomic `RefCountDisposable` double buffering |

## Project Status

This project is under active development following a structured sprint cadence:

| Sprint | Focus | Duration |
|:-------|:------|:---------|
| Sprint 1 | Foundational Scaffolding & Proxy Mechanics | Days 1–14 |
| Sprint 2 | Memory Optimization & Inference Engines | Days 15–28 |
| Sprint 3 | Concurrency, Hot-Swapping & Validation | Days 29–42 |

Track progress on the [TensorGate Project Board](https://github.com/users/syed-dawood/projects/1).

## Getting Started

> **Prerequisites:** .NET 10.0 SDK (LTS), Docker (optional for sidecar deployment)
>
> **Language policy:** TensorGate tracks the latest stable C# language version via central build settings.

```bash
# Clone the repository
git clone https://github.com/syed-dawood/TensorGate.git
cd TensorGate

# Build
dotnet build

# Run tests
dotnet test

# Run the sidecar
dotnet run --project src/TensorGate.Proxy
```

## Contributing

Contributions are welcome. Please read the [Contributing Guidelines](CONTRIBUTING.md) before submitting a pull request.
For operational flow and engineering runbooks, see
[Operating Pipeline](docs/operating-pipeline.md).
For Cursor-native agent/MCP execution strategy, see
[Cursor Agent Ops](docs/cursor-agent-ops.md).
For full lifecycle automation design, see
[Phase 2 Intelligent Orchestration](docs/phase2-intelligent-orchestration.md).

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

## References

- [NIST AI 600-1 Risk Management Framework](https://www.nist.gov/artificial-intelligence)
- [OWASP Top 10 for LLM Applications](https://owasp.org/www-project-top-10-for-large-language-model-applications/)
- [HarmBench: A Standardized Evaluation Framework for Automated Red Teaming](https://github.com/centerforaisafety/HarmBench)
- [Microsoft YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [ONNX Runtime C# API](https://onnxruntime.ai/docs/get-started/with-csharp.html)
