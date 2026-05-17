# Security Policy

## Supported Versions

| Version | Supported          |
|:--------|:-------------------|
| 0.1.x   | :white_check_mark: |

## Reporting a Vulnerability

**Do not report security vulnerabilities through public GitHub issues.**

If you discover a security vulnerability in TensorGate, please report it responsibly:

1. **GitHub Security Advisories:** Use [Report a vulnerability](https://github.com/TensorGateLabs/TensorGate/security/advisories/new) on this repository (preferred).
2. **Maintainer contact:** Reach the TensorGateLabs maintainers via [GitHub Discussions](https://github.com/TensorGateLabs/TensorGate/discussions) only for coordination after submitting an advisory — do not post exploit details publicly.

**Include in your report:**
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact assessment
   - Suggested fix (if any)

### What to Expect

- **Acknowledgment** within 48 hours of your report.
- **Assessment** and severity classification within 7 days.
- **Fix timeline** communicated based on severity:
  - Critical: patch within 48 hours
  - High: patch within 7 days
  - Medium/Low: addressed in next scheduled release

### Scope

The following are in scope for security reports:

- Prompt injection bypasses (adversarial inputs that evade the classification model)
- Memory safety issues (buffer overflows, use-after-free in unmanaged bindings)
- Race conditions in the `RefCountDisposable` concurrency model
- Information leakage through the proxy layer
- Denial of service via resource exhaustion

### Out of Scope

- Vulnerabilities in upstream dependencies (report to the respective projects)
- Theoretical attacks without a proof of concept
- Social engineering

## Security Design Principles

TensorGate is built with security as a core architectural concern:

- **Defense in depth:** Multiple validation layers before traffic reaches upstream providers
- **Zero-trust proxy:** All LLM traffic is inspected regardless of source
- **Deterministic resource management:** Lock-free reference counting prevents resource leaks and access violations
- **Continuous adversarial validation:** Automated HarmBench testing in CI/CD pipeline
