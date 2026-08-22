# Offline24 implementation start

```text
Authorized: 2026-08-22
Repository: xtcsystems/ksef-scenariocheck
Base: 161b3e312a82b1a37b71122791f415a22f30a772
Branch: implementation/o-0001-ksef-scenariocheck-free-v0.1
Scope: offline24-timeline only
Reviewer: owner:xtcsystems
Security channel: GitHub private vulnerability reporting / Security Advisories
Runtime recheck: .NET SDK 10.0.400 / runtime 10.0.11
```

The .NET refresh stays within the approved .NET 10 architecture and records the current security patch available at implementation start.

No exact public NuGet package named `XtcSystems.KsefScenarioCheck.Tool` resolved during the start-time recheck. This is an availability observation only; the package is not reserved or published.

The implementation uses a BCL-only test harness, so the proposed MSTest and JsonSchema.Net test dependencies are not introduced.

Temporary owner-time rule: historical owner time remains unknown and is not treated as zero. Count all new owner attention from this authorization; issue an early warning at three actual hours and hard-pause at four actual hours. This rule authorizes no release work.

Codex implementation, Claude review, one repair cycle, EUR 0 cash and no spike salvage remain the delivery constraints.
