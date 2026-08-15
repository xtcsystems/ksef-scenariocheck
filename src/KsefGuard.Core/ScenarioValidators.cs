using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace KsefGuard;

internal interface IScenarioValidator
{
    string Type { get; }
    Task<IReadOnlyList<Finding>> ValidateAsync(string packRoot, ScenarioDefinition scenario, CancellationToken cancellationToken);
}

internal abstract class ScenarioValidatorBase : IScenarioValidator
{
    public abstract string Type { get; }

    public async Task<IReadOnlyList<Finding>> ValidateAsync(string packRoot, ScenarioDefinition scenario, CancellationToken cancellationToken)
    {
        using var fixture = await ReadJsonAsync(packRoot, scenario.Fixture, cancellationToken).ConfigureAwait(false);
        using var expected = await ReadJsonAsync(packRoot, scenario.Expected, cancellationToken).ConfigureAwait(false);
        return Validate(packRoot, scenario, fixture, expected);
    }

    protected abstract IReadOnlyList<Finding> Validate(string packRoot, ScenarioDefinition scenario, JsonDocument fixture, JsonDocument expected);

    protected static Finding Pass(string code, ScenarioDefinition scenario, string en, string pl, string? path = null)
        => new(code, CheckStatus.Pass, scenario.Id, en, pl, path);

    protected static Finding Fail(string code, ScenarioDefinition scenario, string en, string pl, string? path = null, string? detail = null)
        => new(code, CheckStatus.Fail, scenario.Id, en, pl, path, detail);

    protected static Finding NeedsReview(string code, ScenarioDefinition scenario, string en, string pl, string? path = null, string? detail = null)
        => new(code, CheckStatus.NeedsReview, scenario.Id, en, pl, path, detail);

    protected static string GetRequiredString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new PackValidationException($"Required string property is missing: {property}");
        }

        return value.GetString()!;
    }

    protected static int GetRequiredInt(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || !value.TryGetInt32(out var result))
        {
            throw new PackValidationException($"Required integer property is missing: {property}");
        }

        return result;
    }

    protected static IReadOnlyList<string> GetStringArray(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            throw new PackValidationException($"Required array property is missing: {property}");
        }

        return value.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
    }

    private static async Task<JsonDocument> ReadJsonAsync(string packRoot, string relativePath, CancellationToken cancellationToken)
    {
        var path = Path.Combine(packRoot, relativePath);
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            throw new PackValidationException($"Invalid JSON in {relativePath}.", exception);
        }
    }
}

internal sealed class CertificateMetadataValidator : ScenarioValidatorBase
{
    public override string Type => "certificate-metadata";

    protected override IReadOnlyList<Finding> Validate(string packRoot, ScenarioDefinition scenario, JsonDocument fixture, JsonDocument expected)
    {
        var findings = new List<Finding>();
        var certificateBase64 = GetRequiredString(fixture.RootElement, "certificateDerBase64");
        var relativePath = scenario.Fixture;

        X509Certificate2 certificate;
        try
        {
            certificate = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(certificateBase64));
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            return [Fail("KG-CERT-001", scenario, "Certificate data could not be parsed.", "Nie można odczytać danych certyfikatu.", relativePath)];
        }

