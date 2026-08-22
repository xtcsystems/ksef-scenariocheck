using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using XtcSystems.KsefScenarioCheck.Core.Serialization;

namespace XtcSystems.KsefScenarioCheck.Core.Contracts;

public static class ContractValidator
{
    public const int MaximumObservationBytes = 1_048_576;
    public const int MaximumPackBytes = 2_097_152;
    public const int MaximumJsonDepth = 8;
    public const int MaximumEvents = 256;
    public const int MaximumAssertionsPerScenario = 128;
    public const int MaximumAssertionsPerPack = 512;

    private static readonly Regex ProductIdentifier = new(
        @"^[a-z][a-z0-9]*(?:[._-][a-z0-9]+)*$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex SyntheticReference = new(
        @"^syn:[A-Za-z0-9][A-Za-z0-9._-]{0,47}$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex SourceRuleId = new(
        @"^R-[A-Z]+-[0-9]{3}$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex MessageCode = new(
        @"^KSC\.[A-Z0-9_.]+$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex SemVer = new(
        @"^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
        TimeSpan.FromMilliseconds(100));

    private static readonly HashSet<string> EventKinds = new(StringComparer.Ordinal)
    {
        "invoice-issued",
        "offline-mode-declared",
        "transmission-attempt"
    };

    private static readonly HashSet<string> Operators = new(StringComparer.Ordinal)
    {
        "event-present",
        "field-equals",
        "sequence-before",
        "timestamp-before-or-equal",
        "logical-all"
    };

    private static readonly HashSet<string> LifecycleStatuses = new(StringComparer.Ordinal)
    {
        "SUPPORTED",
        "MAINTENANCE_ONLY",
        "FROZEN",
        "ARCHIVED"
    };

    public static ValidatedObservation ValidateObservation(byte[] bytes)
    {
        ObservationDocument document = StrictJson.Deserialize<ObservationDocument>(bytes, MaximumObservationBytes, "KSC.INPUT.INVALID");

        Require(document.SchemaVersion == "1.0", "KSC.INPUT.INVALID", "The observation schema version is unsupported.");
        Require(document.ProfileId.Length is > 0 and <= 128, "KSC.INPUT.INVALID", "The observation profile identifier is invalid.");
        Require(document.ScenarioFamily == "offline24-timeline", "KSC.INPUT.INVALID", "The observation scenario family is unsupported by this vertical slice.");
        ValidateProductIdentifier(document.ScenarioId, "scenarioId", "KSC.INPUT.INVALID");
        ValidateProductIdentifier(document.ObservationId, "observationId", "KSC.INPUT.INVALID");

        if (document.GeneratedAt is not null)
        {
            _ = ParseUtc(document.GeneratedAt, "KSC.INPUT.INVALID");
        }

        ValidateSourceSystem(document.SourceSystem);
        Require(document.Events.Count is >= 1 and <= MaximumEvents, "KSC.INPUT.INVALID", "The observation event count is outside the accepted range.");

        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        var sequences = new HashSet<int>();
        foreach (ObservationEvent item in document.Events)
        {
            ValidateEvent(item);
            Require(identifiers.Add(item.Id), "KSC.INPUT.INVALID", "Observation event identifiers must be unique.");
            Require(sequences.Add(item.Sequence), "KSC.INPUT.INVALID", "Observation event sequences must be unique.");
        }

        return new ValidatedObservation(document, bytes, CanonicalJson.ComputeObservationDigest(bytes));
    }

