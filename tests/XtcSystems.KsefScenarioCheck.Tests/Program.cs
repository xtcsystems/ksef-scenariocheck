using System.Globalization;
using System.Text;
using System.Text.Json;
using XtcSystems.KsefScenarioCheck.Cli;
using XtcSystems.KsefScenarioCheck.Core;
using XtcSystems.KsefScenarioCheck.Core.Contracts;
using XtcSystems.KsefScenarioCheck.Core.Reporting;

namespace XtcSystems.KsefScenarioCheck.Tests;

public static class Program
{
    private static readonly string BaseDirectory = AppContext.BaseDirectory;
    private static readonly string ObservationDirectory = Path.Combine(BaseDirectory, "examples", "observations");
    private static readonly string BaselinePackPath = Path.Combine(BaseDirectory, "packs", "baseline-pl", "pack.json");
    private static readonly string UnverifiedPackPath = Path.Combine(BaseDirectory, "Fixtures", "packs", "unverified", "pack.json");

    public static async Task<int> Main()
    {
        var tests = new (string Name, Func<Task> Execute)[]
        {
            ("pass fixture", PassFixture),
            ("negative fixtures", NegativeFixtures),
            ("safety states", SafetyStates),
            ("invalid fixtures", InvalidFixtures),
            ("duplicate property", DuplicateProperty),
            ("invalid UTF-8", InvalidUtf8),
            ("size and depth limits", SizeAndDepthLimits),
            ("determinism", Determinism),
            ("canonical observation digest", CanonicalObservationDigest),
            ("pack digest", PackDigest),
            ("CLI create-new and redaction", CliCreateNewAndRedaction),
            ("CLI JSON stdout", CliJsonStdout),
            ("CLI supporting commands", CliSupportingCommands),
            ("schema inventory", SchemaInventory),
            ("no network assembly dependency", NoNetworkAssemblyDependency),
            ("golden summary", GoldenSummary)
        };

        int failed = 0;
        foreach ((string name, Func<Task> execute) in tests)
        {
            try
            {
                await execute().ConfigureAwait(false);
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception exception)
            {
                failed++;
                Console.Error.WriteLine($"FAIL {name}: {exception.GetType().Name}: {exception.Message}");
            }
        }

        Console.WriteLine($"{tests.Length - failed}/{tests.Length} tests passed");
        return failed == 0 ? 0 : 1;
    }

    private static Task PassFixture()
    {
        ScenarioCheckReport report = Evaluate("offline24.pass.json", BaselinePackPath, "2026-09-01T00:00:00Z");
        Equal("PASS", report.OverallResult);
        Equal(7, report.Summary.Passed);
        Equal(0, report.Summary.Failed);
        Equal(0, report.Summary.NeedsReview);
        Equal(7, report.Findings.Count);
        DoesNotContain(ReportSerializer.ToJson(report), "syn:invoice-001");
        return Task.CompletedTask;
    }

    private static Task NegativeFixtures()
    {
        foreach (string fixture in new[]
        {
            "offline24.marker-missing.json",
            "offline24.marker-false.json",
            "offline24.issue-date-differs.json",
            "offline24.outside-declared-window.json"
        })
        {
            ScenarioCheckReport report = Evaluate(fixture, BaselinePackPath, "2026-09-01T00:00:00Z");
            Equal("FAIL", report.OverallResult);
            True(report.Summary.Failed > 0);
        }

        return Task.CompletedTask;
    }

    private static Task SafetyStates()
    {
        Equal("UNSUPPORTED", Evaluate("offline24.unsupported-profile.json", BaselinePackPath, "2026-09-01T00:00:00Z").OverallResult);
        Equal("STALE", Evaluate("offline24.stale-pack.json", BaselinePackPath, "2026-10-01T00:00:00Z").OverallResult);
        Equal("UNVERIFIED", Evaluate("offline24.unverified-pack.json", UnverifiedPackPath, "2026-09-01T00:00:00Z").OverallResult);
        return Task.CompletedTask;
    }

    private static Task InvalidFixtures()
    {
        foreach (string fixture in new[]
        {
            "offline24.unknown-property.json",
            "offline24.duplicate-event-id.json",
            "offline24.duplicate-sequence.json",
            "offline24.production-field.json"
        })
        {
            ThrowsContract(() => new ScenarioCheckEngine().ValidateObservation(File.ReadAllBytes(Path.Combine(ObservationDirectory, fixture))));
        }

        return Task.CompletedTask;
    }