        using (certificate)
        {
            findings.Add(Pass("KG-CERT-001", scenario, "Certificate data is parseable.", "Dane certyfikatu są możliwe do odczytania.", relativePath));

            var subjectContains = GetRequiredString(expected.RootElement, "subjectContains");
            findings.Add(certificate.Subject.Contains(subjectContains, StringComparison.OrdinalIgnoreCase)
                ? Pass("KG-CERT-002", scenario, "Certificate subject matches the fixture expectation.", "Podmiot certyfikatu odpowiada oczekiwaniu fixture.", relativePath)
                : Fail("KG-CERT-002", scenario, "Certificate subject does not match the fixture expectation.", "Podmiot certyfikatu nie odpowiada oczekiwaniu fixture.", relativePath));

            var issuerContains = GetRequiredString(expected.RootElement, "issuerContains");
            findings.Add(certificate.Issuer.Contains(issuerContains, StringComparison.OrdinalIgnoreCase)
                ? Pass("KG-CERT-003", scenario, "Certificate issuer matches the fixture expectation.", "Wystawca certyfikatu odpowiada oczekiwaniu fixture.", relativePath)
                : Fail("KG-CERT-003", scenario, "Certificate issuer does not match the fixture expectation.", "Wystawca certyfikatu nie odpowiada oczekiwaniu fixture.", relativePath));

            var validAt = DateTimeOffset.Parse(GetRequiredString(expected.RootElement, "validAt"), System.Globalization.CultureInfo.InvariantCulture);
            var notBefore = new DateTimeOffset(certificate.NotBefore).ToUniversalTime();
            var notAfter = new DateTimeOffset(certificate.NotAfter).ToUniversalTime();
            var valid = validAt >= notBefore && validAt <= notAfter;
            findings.Add(valid
                ? Pass("KG-CERT-004", scenario, "Certificate covers the pack-declared evaluation time.", "Certyfikat obejmuje czas oceny zadeklarowany w pakiecie.", relativePath)
                : Fail("KG-CERT-004", scenario, "Certificate does not cover the pack-declared evaluation time.", "Certyfikat nie obejmuje czasu oceny zadeklarowanego w pakiecie.", relativePath));

            var expectedThumbprint = NormalizeHex(GetRequiredString(expected.RootElement, "sha256Thumbprint"));
            var actualThumbprint = NormalizeHex(certificate.GetCertHashString(HashAlgorithmName.SHA256));
            findings.Add(string.Equals(actualThumbprint, expectedThumbprint, StringComparison.Ordinal)
                ? Pass("KG-CERT-005", scenario, "Certificate SHA-256 thumbprint matches.", "Odcisk SHA-256 certyfikatu jest zgodny.", relativePath)
                : Fail("KG-CERT-005", scenario, "Certificate SHA-256 thumbprint does not match.", "Odcisk SHA-256 certyfikatu nie jest zgodny.", relativePath));
        }

        return findings;
    }

    private static string NormalizeHex(string value) => value.Replace(":", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
}

internal sealed class QrVerificationLinkValidator : ScenarioValidatorBase
{
    public override string Type => "qr-verification-link";

    protected override IReadOnlyList<Finding> Validate(string packRoot, ScenarioDefinition scenario, JsonDocument fixture, JsonDocument expected)
    {
        var source = GetRequiredString(fixture.RootElement, "hashSourceUtf8");
        var declaredHash = GetRequiredString(fixture.RootElement, "declaredHash");
        var declaredLink = GetRequiredString(fixture.RootElement, "declaredLink");
        var linkPrefix = GetRequiredString(expected.RootElement, "linkPrefix");

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        var calculatedHash = Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var expectedLink = linkPrefix + calculatedHash;

        return
        [
            string.Equals(calculatedHash, declaredHash, StringComparison.Ordinal)
                ? Pass("KG-QR-001", scenario, "Declared synthetic hash matches the calculated SHA-256 value.", "Zadeklarowany syntetyczny skrót odpowiada obliczonej wartości SHA-256.", scenario.Fixture)
                : Fail("KG-QR-001", scenario, "Declared synthetic hash does not match the calculated SHA-256 value.", "Zadeklarowany syntetyczny skrót nie odpowiada obliczonej wartości SHA-256.", scenario.Fixture),
            string.Equals(expectedLink, declaredLink, StringComparison.Ordinal)
                ? Pass("KG-QR-002", scenario, "Declared verification link matches the explicit pack rule.", "Zadeklarowany link weryfikacyjny odpowiada jawnej regule pakietu.", scenario.Fixture)
                : Fail("KG-QR-002", scenario, "Declared verification link does not match the explicit pack rule.", "Zadeklarowany link weryfikacyjny nie odpowiada jawnej regule pakietu.", scenario.Fixture)
        ];
    }
}

internal sealed class OfflineTimelineValidator : ScenarioValidatorBase
{
    public override string Type => "offline24-timeline";

