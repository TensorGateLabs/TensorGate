# TensorGate Professional Setup - Top 20 Execution Plan

## Scope

This plan defines 20 high-value repository setup controls for a production-grade open-source .NET middleware project, then records implementation status.

## Top 20 Controls

| # | Control | Category | Status |
|---|---|---|---|
| 1 | Pin SDK to .NET 10 LTS with `global.json` | Runtime governance | Done |
| 2 | Enforce latest C# via `Directory.Build.props` (`LangVersion=latest`) | Language governance | Done |
| 3 | Centralize `TargetFramework=net10.0` | Build consistency | Done |
| 4 | CI build/test on .NET 10 | CI | Done |
| 5 | Formatting gate (`dotnet format --verify-no-changes`) | CI quality | Done |
| 6 | Release workflow on semantic tags (`v*.*.*`) using .NET 10 | Release | Done |
| 7 | CodeQL scanning for C# | Security | Done |
| 8 | Dependency review on pull requests | Supply chain | Done |
| 9 | Secret scanning (`gitleaks`) | Security | Done |
| 10 | OpenSSF Scorecards workflow | Security posture | Done |
| 11 | SBOM generation and artifact publishing | Supply chain | Done |
| 12 | Dependabot for NuGet | Maintenance | Done |
| 13 | Dependabot for GitHub Actions | Maintenance | Done |
| 14 | PR title validation (conventional style) | Contribution quality | Done |
| 15 | Branch name validation against project convention | Contribution quality | Done |
| 16 | Automatic PR size labeling | Review ergonomics | Done |
| 17 | Automatic file-path-based labeling | Triage automation | Done |
| 18 | Documentation quality workflow (markdown lint + link check) | Docs quality | Done |
| 19 | Release draft automation | Release management | Done |
| 20 | CODEOWNERS for ownership and review routing | Governance | Done |

## Notes on Manual Platform Settings

Some controls require repository settings API operations and cannot be fully represented as files:

- Branch protection rules for `main`
- Required status checks
- Optional merge strategy restrictions

These are managed through GitHub repository settings or API automation.

## .NET / C# Upgrade Notes

- All active repository references now target `.NET 10`.
- C# version policy is now `latest` (current stable compiler language version for installed SDK).
