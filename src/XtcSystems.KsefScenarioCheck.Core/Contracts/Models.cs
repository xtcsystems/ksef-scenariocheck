using System.Text.Json;

namespace XtcSystems.KsefScenarioCheck.Core.Contracts;

public sealed class ObservationDocument
{
    public required string SchemaVersion { get; init; }
    public required string ProfileId { get; init; }
    public required string ScenarioFamily { get; init; }
    public required string ScenarioId { get; init; }
    public required string ObservationId { get; init; }
    public string? GeneratedAt { get; init; }
    public SourceSystemInfo? SourceSystem { get; init; }
    public required List<ObservationEvent> Events { get; init; }
}

public sealed class SourceSystemInfo
{
    public required string Kind { get; init; }
    public string? Version { get; init; }
}

public sealed class ObservationEvent
{
    public required string Id { get; init; }
    public required string Kind { get; init; }
    public required string OccurredAt { get; init; }
    public required int Sequence { get; init; }
    public required string Reference { get; init; }
    public string? IssueDate { get; init; }
    public bool? OfflineMode { get; init; }
    public int? Attempt { get; init; }
}

public sealed class ScenarioPackDocument
{
    public required string SchemaVersion { get; init; }
    public required PackManifest Manifest { get; init; }
    public required List<ScenarioDefinition> Scenarios { get; init; }
}

public sealed class PackManifest
{
    public required string SchemaVersion { get; init; }
    public required string PackId { get; init; }
    public required string PackVersion { get; init; }
    public required string PackDigest { get; init; }
    public required List<string> ProfileIds { get; init; }
    public required string MinimumEngineVersion { get; init; }
    public required string MaximumEngineVersionExclusive { get; init; }
    public required string SourceSnapshot { get; init; }
    public required string OpenApiVersion { get; init; }
    public required string OpenApiBuild { get; init; }
    public required string LastVerified { get; init; }
    public required string ReviewDueAt { get; init; }
    public required string Reviewer { get; init; }
    public required string LifecycleStatus { get; init; }
    public required string VerificationStatus { get; init; }
}

public sealed class ScenarioDefinition
{
    public required string ScenarioId { get; init; }
    public required string Family { get; init; }
    public required string TitleCode { get; init; }
    public required string ProfileId { get; init; }
    public required List<AssertionDefinition> Assertions { get; init; }
    public required List<string> ExcludedRuleIds { get; init; }
}

public sealed class AssertionDefinition
{
    public required string AssertionId { get; init; }
    public required string Operator { get; init; }
    public required string RuleId { get; init; }
    public required string Classification { get; init; }
    public required List<string> SourceIds { get; init; }
    public required string SourceVersionOrCommit { get; init; }
    public required string SourceSection { get; init; }
    public required string EffectiveDate { get; init; }
    public required string LastVerified { get; init; }
    public required string ReviewDueAt { get; init; }
    public required string ReviewStatus { get; init; }
    public required string InterpretationNote { get; init; }
    public required string LifecycleStatus { get; init; }
    public required string MessageCodePassed { get; init; }
    public required string MessageCodeFailed { get; init; }
    public EventSelector? Selector { get; init; }
    public EventSelector? RightSelector { get; init; }
    public string? Field { get; init; }
    public JsonElement? Expected { get; init; }
    public string? RightTimestamp { get; init; }
    public List<string>? AssertionRefs { get; init; }
}

public sealed class EventSelector
{
    public required string Kind { get; init; }
    public string? Reference { get; init; }
    public required string Occurrence { get; init; }
}

public sealed record ValidatedObservation(
    ObservationDocument Document,
    byte[] OriginalBytes,
    string Digest);

public sealed record ValidatedPack(
    ScenarioPackDocument Document,
    byte[] OriginalBytes,
    string Digest);