    public static ValidatedPack ValidatePack(byte[] bytes)
    {
        ScenarioPackDocument document = StrictJson.Deserialize<ScenarioPackDocument>(bytes, MaximumPackBytes, "KSC.PACK.INVALID");
        PackManifest manifest = document.Manifest;

        Require(document.SchemaVersion == "1.0" && manifest.SchemaVersion == "1.0", "KSC.PACK.INVALID", "The pack schema version is unsupported.");
        ValidateProductIdentifier(manifest.PackId, "packId", "KSC.PACK.INVALID");
        Require(IsSemVer(manifest.PackVersion) && IsSemVer(manifest.MinimumEngineVersion) && IsSemVer(manifest.MaximumEngineVersionExclusive), "KSC.PACK.INVALID", "The pack version information is invalid.");
        Require(manifest.PackDigest.Length == 71 && manifest.PackDigest.StartsWith("sha256:", StringComparison.Ordinal) && manifest.PackDigest[7..].All(static item => char.IsAsciiHexDigit(item) && !char.IsUpper(item)), "KSC.PACK.INVALID", "The pack digest is invalid.");
        Require(manifest.ProfileIds.Count is >= 1 and <= 16, "KSC.PACK.INVALID", "The pack profile list is invalid.");
        Require(manifest.ProfileIds.Distinct(StringComparer.Ordinal).Count() == manifest.ProfileIds.Count, "KSC.PACK.INVALID", "Pack profile identifiers must be unique.");
        Require(manifest.ProfileIds.All(static item => item.Length is > 0 and <= 128), "KSC.PACK.INVALID", "A pack profile identifier is invalid.");
        Require(manifest.SourceSnapshot.Length is > 0 and <= 128, "KSC.PACK.INVALID", "The pack source snapshot is invalid.");
        Require(manifest.OpenApiVersion.Length is > 0 and <= 32 && manifest.OpenApiBuild.Length is > 0 and <= 32, "KSC.PACK.INVALID", "The pack OpenAPI identity is invalid.");

        DateTimeOffset lastVerified = ParseUtc(manifest.LastVerified, "KSC.PACK.INVALID");
        DateTimeOffset reviewDueAt = ParseUtc(manifest.ReviewDueAt, "KSC.PACK.INVALID");
        Require(reviewDueAt > lastVerified, "KSC.PACK.INVALID", "The pack review due date must be later than the last verified date.");
        Require(manifest.Reviewer.Length is > 0 and <= 128, "KSC.PACK.INVALID", "The pack reviewer identity is invalid.");
        Require(LifecycleStatuses.Contains(manifest.LifecycleStatus), "KSC.PACK.INVALID", "The pack lifecycle status is invalid.");
        Require(manifest.VerificationStatus is "FIRST_PARTY_REVIEWED" or "UNVERIFIED", "KSC.PACK.INVALID", "The pack verification status is invalid.");

        string computedDigest = CanonicalJson.ComputePackDigest(bytes);
        Require(string.Equals(computedDigest, manifest.PackDigest, StringComparison.Ordinal), "KSC.PACK.DIGEST_MISMATCH", "The supplied pack digest does not match the canonical pack content.");

        Require(document.Scenarios.Count is >= 1 and <= 64, "KSC.PACK.INVALID", "The scenario count is outside the accepted range.");
        var scenarioIds = new HashSet<string>(StringComparer.Ordinal);
        int assertionTotal = 0;

        foreach (ScenarioDefinition scenario in document.Scenarios)
        {
            ValidateProductIdentifier(scenario.ScenarioId, "scenarioId", "KSC.PACK.INVALID");
            Require(scenarioIds.Add(scenario.ScenarioId), "KSC.PACK.INVALID", "Scenario identifiers must be unique.");
            Require(scenario.Family == "offline24-timeline", "KSC.PACK.INVALID", "The pack includes a scenario family outside this vertical slice.");
            Require(MessageCode.IsMatch(scenario.TitleCode), "KSC.PACK.INVALID", "The scenario title code is invalid.");
            Require(manifest.ProfileIds.Contains(scenario.ProfileId, StringComparer.Ordinal), "KSC.PACK.INVALID", "The scenario profile is not declared by the pack.");
            Require(scenario.Assertions.Count is >= 1 and <= MaximumAssertionsPerScenario, "KSC.PACK.INVALID", "The scenario assertion count is outside the accepted range.");

            assertionTotal += scenario.Assertions.Count;
            Require(assertionTotal <= MaximumAssertionsPerPack, "KSC.PACK.INVALID", "The pack contains too many assertions.");

            var previousAssertionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AssertionDefinition assertion in scenario.Assertions)
            {
                ValidateAssertion(assertion, manifest.VerificationStatus, previousAssertionIds);
                previousAssertionIds.Add(assertion.AssertionId);
            }

            foreach (string excludedRuleId in scenario.ExcludedRuleIds)
            {
                Require(SourceRuleId.IsMatch(excludedRuleId), "KSC.PACK.INVALID", "An excluded rule identifier is invalid.");
            }
        }

