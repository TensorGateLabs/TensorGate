# Contributing to TensorGate

Thank you for your interest in contributing to TensorGate. This document provides guidelines and conventions for contributing to the project.

## Getting Started

1. Fork the repository and clone your fork
2. Create a feature branch from `main`
3. Make your changes following the conventions below
4. Push to your fork and open a pull request

### Prerequisites

- [.NET 10.0 SDK (LTS)](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (optional, for container testing)
- A C# editor with EditorConfig support (VS Code, Visual Studio, Rider)
- Latest stable C# compiler support (configured centrally via `Directory.Build.props`)

### Building

```bash
dotnet restore
dotnet build --configuration Release
dotnet test
```

## Branching Strategy

| Branch Pattern | Purpose |
|:---------------|:--------|
| `main` | Stable, release-ready code |
| `feature/<issue>-<short-desc>` | New features (e.g., `feature/7-tokenizer-integration`) |
| `fix/<issue>-<short-desc>` | Bug fixes (e.g., `fix/23-sse-buffering`) |
| `refactor/<short-desc>` | Non-functional improvements |
| `docs/<short-desc>` | Documentation changes |
| `bench/<short-desc>` | Benchmark additions or changes |

Always branch from `main`. Keep branches short-lived.

## Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```text
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types:** `feat`, `fix`, `refactor`, `test`, `bench`, `docs`, `ci`, `chore`

**Scopes:** `proxy`, `tokenizer`, `inference`, `concurrency`, `memory`, `validation`, `ci`

**Examples:**
```text
feat(proxy): implement Utf8JsonReader state machine for prompt extraction
fix(inference): prevent ArrayPool lease leak on OrtValue binding failure
bench(tokenizer): add BenchmarkDotNet suite for WordPiece encoding
docs(adr): add ADR-002 for zero-allocation JSON parsing decision
```

## Pull Request Guidelines

- Reference the related issue: `Closes #7` or `Relates to #12`
- Fill out the PR template completely
- Ensure CI passes before requesting review
- Keep PRs focused — one logical change per PR
- Add tests for new functionality
- Update documentation if behavior changes

### PR Size Guidelines

| Size | Lines Changed | Review Expectation |
|:-----|:-------------|:-------------------|
| Small | < 100 | Quick review, same day |
| Medium | 100–400 | Thorough review, 1–2 days |
| Large | 400+ | Consider splitting |

## Code Style

This project uses `.editorconfig` for formatting. Run `dotnet format` before committing.

### Key Conventions

- **Naming:** PascalCase for public members, `_camelCase` for private fields
- **Nullability:** Nullable reference types are enabled — no suppressions (`!`) without a comment explaining why
- **Async:** Suffix async methods with `Async`; do not use `async void`
- **Disposal:** All `IDisposable` resources must be in `using` blocks or deterministic disposal patterns
- **Allocations:** Hot-path code must use `Span<T>`, `ArrayPool<T>`, and stack-based allocation — verify with `[MemoryDiagnoser]`
- **Comments:** Explain *why*, not *what*. No narration comments.

### Performance-Critical Code

For code in the request pipeline hot path:

- No LINQ on hot paths (use `for`/`foreach` with spans)
- No `string` concatenation or interpolation (use `Utf8JsonWriter` or `StringBuilder`)
- No closures or lambda captures (prevents hidden allocations)
- Verify with BenchmarkDotNet `[MemoryDiagnoser]` showing 0 bytes allocated

## Testing

- **Unit tests:** Required for all new public APIs
- **Integration tests:** Required for pipeline changes (proxy → tokenizer → inference)
- **Benchmarks:** Required for any hot-path changes
- **Naming:** `MethodName_Condition_ExpectedResult` (e.g., `ExtractPrompt_EmptyPayload_ReturnsEmptySpan`)

## Issue Labels

When creating issues, apply labels from these dimensions:

- **Component:** `component:proxy`, `component:tokenizer`, `component:inference`, `component:concurrency`, `component:memory`, `component:validation`, `component:ci-cd`
- **Type:** `type:feature`, `type:research`, `type:benchmark`, `type:architecture`, `type:testing`, `type:infrastructure`
- **Priority:** `priority:critical`, `priority:high`, `priority:medium`
- **Sprint:** `sprint:1`, `sprint:2`, `sprint:3`

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
