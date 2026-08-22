
# Getting started from source

```bash
dotnet build tests/XtcSystems.KsefScenarioCheck.Tests/XtcSystems.KsefScenarioCheck.Tests.csproj -c Release
dotnet run --project tests/XtcSystems.KsefScenarioCheck.Tests/XtcSystems.KsefScenarioCheck.Tests.csproj -c Release --no-build
dotnet run --project src/XtcSystems.KsefScenarioCheck.Cli -- run --observation examples/observations/offline24.pass.json --as-of 2026-09-01T00:00:00Z --output report.json
```

All example identifiers and dates are synthetic.
