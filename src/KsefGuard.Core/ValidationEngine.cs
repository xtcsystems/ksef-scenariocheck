using System.Reflection;

namespace KsefGuard;

public sealed class ValidationEngine
{
    private readonly ScenarioPackLoader _loader;

    public ValidationEngine(ScenarioPackLoader? loader = null)
    {
        _loader = loader ?? new ScenarioPackLoader();
    }

    public async Task<ValidationReport> ValidateAsync(
        string packPath,
        DateTimeOffset? runTimestamp = null,
        CancellationToken cancellationToken = default)
    {
        await using var loaded = await _loader.LoadAsync(packPath, cancellationToken).ConfigureAwait(false);
        var scenarioResults = new List<ScenarioResult>(loaded.Manifest.Scenarios.Count);

        foreach (var scenario in loaded.Manifest.Scenarios)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var validator = ScenarioValidatorFactory.Get(scenario.Type);
            var findings = await validator.ValidateAsync(loaded.RootPath, scenario, cancellationToken).ConfigureAwait(false);
            var status = Aggregate(findings.Select(item => item.Status));
            scenarioResults.Add(new ScenarioResult(scenario.Id, scenario.Type, status, findings));
        }

        var allFindings = scenarioResults.SelectMany(item => item.Findings).ToArray();
        var summary = new ReportSummary(
            allFindings.Count(item => item.Status == CheckStatus.Pass),
            allFindings.Count(item => item.Status == CheckStatus.Fail),
            allFindings.Count(item => item.Status == CheckStatus.NeedsReview));

        return new ValidationReport(
            ToolVersion: GetToolVersion(),
            ScenarioPackSchemaVersion: loaded.Manifest.SchemaVersion,
            PackId: loaded.Manifest.Id,
            PackVersion: loaded.Manifest.Version,
            RunTimestamp: runTimestamp ?? DateTimeOffset.UtcNow,
            Status: Aggregate(scenarioResults.Select(item => item.Status)),
            Summary: summary,
            Scenarios: scenarioResults);
    }

    private static CheckStatus Aggregate(IEnumerable<CheckStatus> statuses)
    {
        var values = statuses.ToArray();
        if (values.Contains(CheckStatus.Fail))
        {
            return CheckStatus.Fail;
        }

        return values.Contains(CheckStatus.NeedsReview) ? CheckStatus.NeedsReview : CheckStatus.Pass;
    }

    private static string GetToolVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(ValidationEngine).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0-unknown";
    }
}