    private static Task DuplicateProperty()
    {
        const string json = "{\"schemaVersion\":\"1.0\",\"schemaVersion\":\"1.0\",\"profileId\":\"x\",\"scenarioFamily\":\"offline24-timeline\",\"scenarioId\":\"x\",\"observationId\":\"x\",\"events\":[]}";
        ThrowsContract(() => new ScenarioCheckEngine().ValidateObservation(Encoding.UTF8.GetBytes(json)));
        return Task.CompletedTask;
    }

    private static Task InvalidUtf8()
    {
        ThrowsContract(() => new ScenarioCheckEngine().ValidateObservation(new byte[] { 0xFF, 0xFE, 0x00 }));
        return Task.CompletedTask;
    }

    private static Task SizeAndDepthLimits()
    {
        ThrowsContract(() => new ScenarioCheckEngine().ValidateObservation(new byte[ContractValidator.MaximumObservationBytes + 1]));

        string nestedValue = new string('[', 12) + "0" + new string(']', 12);
        string nested = "{\"schemaVersion\":\"1.0\",\"profileId\":\"x\",\"scenarioFamily\":\"offline24-timeline\",\"scenarioId\":\"x\",\"observationId\":\"x\",\"events\":[],\"unexpected\":" + nestedValue + "}";
        ThrowsContract(() => new ScenarioCheckEngine().ValidateObservation(Encoding.UTF8.GetBytes(nested)));
        return Task.CompletedTask;
    }

