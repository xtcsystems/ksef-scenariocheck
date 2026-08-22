
# KSeF ScenarioCheck

KSeF ScenarioCheck is a local, deterministic developer tool for evaluating **synthetic** KSeF integration observations against source-grounded modeled scenarios.

> **Development status:** the `offline24-timeline` vertical slice is under implementation. There is no supported public release yet.

KSeF ScenarioCheck is independent. It is not affiliated with or endorsed by the Polish Ministry of Finance or CIRFMF. It does not provide tax, accounting, legal, or compliance advice and does not certify that an implementation complies with KSeF requirements.

## Current slice

The first slice accepts a bounded JSON observation produced by a test harness, evaluates one modeled `offline24-timeline` scenario, and emits deterministic text and JSON evidence for local development or single-repository CI.

It deliberately does **not** contact KSeF, accept invoice XML, NIP values, credentials, tokens, certificates or private keys, drive an ERP/SDK/gateway, calculate Polish legal business days, return legal or tax conclusions, or collect telemetry.

## Build and test

The repository pins .NET SDK `10.0.302`, targets `net10.0`, and has no third-party NuGet dependencies.

```bash
dotnet build tests/XtcSystems.KsefScenarioCheck.Tests/XtcSystems.KsefScenarioCheck.Tests.csproj -c Release
dotnet run --project tests/XtcSystems.KsefScenarioCheck.Tests/XtcSystems.KsefScenarioCheck.Tests.csproj -c Release --no-build
```

A local prerelease package can be built without publishing it:

```bash
dotnet pack src/XtcSystems.KsefScenarioCheck.Cli/XtcSystems.KsefScenarioCheck.Cli.csproj -c Release -o artifacts
```

Example source run:

```bash
dotnet run --project src/XtcSystems.KsefScenarioCheck.Cli -- run   --observation examples/observations/offline24.pass.json   --as-of 2026-09-01T00:00:00Z   --output report.json
```

The explicit `--as-of` value is part of the deterministic input. It prevents freshness state from depending on the machine clock.

The public code, schemas, examples and independently authored baseline content are Apache-2.0 licensed. Contributions use DCO sign-off. Security reports belong in GitHub's private vulnerability-reporting / Security Advisory channel; see [SECURITY.md](SECURITY.md).

`retry-recovery-trace`, package publication, merge/release and observation remain outside this vertical slice.
