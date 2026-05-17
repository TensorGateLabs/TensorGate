# Contributing to TensorGate

Thank you for your interest in contributing to TensorGate. This document provides guidelines and information to help you contribute effectively.

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- Git
- A C# editor (VS Code with C# Dev Kit, Visual Studio, or Rider)

### Setting Up

```bash
git clone https://github.com/syed-dawood/TensorGate.git
cd TensorGate
dotnet restore
dotnet build
dotnet test
```

## How to Contribute

### Reporting Issues

- Use the [issue templates](https://github.com/syed-dawood/TensorGate/issues/new/choose) to report bugs, request features, or submit benchmark findings.
- Search existing issues before creating a new one.
- Provide as much context as possible — .NET version, OS, error messages, and steps to reproduce.

### Submitting Pull Requests

1. **Fork** the repository and create a feature branch from `main`:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes.** Follow the coding standards below.

3. **Write tests.** All new functionality must have corresponding tests. Performance-critical changes must include benchmark results.

4. **Verify locally:**
   ```bash
   dotnet build --configuration Release /p:TreatWarningsAsErrors=true
   dotnet test --configuration Release
   dotnet format --verify-no-changes
   ```

5. **Commit** with clear, descriptive messages (see commit conventions below).

6. **Push** to your fork and open a pull request against `main`.

7. **Fill out** the PR template completely.

### Commit Message Conventions

This project follows [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:** `feat`, `fix`, `perf`, `refactor`, `test`, `docs`, `ci`, `chore`

**Scopes:** `proxy`, `tokenizer`, `inference`, `concurrency`, `memory`, `ci`, `docs`

**Examples:**
```
feat(proxy): implement Utf8JsonReader FSM for zero-alloc JSON extraction
fix(inference): prevent ArrayPool leak on OrtValue binding exception
perf(tokenizer): reduce BertTokenizer encode time by 15% via span optimization
test(concurrency): add stress test for RefCountDisposable under 500 threads
docs: update architecture diagram with hot-reload sequence
ci: add format check to CI pipeline
```

## Coding Standards

### General

- Target **.NET 9.0** with C# 13 language features.
- Treat all warnings as errors in Release configuration.
- Use `dotnet format` to enforce style. The `.editorconfig` in the repo root defines the rules.

### Performance-Critical Code

This project has strict zero-allocation requirements on the hot path. When contributing to performance-critical components:

- **No LINQ on the hot path.** LINQ allocates enumerator objects. Use `for`/`foreach` over `Span<T>` or arrays.
- **No `string` manipulation on the hot path.** Use `ReadOnlySpan<char>` or `ReadOnlySpan<byte>`.
- **Use `ArrayPool<T>`** instead of `new T[]` for temporary buffers. Always return buffers in `finally` blocks.
- **Avoid `async`/`await`** in the inference pipeline. The `Utf8JsonReader` is a `ref struct` and cannot cross async boundaries.
- **No boxing.** Avoid casting value types to `object` or interfaces.
- **Benchmark your changes.** Include BenchmarkDotNet results showing allocation counts and median latency.

### Naming Conventions

| Element | Convention | Example |
|:--------|:-----------|:--------|
| Class / Struct | PascalCase | `RefCountDisposable` |
| Interface | IPascalCase | `IModelSession` |
| Method | PascalCase | `AcquireReference()` |
| Local variable | camelCase | `tokenCount` |
| Private field | _camelCase | `_activeSession` |
| Constant | PascalCase | `MaxSequenceLength` |

## Architecture Decision Records

Significant architectural decisions are documented as ADRs in `docs/adr/`. If your contribution involves a meaningful design choice, please add or update an ADR following the existing format.

## Review Process

- All PRs require at least one approval before merging.
- CI must pass (build, tests, format check).
- Performance-critical PRs require benchmark results in the PR description.
- Breaking changes require discussion in a GitHub issue before implementation.

## License

By contributing to TensorGate, you agree that your contributions will be licensed under the [MIT License](LICENSE).
