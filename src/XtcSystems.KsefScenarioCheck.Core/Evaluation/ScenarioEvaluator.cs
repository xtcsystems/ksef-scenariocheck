using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using XtcSystems.KsefScenarioCheck.Core.Contracts;
using XtcSystems.KsefScenarioCheck.Core.Reporting;

namespace XtcSystems.KsefScenarioCheck.Core.Evaluation;

public sealed class ScenarioEvaluator
{
    public ScenarioCheckReport Evaluate(
        ValidatedObservation observation,
        ValidatedPack pack,
        string evaluatedAt,
        DateTimeOffset evaluatedAtInstant,
        string engineVersion)
    {
        ScenarioPackDocument packDocument = pack.Document;
        PackManifest manifest = packDocument.Manifest;
        ObservationDocument observationDocument = observation.Document;
        ScenarioDefinition? scenario = packDocument.Scenarios.SingleOrDefault(
            item => string.Equals(item.ScenarioId, observationDocument.ScenarioId, StringComparison.Ordinal));

        bool profileSupported = manifest.ProfileIds.Contains(observationDocument.ProfileId, StringComparer.Ordinal) &&
                                scenario is not null &&
                                string.Equals(scenario.ProfileId, observationDocument.ProfileId, StringComparison.Ordinal) &&
                                string.Equals(scenario.Family, observationDocument.ScenarioFamily, StringComparison.Ordinal);
        bool lifecycleSupported = manifest.LifecycleStatus is "SUPPORTED" or "MAINTENANCE_ONLY";
        DateTimeOffset reviewDueAt = ContractValidator.ParseUtc(manifest.ReviewDueAt, "KSC.PACK.INVALID");
        bool stale = evaluatedAtInstant > reviewDueAt;

        var assertionResults = new List<AssertionResult>();
        var findings = new List<Finding>();
        var statuses = new Dictionary<string, string>(StringComparer.Ordinal);

        if (profileSupported && lifecycleSupported && scenario is not null)
        {
            foreach (AssertionDefinition assertion in scenario.Assertions)
            {
                string status;
                if (assertion.ReviewStatus != "OWNER_APPROVED" ||
                    !(assertion.LifecycleStatus is "SUPPORTED" or "MAINTENANCE_ONLY"))
                {
                    status = "NEEDS_REVIEW";
                }
                else
                {
                    status = EvaluateAssertion(assertion, observationDocument.Events, statuses) ? "PASS" : "FAIL";
                }

                string messageCode = status switch
                {
                    "PASS" => assertion.MessageCodePassed,
                    "FAIL" => assertion.MessageCodeFailed,
                    _ => "KSC.RULE.NEEDS_REVIEW"
                };

                statuses[assertion.AssertionId] = status;
                assertionResults.Add(new AssertionResult
                {
                    AssertionId = assertion.AssertionId,
                    RuleId = assertion.RuleId,
                    Classification = assertion.Classification,
                    Status = status,
                    MessageCode = messageCode
                });

                findings.Add(new Finding
                {
                    FindingId = CreateFindingId(pack.Digest, scenario.ScenarioId, assertion.AssertionId, status, messageCode),
                    AssertionId = assertion.AssertionId,
                    RuleId = assertion.RuleId,
                    Classification = assertion.Classification,
                    ScenarioId = scenario.ScenarioId,
                    Status = status,
                    MessageCode = messageCode,
                    SafeMessage = MessageCatalog.Get(messageCode),
                    SourceIds = assertion.SourceIds.OrderBy(static item => item, StringComparer.Ordinal).ToList()
                });
            }
        }

        int passed = assertionResults.Count(static item => item.Status == "PASS");
        int failed = assertionResults.Count(static item => item.Status == "FAIL");
        int needsReview = assertionResults.Count(static item => item.Status == "NEEDS_REVIEW");

        string overallResult = !profileSupported || !lifecycleSupported || scenario is null
            ? "UNSUPPORTED"
            : stale
                ? "STALE"
                : manifest.VerificationStatus == "UNVERIFIED"
                    ? "UNVERIFIED"
                    : needsReview > 0
                        ? "NEEDS_REVIEW"
                        : failed > 0
                            ? "FAIL"
                            : "PASS";

        var scenarioResults = new List<ScenarioResult>();
        if (scenario is not null)
        {
            scenarioResults.Add(new ScenarioResult
            {
                ScenarioId = scenario.ScenarioId,
                Family = scenario.Family,
                Status = overallResult,
                AssertionResults = assertionResults.OrderBy(static item => item.AssertionId, StringComparer.Ordinal).ToList()
            });
        }

        return new ScenarioCheckReport
        {
            ReportSchemaVersion = "1.0",
            EngineVersion = engineVersion,
            EvaluatedAt = evaluatedAt,
            OverallResult = overallResult,
            ObservationSchemaVersion = observationDocument.SchemaVersion,
            ObservationDigest = observation.Digest,
            PackId = manifest.PackId,
            PackVersion = manifest.PackVersion,
            PackDigest = pack.Digest,
            ProfileId = observationDocument.ProfileId,
            SourceSnapshot = manifest.SourceSnapshot,
            LastVerified = manifest.LastVerified,
            ReviewDueAt = manifest.ReviewDueAt,
            Stale = stale,
            LifecycleStatus = manifest.LifecycleStatus,
            VerificationStatus = manifest.VerificationStatus,
            ScenarioResults = scenarioResults.OrderBy(static item => item.ScenarioId, StringComparer.Ordinal).ToList(),
            Findings = findings
                .OrderBy(static item => item.ScenarioId, StringComparer.Ordinal)
                .ThenBy(static item => item.AssertionId, StringComparer.Ordinal)
                .ThenBy(static item => item.Status, StringComparer.Ordinal)
                .ThenBy(static item => item.MessageCode, StringComparer.Ordinal)
                .ToList(),
            Summary = new ReportSummary
            {
                ScenariosTotal = scenario is null ? 0 : 1,
                AssertionsTotal = assertionResults.Count,
                Passed = passed,
                Failed = failed,
                NeedsReview = needsReview,
                Unsupported = overallResult == "UNSUPPORTED" ? 1 : 0
            }
        };
    }