    protected override IReadOnlyList<Finding> Validate(string packRoot, ScenarioDefinition scenario, JsonDocument fixture, JsonDocument expected)
    {
        var events = fixture.RootElement.GetProperty("events").EnumerateArray()
            .Select(item => new TimelineEvent(
                GetRequiredString(item, "type"),
                DateTimeOffset.Parse(GetRequiredString(item, "at"), System.Globalization.CultureInfo.InvariantCulture)))
            .ToArray();
        var required = GetStringArray(expected.RootElement, "requiredSequence");
        var maxElapsedMinutes = GetRequiredInt(expected.RootElement, "maxElapsedMinutes");

        var actualTypes = events.Select(item => item.Type).ToArray();
        var sequenceMatches = actualTypes.SequenceEqual(required, StringComparer.Ordinal);
        var chronological = events.Zip(events.Skip(1), (left, right) => left.At <= right.At).All(value => value);
        var elapsed = events.Length >= 2 ? events[^1].At - events[0].At : TimeSpan.Zero;
        var withinWindow = elapsed <= TimeSpan.FromMinutes(maxElapsedMinutes);

        return
        [
            sequenceMatches
                ? Pass("KG-OFFLINE-001", scenario, "Event sequence matches the explicit scenario-pack expectation.", "Sekwencja zdarzeń odpowiada jawnemu oczekiwaniu pakietu scenariuszy.", scenario.Fixture)
                : Fail("KG-OFFLINE-001", scenario, "Event sequence does not match the explicit scenario-pack expectation.", "Sekwencja zdarzeń nie odpowiada jawnemu oczekiwaniu pakietu scenariuszy.", scenario.Fixture, $"event-count={actualTypes.Length}"),
            chronological
                ? Pass("KG-OFFLINE-002", scenario, "Event timestamps are chronological.", "Znaczniki czasu zdarzeń są chronologiczne.", scenario.Fixture)
                : Fail("KG-OFFLINE-002", scenario, "Event timestamps are not chronological.", "Znaczniki czasu zdarzeń nie są chronologiczne.", scenario.Fixture),
            withinWindow
                ? Pass("KG-OFFLINE-003", scenario, "Elapsed time is inside the pack-declared window.", "Czas mieści się w oknie zadeklarowanym w pakiecie.", scenario.Fixture)
                : Fail("KG-OFFLINE-003", scenario, "Elapsed time exceeds the pack-declared window.", "Czas przekracza okno zadeklarowane w pakiecie.", scenario.Fixture, $"elapsed-minutes={elapsed.TotalMinutes:F0}")
        ];
    }

    private sealed record TimelineEvent(string Type, DateTimeOffset At);
}

internal sealed class RetryRecoveryValidator : ScenarioValidatorBase
{
    public override string Type => "retry-recovery-sequence";

    protected override IReadOnlyList<Finding> Validate(string packRoot, ScenarioDefinition scenario, JsonDocument fixture, JsonDocument expected)
    {
        var outcomes = GetStringArray(fixture.RootElement, "outcomes");
        var maxAttempts = GetRequiredInt(expected.RootElement, "maxAttempts");
        var requiredTerminal = GetRequiredString(expected.RootElement, "requiredTerminalOutcome");
        var allowedIntermediate = GetStringArray(expected.RootElement, "allowedIntermediateOutcomes");

        var attemptsWithinLimit = outcomes.Count <= maxAttempts;
        var hasTerminal = outcomes.Count > 0 && string.Equals(outcomes[^1], requiredTerminal, StringComparison.Ordinal);
        var intermediateAllowed = outcomes.Take(Math.Max(0, outcomes.Count - 1)).All(value => allowedIntermediate.Contains(value, StringComparer.Ordinal));

        return
        [
            attemptsWithinLimit
                ? Pass("KG-RETRY-001", scenario, "Retry sequence is inside the declared attempt limit.", "Sekwencja prób mieści się w zadeklarowanym limicie.", scenario.Fixture)
                : Fail("KG-RETRY-001", scenario, "Retry sequence exceeds the declared attempt limit.", "Sekwencja prób przekracza zadeklarowany limit.", scenario.Fixture, $"attempt-count={outcomes.Count}"),
            intermediateAllowed
                ? Pass("KG-RETRY-002", scenario, "Intermediate outcomes are allowed by the pack.", "Wyniki pośrednie są dozwolone przez pakiet.", scenario.Fixture)
                : Fail("KG-RETRY-002", scenario, "One or more intermediate outcomes are not allowed by the pack.", "Co najmniej jeden wynik pośredni nie jest dozwolony przez pakiet.", scenario.Fixture),
            hasTerminal
                ? Pass("KG-RETRY-003", scenario, "Sequence reaches the required terminal outcome.", "Sekwencja osiąga wymagany wynik końcowy.", scenario.Fixture)
                : Fail("KG-RETRY-003", scenario, "Sequence does not reach the required terminal outcome.", "Sekwencja nie osiąga wymaganego wyniku końcowego.", scenario.Fixture)
        ];
    }
}

