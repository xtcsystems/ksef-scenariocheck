using System.Text.Encodings.Web;
using System.Text.Json;

namespace XtcSystems.KsefScenarioCheck.Core.Reporting;

public static class ReportSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.Default,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string ToJson(ScenarioCheckReport report)
        => JsonSerializer.Serialize(report, Options) + "\n";

    public static string ToText(ScenarioCheckReport report)
    {
        ScenarioResult? scenario = report.ScenarioResults.SingleOrDefault();
        string scenarioId = scenario?.ScenarioId ?? "none";
        return string.Join("\n", new[]
        {
            $"KSeF ScenarioCheck {report.EngineVersion}",
            $"Result: {report.OverallResult}",
            $"Scenario: {scenarioId}",
            $"Assertions: {report.Summary.Passed} passed, {report.Summary.Failed} failed, {report.Summary.NeedsReview} need review",
            $"Pack: {report.PackId} {report.PackVersion} ({report.PackDigest})",
            $"Source: {report.SourceSnapshot}",
            string.Empty
        });
    }
}
