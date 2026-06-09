# ADR-0001: YARP reverse proxy over custom Kestrel middleware

- **Status:** Accepted
- **Date:** 2026-05-17
- **Deciders:** TensorGate maintainers

## Context

TensorGate is an out-of-process sidecar that intercepts outbound LLM API traffic,
inspects prompt payloads, and forwards or blocks each request before it reaches an
upstream provider. The interception layer must:

- Terminate inbound HTTP/1.1 and HTTP/2 connections and forward to a configurable
  OpenAI-compatible upstream.
- Preserve `text/event-stream` (SSE) responses without buffering, so token
  streaming stays real-time (see [ADR-0002](0002-zero-allocation-utf8jsonreader.md)
  and the `SseRollingWindow` response path).
- Expose a synchronous extensibility point where an outbound request can be
  inspected and short-circuited **before** it departs.
- Carry per-route configuration (destinations, metadata) from `appsettings.json`
  without recompilation.

The two realistic options were to build a bespoke forwarding layer directly on
Kestrel + `HttpClient`, or to adopt [YARP](https://github.com/microsoft/reverse-proxy),
Microsoft's reverse-proxy toolkit, which already solves connection pooling,
header normalization, request/response transforms, and streaming forwarding.

## Decision

We will build the proxy on **YARP**, configured via the `ReverseProxy` section of
`appsettings.json` and extended through YARP's transform pipeline.

Prompt classification is implemented as a YARP **request transform**
(`AddRequestTransform`) that attaches only to routes opting in via the
`TensorGate.Classify` route metadata flag. SSE preservation is implemented as a
response transform. We do not hand-roll connection management or forwarding.

Short-circuiting is performed within the request transform by writing a `403`
response and not forwarding — the YARP-supported mechanism — rather than by
replacing the outgoing proxied content (which YARP rejects).

## Consequences

### Positive

- Connection pooling, HTTP/2, header handling, and destination health are handled
  by a maintained Microsoft library rather than bespoke code.
- The transform pipeline gives a clean, per-route seam for both the request-side
  classifier and the response-side SSE window, keeping non-classified routes at
  zero added overhead.
- Routing/destinations are configuration-driven, so adding upstreams needs no code
  change.

### Negative / Trade-offs

- We inherit YARP's extensibility constraints. Notably, the outbound request
  `HttpContent` cannot be replaced inside a transform; to forward an inspected body
  we must buffer it and rewind `HttpContext.Request.Body` instead.
- Buffering a request body for inspection adds a copy on classified routes. This is
  acceptable for prompt-sized JSON payloads and is confined to opted-in routes.
- A YARP version bump can change transform semantics and must be validated by the
  integration tests.

## Alternatives Considered

- **Custom Kestrel middleware + `HttpClient` forwarding** — maximal control, but we
  would re-implement connection pooling, streaming, header rules, and route config
  that YARP already provides and tests. Rejected as undifferentiated heavy lifting.
- **An external proxy (Envoy/NGINX) with an out-of-band classifier** — adds a second
  runtime and a network hop inside the latency budget, and moves inspection out of
  the .NET zero-allocation pipeline. Rejected on latency and operational complexity.
