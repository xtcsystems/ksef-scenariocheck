using System.Text.Json.Serialization;

namespace XtcSystems.KsefScenarioCheck.Core.Reporting;

public sealed class ScenarioCheckReport
{
    [JsonPropertyOrder(0)] public required string ReportSchemaVersion { get; init; }
    [JsonPropertyOrder(1)] public required string EngineVersion { get; init; }
    [JsonPropertyOrder(2)] public required string EvaluatedAt { get; init; }
    [JsonPropertyOrder(3)] public required string OverallResult { get; init; }
    [JsonPropertyOrder(4)] public string? ObservationSchemaVersion { get; init; }
    [JsonPropertyOrder(5)] public required string ObservationDigest { get; init; }
    [JsonPropertyOrder(6)] public string? PackId { get; init; }
    [JsonPropertyOrder(7)] public string? PackVersion { get; init; }
    [JsonPropertyOrder(8)] public string? PackDigest { get; init; }
    [JsonPropertyOrder(9)] public string? ProfileId { get; init; }
    [JsonPropertyOrder(10)] public string? SourceSnapshot { get; init; }
    [JsonPropertyOrder(11)] public string? LastVerified { get; init; }
    [JsonPropertyOrder(12)] public string? ReviewDueAt { get; init; }
    [JsonPropertyOrder(13)] public required bool Stale { get; init; }
    [JsonPropertyOrder(14)] public string? LifecycleStatus { get; init; }
    [JsonPropertyOrder(15)] public string? VerificationStatus { get; init; }
    [JsonPropertyOrder(16)] public required List<ScenarioResult> ScenarioResults { get; init; }
    [JsonPropertyOrder(17)] public required List<Finding> Findings { get; init; }
    [JsonPropertyOrder(18)] public required ReportSummary Summary { get; init; }
}

public sealed class ScenarioResult
{
    [JsonPropertyOrder(0)] public required string ScenarioId { get; init; }
    [JsonPropertyOrder(1)] public required string Family { get; init; }
    [JsonPropertyOrder(2)] public required string Status { get; init; }
    [JsonPropertyOrder(3)] public required List<AssertionResult> AssertionResults { get; init; }
}

public sealed class AssertionResult
{
    [JsonPropertyOrder(0)] public required string AssertionId { get; init; }
    [JsonPropertyOrder(1)] public required string RuleId { get; init; }
    [JsonPropertyOrder(2)] public required string Classification { get; init; }
    [JsonPropertyOrder(3)] public required string Status { get; init; }
    [JsonPropertyOrder(4)] public required string MessageCode { get; init; }
}

public sealed class Finding
{
    [JsonPropertyOrder(0)] public required string FindingId { get; init; }
    [JsonPropertyOrder(1)] public required string AssertionId { get; init; }
    [JsonPropertyOrder(2)] public required string RuleId { get; init; }
    [JsonPropertyOrder(3)] public required string Classification { get; init; }
    [JsonPropertyOrder(4)] public required string ScenarioId { get; init; }
    [JsonPropertyOrder(5)] public required string Status { get; init; }
    [JsonPropertyOrder(6)] public required string MessageCode { get; init; }
    [JsonPropertyOrder(7)] public required string SafeMessage { get; init; }
    [JsonPropertyOrder(8)] public required List<string> SourceIds { get; init; }
}

public sealed class ReportSummary
{
    [JsonPropertyOrder(0)] public required int ScenariosTotal { get; init; }
    [JsonPropertyOrder(1)] public required int AssertionsTotal { get; init; }
    [JsonPropertyOrder(2)] public required int Passed { get; init; }
    [JsonPropertyOrder(3)] public required int Failed { get; init; }
    [JsonPropertyOrder(4)] public required int NeedsReview { get; init; }
    [JsonPropertyOrder(5)] public required int Unsupported { get; init; }
}
