using System.Text.Json.Serialization;

namespace KsefGuard;

[JsonConverter(typeof(JsonStringEnumConverter<CheckStatus>))]
public enum CheckStatus
{
    Pass,
    Fail,
    NeedsReview
}

public sealed record LocalizedText(string En, string Pl);

public sealed record SourceReference(string Title, string Url);

public sealed record ScenarioDefinition(
    string Id,
    string Type,
    string Fixture,
    string Expected);

public sealed record ScenarioPackManifest(
    int SchemaVersion,
    string Id,
    string Version,
    LocalizedText Title,
    IReadOnlyList<SourceReference> SourceReferences,
    IReadOnlyList<ScenarioDefinition> Scenarios);

public sealed record Finding(
    string Code,
    CheckStatus Status,
    string ScenarioId,
    string MessageEn,
    string MessagePl,
    string? ArtifactPath = null,
    string? Detail = null);

public sealed record ScenarioResult(
    string Id,
    string Type,
    CheckStatus Status,
    IReadOnlyList<Finding> Findings);

public sealed record ReportSummary(int Passed, int Failed, int NeedsReview);

public sealed record ValidationReport(
    string ToolVersion,
    int ScenarioPackSchemaVersion,
    string PackId,
    string PackVersion,
    DateTimeOffset RunTimestamp,
    CheckStatus Status,
    ReportSummary Summary,
    IReadOnlyList<ScenarioResult> Scenarios);

public sealed record LoadedScenarioPack(
    string RootPath,
    bool DeleteOnDispose,
    ScenarioPackManifest Manifest) : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        if (DeleteOnDispose && Directory.Exists(RootPath))
        {
            try
            {
                Directory.Delete(RootPath, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup for a temporary unpack directory.
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup for a temporary unpack directory.
            }
        }

        return ValueTask.CompletedTask;
    }
}

public class PackValidationException : Exception
{
    public PackValidationException(string message) : base(message) { }
    public PackValidationException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class UnsafePackException : PackValidationException
{
    public UnsafePackException(string message) : base(message) { }
}