        return new ValidatedPack(document, bytes, computedDigest);
    }

    public static DateTimeOffset ParseUtc(string value, string errorCode)
    {
        if (string.IsNullOrEmpty(value) ||
            !value.EndsWith("Z", StringComparison.Ordinal) ||
            value.Length > 40 ||
            !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset result) ||
            result.Offset != TimeSpan.Zero)
        {
            throw new ContractException(errorCode, "A UTC RFC 3339 timestamp ending in Z is required.");
        }

        return result;
    }

    private static bool IsSemVer(string value)
        => value.Length is > 0 and <= 32 && SemVer.IsMatch(value);

    private static void ValidateSourceSystem(SourceSystemInfo? sourceSystem)
    {
        if (sourceSystem is null)
        {
            return;
        }

        Require(sourceSystem.Kind is "integration-test-harness" or "fixture-generator" or "ci-pipeline", "KSC.INPUT.INVALID", "The source system kind is invalid.");
        if (sourceSystem.Version is not null)
        {
            Require(
                sourceSystem.Version.Length is > 0 and <= 32 && sourceSystem.Version.All(static item => char.IsAsciiLetterOrDigit(item) || item is '.' or '_' or '+' or '-'),
                "KSC.INPUT.INVALID",
                "The source system version is invalid.");
        }
    }

    private static void ValidateEvent(ObservationEvent item)
    {
        ValidateProductIdentifier(item.Id, "event id", "KSC.INPUT.INVALID");
        Require(EventKinds.Contains(item.Kind), "KSC.INPUT.INVALID", "The observation contains an unsupported event kind.");
        _ = ParseUtc(item.OccurredAt, "KSC.INPUT.INVALID");
        Require(item.Sequence is >= 0 and <= 65_535, "KSC.INPUT.INVALID", "An event sequence is outside the accepted range.");
        Require(SyntheticReference.IsMatch(item.Reference), "KSC.INPUT.INVALID", "Event references must be bounded synthetic references.");

        switch (item.Kind)
        {
            case "invoice-issued":
                Require(item.IssueDate is not null && DateOnly.TryParseExact(item.IssueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _), "KSC.INPUT.INVALID", "An invoice-issued event requires a valid synthetic issue date.");
                Require(item.OfflineMode is null && item.Attempt is null, "KSC.INPUT.INVALID", "The invoice-issued event contains fields outside its closed contract.");
                break;
            case "offline-mode-declared":
                Require(item.OfflineMode is not null, "KSC.INPUT.INVALID", "An offline-mode-declared event requires offlineMode.");
                Require(item.IssueDate is null && item.Attempt is null, "KSC.INPUT.INVALID", "The offline-mode-declared event contains fields outside its closed contract.");
                break;
            case "transmission-attempt":
                Require(item.Attempt is >= 1 and <= 100, "KSC.INPUT.INVALID", "A transmission-attempt event requires a bounded attempt number.");
                Require(item.IssueDate is null && item.OfflineMode is null, "KSC.INPUT.INVALID", "The transmission-attempt event contains fields outside its closed contract.");
                break;
        }
    }

    private static void ValidateAssertion(AssertionDefinition assertion, string verificationStatus, HashSet<string> previousAssertionIds)
    {
        ValidateProductIdentifier(assertion.AssertionId, "assertionId", "KSC.PACK.INVALID");
        Require(!previousAssertionIds.Contains(assertion.AssertionId), "KSC.PACK.INVALID", "Assertion identifiers must be unique within a scenario.");
        Require(Operators.Contains(assertion.Operator), "KSC.PACK.INVALID", "The pack contains an unsupported assertion operator.");
        Require(SourceRuleId.IsMatch(assertion.RuleId), "KSC.PACK.INVALID", "An assertion rule identifier is invalid.");
        Require(assertion.Classification is "ACCEPTED_DIRECT" or "ACCEPTED_INTERPRETATION" or "SYNTHETIC_ASSUMPTION", "KSC.PACK.INVALID", "An assertion classification is invalid.");
        Require(assertion.SourceIds.Count is >= 1 and <= 8, "KSC.PACK.INVALID", "Assertion source identifiers are invalid.");
        Require(assertion.SourceIds.Distinct(StringComparer.Ordinal).Count() == assertion.SourceIds.Count, "KSC.PACK.INVALID", "Assertion source identifiers must be unique.");
        Require(assertion.SourceIds.All(static item => item.Length is > 0 and <= 64), "KSC.PACK.INVALID", "Assertion source identifiers are invalid.");
        Require(assertion.SourceVersionOrCommit.Length is > 0 and <= 128 && assertion.SourceSection.Length is > 0 and <= 128, "KSC.PACK.INVALID", "Assertion provenance is invalid.");

        _ = ParseUtc(assertion.EffectiveDate, "KSC.PACK.INVALID");
        DateTimeOffset lastVerified = ParseUtc(assertion.LastVerified, "KSC.PACK.INVALID");
        DateTimeOffset reviewDueAt = ParseUtc(assertion.ReviewDueAt, "KSC.PACK.INVALID");
        Require(reviewDueAt > lastVerified, "KSC.PACK.INVALID", "Assertion review metadata is invalid.");
        Require(assertion.ReviewStatus is "OWNER_APPROVED" or "PENDING", "KSC.PACK.INVALID", "Assertion review status is invalid.");
        Require(assertion.InterpretationNote.Length <= 256, "KSC.PACK.INVALID", "An assertion interpretation note is too long.");
        Require(LifecycleStatuses.Contains(assertion.LifecycleStatus), "KSC.PACK.INVALID", "An assertion lifecycle status is invalid.");
        Require(MessageCode.IsMatch(assertion.MessageCodePassed) && MessageCode.IsMatch(assertion.MessageCodeFailed), "KSC.PACK.INVALID", "An assertion message code is invalid.");

        if (verificationStatus == "UNVERIFIED")
        {
            Require(assertion.Classification == "SYNTHETIC_ASSUMPTION", "KSC.PACK.INVALID", "Unverified packs may execute only synthetic assumptions.");
        }

        switch (assertion.Operator)
        {
            case "event-present":
                _ = RequireSelector(assertion.Selector);
                Require(assertion.RightSelector is null && assertion.Field is null && assertion.Expected is null && assertion.RightTimestamp is null && assertion.AssertionRefs is null, "KSC.PACK.INVALID", "The event-present assertion has unsupported operands.");
                break;
            case "field-equals":
                _ = RequireSelector(assertion.Selector);
                Require(assertion.Field is "occurredAt" or "sequence" or "reference" or "issueDate" or "offlineMode" or "attempt", "KSC.PACK.INVALID", "The field-equals assertion field is invalid.");
                Require(assertion.Expected is not null, "KSC.PACK.INVALID", "The field-equals assertion requires an expected value.");
                Require(assertion.RightSelector is null && assertion.RightTimestamp is null && assertion.AssertionRefs is null, "KSC.PACK.INVALID", "The field-equals assertion has unsupported operands.");
                break;
            case "sequence-before":
                _ = RequireSelector(assertion.Selector);
                _ = RequireSelector(assertion.RightSelector);
                Require(assertion.Field is null && assertion.Expected is null && assertion.RightTimestamp is null && assertion.AssertionRefs is null, "KSC.PACK.INVALID", "The sequence-before assertion has unsupported operands.");
                break;
            case "timestamp-before-or-equal":
                _ = RequireSelector(assertion.Selector);
                string rightTimestamp = assertion.RightTimestamp ?? throw new ContractException("KSC.PACK.INVALID", "The timestamp assertion requires a pack-declared UTC timestamp.");
                _ = ParseUtc(rightTimestamp, "KSC.PACK.INVALID");
                Require(assertion.RightSelector is null && assertion.Field is null && assertion.Expected is null && assertion.AssertionRefs is null, "KSC.PACK.INVALID", "The timestamp assertion has unsupported operands.");
                break;
            case "logical-all":
                List<string> references = assertion.AssertionRefs ?? throw new ContractException("KSC.PACK.INVALID", "The logical-all assertion requires references.");
                Require(references.Count is >= 2 and <= 8, "KSC.PACK.INVALID", "The logical-all assertion requires two to eight references.");
                Require(references.Distinct(StringComparer.Ordinal).Count() == references.Count, "KSC.PACK.INVALID", "Logical assertion references must be unique.");
                Require(references.All(previousAssertionIds.Contains), "KSC.PACK.INVALID", "Logical assertions may reference only earlier assertions.");
                Require(assertion.Selector is null && assertion.RightSelector is null && assertion.Field is null && assertion.Expected is null && assertion.RightTimestamp is null, "KSC.PACK.INVALID", "The logical-all assertion has unsupported operands.");
                break;
        }
    }

    private static EventSelector RequireSelector(EventSelector? selector)
    {
        if (selector is null)
        {
            throw new ContractException("KSC.PACK.INVALID", "The assertion requires an event selector.");
        }

        Require(EventKinds.Contains(selector.Kind), "KSC.PACK.INVALID", "An assertion selector contains an unsupported event kind.");
        Require(selector.Occurrence is "FIRST" or "LAST", "KSC.PACK.INVALID", "An assertion selector occurrence is invalid.");
        if (selector.Reference is not null)
        {
            Require(SyntheticReference.IsMatch(selector.Reference), "KSC.PACK.INVALID", "An assertion selector reference is invalid.");
        }

        return selector;
    }

    private static void ValidateProductIdentifier(string value, string field, string errorCode)
    {
        Require(value.Length is > 0 and <= 64 && ProductIdentifier.IsMatch(value), errorCode, $"The {field} value is invalid.");
    }

    private static void Require(bool condition, string code, string safeMessage)
    {
        if (!condition)
        {
            throw new ContractException(code, safeMessage);
        }
    }
}