    private static bool EvaluateAssertion(
        AssertionDefinition assertion,
        IReadOnlyCollection<ObservationEvent> events,
        IReadOnlyDictionary<string, string> statuses)
    {
        return assertion.Operator switch
        {
            "event-present" => Select(assertion.Selector!, events) is not null,
            "field-equals" => EvaluateFieldEquals(assertion, events),
            "sequence-before" => EvaluateSequenceBefore(assertion, events),
            "timestamp-before-or-equal" => EvaluateTimestamp(assertion, events),
            "logical-all" => assertion.AssertionRefs!.All(reference => statuses.TryGetValue(reference, out string? status) && status == "PASS"),
            _ => false
        };
    }

    private static bool EvaluateFieldEquals(AssertionDefinition assertion, IReadOnlyCollection<ObservationEvent> events)
    {
        ObservationEvent? item = Select(assertion.Selector!, events);
        if (item is null || assertion.Expected is null)
        {
            return false;
        }

        JsonElement expected = assertion.Expected.Value;
        return assertion.Field switch
        {
            "occurredAt" => expected.ValueKind == JsonValueKind.String && string.Equals(item.OccurredAt, expected.GetString(), StringComparison.Ordinal),
            "sequence" => expected.ValueKind == JsonValueKind.Number && expected.TryGetInt32(out int sequence) && item.Sequence == sequence,
            "reference" => expected.ValueKind == JsonValueKind.String && string.Equals(item.Reference, expected.GetString(), StringComparison.Ordinal),
            "issueDate" => expected.ValueKind == JsonValueKind.String && string.Equals(item.IssueDate, expected.GetString(), StringComparison.Ordinal),
            "offlineMode" => expected.ValueKind is JsonValueKind.True or JsonValueKind.False && item.OfflineMode == expected.GetBoolean(),
            "attempt" => expected.ValueKind == JsonValueKind.Number && expected.TryGetInt32(out int attempt) && item.Attempt == attempt,
            _ => false
        };
    }

    private static bool EvaluateSequenceBefore(AssertionDefinition assertion, IReadOnlyCollection<ObservationEvent> events)
    {
        ObservationEvent? left = Select(assertion.Selector!, events);
        ObservationEvent? right = Select(assertion.RightSelector!, events);
        return left is not null && right is not null && left.Sequence < right.Sequence;
    }

    private static bool EvaluateTimestamp(AssertionDefinition assertion, IReadOnlyCollection<ObservationEvent> events)
    {
        ObservationEvent? left = Select(assertion.Selector!, events);
        if (left is null || assertion.RightTimestamp is null)
        {
            return false;
        }

        DateTimeOffset leftInstant = ContractValidator.ParseUtc(left.OccurredAt, "KSC.INPUT.INVALID");
        DateTimeOffset rightInstant = ContractValidator.ParseUtc(assertion.RightTimestamp, "KSC.PACK.INVALID");
        return leftInstant <= rightInstant;
    }

    private static ObservationEvent? Select(EventSelector selector, IReadOnlyCollection<ObservationEvent> events)
    {
        IEnumerable<ObservationEvent> selected = events
            .Where(item => string.Equals(item.Kind, selector.Kind, StringComparison.Ordinal) &&
                           (selector.Reference is null || string.Equals(item.Reference, selector.Reference, StringComparison.Ordinal)))
            .OrderBy(static item => item.Sequence)
            .ThenBy(static item => item.Id, StringComparer.Ordinal);

        return selector.Occurrence == "LAST" ? selected.LastOrDefault() : selected.FirstOrDefault();
    }

    private static string CreateFindingId(string packDigest, string scenarioId, string assertionId, string status, string messageCode)
    {
        string material = string.Join("\n", new[] { packDigest, scenarioId, assertionId, status, messageCode });
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
        return "KSC-" + hash[..20];
    }
}