    private static Task Determinism()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            string? baseline = null;
            foreach (string cultureName in new[] { "en-US", "pl-PL", "tr-TR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
                for (int index = 0; index < 100; index++)
                {
                    string current = ReportSerializer.ToJson(Evaluate("offline24.pass.json", BaselinePackPath, "2026-09-01T00:00:00Z"));
                    baseline ??= current;
                    Equal(baseline, current);
                }
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        return Task.CompletedTask;
    }

    private static Task CanonicalObservationDigest()
    {
        byte[] original = File.ReadAllBytes(Path.Combine(ObservationDirectory, "offline24.pass.json"));
        using JsonDocument document = JsonDocument.Parse(original);
        JsonElement root = document.RootElement;

        string reordered = JsonSerializer.Serialize(new
        {
            events = root.GetProperty("events"),
            observationId = root.GetProperty("observationId").GetString(),
            scenarioId = root.GetProperty("scenarioId").GetString(),
            scenarioFamily = root.GetProperty("scenarioFamily").GetString(),
            profileId = root.GetProperty("profileId").GetString(),
            schemaVersion = root.GetProperty("schemaVersion").GetString(),
            sourceSystem = root.GetProperty("sourceSystem"),
            generatedAt = "2030-01-01T00:00:00Z"
        });

        var engine = new ScenarioCheckEngine();
        string first = engine.ValidateObservation(original).Digest;
        string second = engine.ValidateObservation(Encoding.UTF8.GetBytes(reordered)).Digest;
        Equal(first, second);
        return Task.CompletedTask;
    }

    private static Task PackDigest()
    {
        ValidatedPack pack = new ScenarioCheckEngine().ValidatePack(File.ReadAllBytes(BaselinePackPath));
        Equal("sha256:2b9ec72f822a57eea1b3be1e89796d6a51738d0cf9a80639530f6a645d9584ad", pack.Digest);
        return Task.CompletedTask;
    }

    private static async Task CliCreateNewAndRedaction()
    {
        string temporaryDirectory = Path.Combine(Path.GetTempPath(), "ksc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            string output = Path.Combine(temporaryDirectory, "report.json");
            var stdout = new StringWriter(CultureInfo.InvariantCulture);
            var stderr = new StringWriter(CultureInfo.InvariantCulture);
            int first = await CliApplication.RunAsync(new[]
            {
                "run",
                "--observation", Path.Combine(ObservationDirectory, "offline24.pass.json"),
                "--pack", Path.GetDirectoryName(BaselinePackPath)!,
                "--as-of", "2026-09-01T00:00:00Z",
                "--output", output
            }, stdout, stderr).ConfigureAwait(false);

            Equal(0, first);
            True(File.Exists(output));
            DoesNotContain(File.ReadAllText(output), "syn:invoice-001");
            Equal(string.Empty, stderr.ToString());

            int second = await CliApplication.RunAsync(new[]
            {
                "run",
                "--observation", Path.Combine(ObservationDirectory, "offline24.pass.json"),
                "--pack", Path.GetDirectoryName(BaselinePackPath)!,
                "--as-of", "2026-09-01T00:00:00Z",
                "--output", output
            }, new StringWriter(CultureInfo.InvariantCulture), stderr).ConfigureAwait(false);

            Equal(2, second);
            DoesNotContain(stderr.ToString(), temporaryDirectory);
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private static async Task CliJsonStdout()
    {
        var stdout = new StringWriter(CultureInfo.InvariantCulture);
        var stderr = new StringWriter(CultureInfo.InvariantCulture);
        int exit = await CliApplication.RunAsync(new[]
        {
            "run",
            "--observation", Path.Combine(ObservationDirectory, "offline24.pass.json"),
            "--pack", Path.GetDirectoryName(BaselinePackPath)!,
            "--as-of", "2026-09-01T00:00:00Z",
            "--output", "-"
        }, stdout, stderr).ConfigureAwait(false);

        Equal(0, exit);
        using JsonDocument document = JsonDocument.Parse(stdout.ToString());
        Equal("PASS", document.RootElement.GetProperty("overallResult").GetString());
        Equal(string.Empty, stderr.ToString());
    }

    private static async Task CliSupportingCommands()
    {
        foreach (string[] arguments in new[]
        {
            new[] { "version", "--format", "json" },
            new[] { "list", "--pack", Path.GetDirectoryName(BaselinePackPath)!, "--format", "json" },
            new[] { "pack", "validate", "--pack", Path.GetDirectoryName(BaselinePackPath)!, "--format", "json" },
            new[] { "pack", "inspect", "--pack", Path.GetDirectoryName(BaselinePackPath)!, "--as-of", "2026-09-01T00:00:00Z", "--format", "json" }
        })
        {
            var stdout = new StringWriter(CultureInfo.InvariantCulture);
            var stderr = new StringWriter(CultureInfo.InvariantCulture);
            int exit = await CliApplication.RunAsync(arguments, stdout, stderr).ConfigureAwait(false);
            Equal(0, exit);
            True(stdout.ToString().Length > 0);
            Equal(string.Empty, stderr.ToString());
        }
    }

    private static Task SchemaInventory()
    {
        foreach (string name in new[] { "observation-v1.schema.json", "scenario-pack-v1.schema.json", "report-v1.schema.json" })
        {
            string path = Path.Combine(BaseDirectory, "schemas", name);
            using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path));
            Equal("https://json-schema.org/draft/2020-12/schema", document.RootElement.GetProperty("$schema").GetString());
            True(document.RootElement.TryGetProperty("additionalProperties", out JsonElement additionalProperties) && additionalProperties.ValueKind == JsonValueKind.False);
        }

        return Task.CompletedTask;
    }

    private static Task NoNetworkAssemblyDependency()
    {
        string[] references = typeof(ScenarioCheckEngine).Assembly.GetReferencedAssemblies()
            .Concat(typeof(CliApplication).Assembly.GetReferencedAssemblies())
            .Select(static item => item.Name ?? string.Empty)
            .ToArray();
        True(!references.Contains("System.Net.Http", StringComparer.Ordinal));
        return Task.CompletedTask;
    }

    private static Task GoldenSummary()
    {
        string expected = NormalizeNewlines(File.ReadAllText(Path.Combine(BaseDirectory, "Golden", "offline24.pass.summary.txt")));
        string actual = NormalizeNewlines(ReportSerializer.ToText(Evaluate("offline24.pass.json", BaselinePackPath, "2026-09-01T00:00:00Z")));
        Equal(expected, actual);
        return Task.CompletedTask;
    }

    private static ScenarioCheckReport Evaluate(string observationName, string packPath, string asOf)
    {
        var engine = new ScenarioCheckEngine();
        ValidatedObservation observation = engine.ValidateObservation(File.ReadAllBytes(Path.Combine(ObservationDirectory, observationName)));
        ValidatedPack pack = engine.ValidatePack(File.ReadAllBytes(packPath));
        return engine.Evaluate(observation, pack, asOf, CliApplication.EngineVersion);
    }

    private static string NormalizeNewlines(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);

    private static void ThrowsContract(Action action)
    {
        try
        {
            action();
        }
        catch (ContractException)
        {
            return;
        }

        throw new InvalidOperationException("Expected a ContractException.");
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }
    }

    private static void True(bool value)
    {
        if (!value)
        {
            throw new InvalidOperationException("Expected condition to be true.");
        }
    }

    private static void DoesNotContain(string value, string fragment)
    {
        if (value.Contains(fragment, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A prohibited raw value was present in output.");
        }
    }
}
