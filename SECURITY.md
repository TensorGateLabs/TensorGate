# Security Policy

## Supported Versions

| Version | Supported          |
|:--------|:-------------------|
| 0.1.x   | :white_check_mark: |

## Reporting a Vulnerability

**Do not report security vulnerabilities through public GitHub issues.**

If you discover a security vulnerability in TensorGate, please report it responsibly:

1. **Email:** Send a detailed report to the repository maintainer via the contact information on their [GitHub profile](https://github.com/syed-dawood).
2. **Include:**
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