internal sealed class UnresolvedStatusValidator : ScenarioValidatorBase
{
    public override string Type => "unresolved-status";

    protected override IReadOnlyList<Finding> Validate(string packRoot, ScenarioDefinition scenario, JsonDocument fixture, JsonDocument expected)
    {
        var terminalStatuses = GetStringArray(expected.RootElement, "terminalStatuses");
        var records = fixture.RootElement.GetProperty("records").EnumerateArray()
            .Select(item => new StatusRecord(GetRequiredString(item, "reference"), GetRequiredString(item, "status")))
            .ToArray();

        var duplicateReferences = records.GroupBy(item => item.Reference, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        var contradictory = records.GroupBy(item => item.Reference, StringComparer.Ordinal)
            .Where(group => group.Select(item => item.Status).Distinct(StringComparer.Ordinal).Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        var unresolved = records.Where(item => !terminalStatuses.Contains(item.Status, StringComparer.Ordinal)).Select(item => item.Reference).Distinct(StringComparer.Ordinal).ToArray();

        return
        [
            duplicateReferences.Length == 0
                ? Pass("KG-STATUS-001", scenario, "No duplicate references were found.", "Nie znaleziono zduplikowanych referencji.", scenario.Fixture)
                : Fail("KG-STATUS-001", scenario, "Duplicate references were found.", "Znaleziono zduplikowane referencje.", scenario.Fixture, $"duplicate-reference-count={duplicateReferences.Length}"),
            contradictory.Length == 0
                ? Pass("KG-STATUS-002", scenario, "No contradictory statuses were found.", "Nie znaleziono sprzecznych statusów.", scenario.Fixture)
                : Fail("KG-STATUS-002", scenario, "Contradictory statuses were found for the same reference.", "Znaleziono sprzeczne statusy dla tej samej referencji.", scenario.Fixture, $"contradictory-reference-count={contradictory.Length}"),
            unresolved.Length == 0
                ? Pass("KG-STATUS-003", scenario, "Every record has a terminal status declared by the pack.", "Każdy rekord ma status końcowy zadeklarowany w pakiecie.", scenario.Fixture)
                : NeedsReview("KG-STATUS-003", scenario, "One or more records remain unresolved under the pack rules.", "Co najmniej jeden rekord pozostaje nierozstrzygnięty według reguł pakietu.", scenario.Fixture, $"unresolved-reference-count={unresolved.Length}")
        ];
    }

    private sealed record StatusRecord(string Reference, string Status);
}

internal static class ScenarioValidatorFactory
{
    private static readonly IReadOnlyDictionary<string, IScenarioValidator> Validators = new IScenarioValidator[]
    {
        new CertificateMetadataValidator(),
        new QrVerificationLinkValidator(),
        new OfflineTimelineValidator(),
        new RetryRecoveryValidator(),
        new UnresolvedStatusValidator()
    }.ToDictionary(item => item.Type, StringComparer.Ordinal);

    public static IScenarioValidator Get(string type) => Validators.TryGetValue(type, out var validator)
        ? validator
        : throw new PackValidationException($"Unsupported scenario type: {type}");
}
