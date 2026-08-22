using System.Text;
using System.Text.Json;
using XtcSystems.KsefScenarioCheck.Core;
using XtcSystems.KsefScenarioCheck.Core.Contracts;
using XtcSystems.KsefScenarioCheck.Core.Reporting;

namespace XtcSystems.KsefScenarioCheck.Cli;

public static class CliApplication
{
    public const string EngineVersion = "0.1.0-alpha.1";

    public static async Task<int> RunAsync(
        string[] args,
        TextWriter standardOutput,
        TextWriter standardError,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (args.Length == 0)
            {
                await standardError.WriteLineAsync("KSC.CLI.INVALID: A command is required.").ConfigureAwait(false);
                return 2;
            }

            return args[0] switch
            {
                "run" => await RunEvaluationAsync(args[1..], standardOutput, standardError, cancellationToken).ConfigureAwait(false),
                "list" => await ListAsync(args[1..], standardOutput, cancellationToken).ConfigureAwait(false),
                "pack" => await PackAsync(args[1..], standardOutput, cancellationToken).ConfigureAwait(false),
                "version" => await VersionAsync(args[1..], standardOutput).ConfigureAwait(false),
                _ => throw new ContractException("KSC.CLI.INVALID", "The requested command is not supported.")
            };
        }
        catch (ContractException exception)
        {
            await standardError.WriteLineAsync($"{exception.Code}: {exception.SafeMessage}").ConfigureAwait(false);
            return exception.Code.StartsWith("KSC.INTERNAL", StringComparison.Ordinal) ? 3 : 2;
        }
        catch (OperationCanceledException)
        {
            await standardError.WriteLineAsync("KSC.INTERNAL.CANCELLED: The operation was cancelled.").ConfigureAwait(false);
            return 3;
        }
        catch
        {
            await standardError.WriteLineAsync("KSC.INTERNAL.ERROR: The operation failed without exposing input details.").ConfigureAwait(false);
            return 3;
        }
    }

    private static async Task<int> RunEvaluationAsync(
        string[] args,
        TextWriter standardOutput,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> options = OptionParser.Parse(args, "--observation", "--pack", "--as-of", "--output");
        string observationPath = OptionParser.Require(options, "--observation");
        string evaluatedAt = OptionParser.Require(options, "--as-of");
        string output = OptionParser.Require(options, "--output");
        string packPath = PackLocator.Resolve(options.TryGetValue("--pack", out string? packValue) ? packValue : "baseline-pl");

        _ = ContractValidator.ParseUtc(evaluatedAt, "KSC.INPUT.INVALID");
        byte[] observationBytes = await BoundedFile.ReadAllBytesAsync(
            observationPath,
            ContractValidator.MaximumObservationBytes,
            "KSC.INPUT.INVALID",
            cancellationToken).ConfigureAwait(false);
        byte[] packBytes = await BoundedFile.ReadAllBytesAsync(
            packPath,
            ContractValidator.MaximumPackBytes,
            "KSC.PACK.INVALID",
            cancellationToken).ConfigureAwait(false);

        var engine = new ScenarioCheckEngine();
        ValidatedObservation observation = engine.ValidateObservation(observationBytes);
        ValidatedPack pack = engine.ValidatePack(packBytes);
        ScenarioCheckReport report = engine.Evaluate(observation, pack, evaluatedAt, EngineVersion);
        string json = ReportSerializer.ToJson(report);

        if (output == "-")
        {
            await standardOutput.WriteAsync(json).ConfigureAwait(false);
        }
        else
        {
            EnsureDistinctPaths(observationPath, packPath, output);
            await WriteNewFileAsync(output, json, cancellationToken).ConfigureAwait(false);
            await standardOutput.WriteAsync(ReportSerializer.ToText(report)).ConfigureAwait(false);
        }

        if (report.OverallResult is "STALE" or "UNVERIFIED" or "NEEDS_REVIEW" or "UNSUPPORTED")
        {
            await standardError.WriteLineAsync("KSC.SAFETY.STATE: The evaluation completed in a non-success safety state.").ConfigureAwait(false);
        }

        return report.OverallResult switch
        {
            "PASS" => 0,
            "FAIL" => 1,
            "STALE" or "UNVERIFIED" or "NEEDS_REVIEW" or "UNSUPPORTED" => 4,
            _ => 2
        };
    }

    private static async Task<int> ListAsync(string[] args, TextWriter standardOutput, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> options = OptionParser.Parse(args, "--pack", "--format");
        string packPath = PackLocator.Resolve(options.TryGetValue("--pack", out string? packValue) ? packValue : "baseline-pl");
        string format = options.TryGetValue("--format", out string? formatValue) ? formatValue : "text";
        byte[] packBytes = await BoundedFile.ReadAllBytesAsync(packPath, ContractValidator.MaximumPackBytes, "KSC.PACK.INVALID", cancellationToken).ConfigureAwait(false);
        ValidatedPack pack = new ScenarioCheckEngine().ValidatePack(packBytes);

        if (format == "json")
        {
            string json = JsonSerializer.Serialize(
                pack.Document.Scenarios.Select(static scenario => new
                {
                    scenarioId = scenario.ScenarioId,
                    family = scenario.Family,
                    profileId = scenario.ProfileId
                }),
                new JsonSerializerOptions { WriteIndented = true });
            await standardOutput.WriteLineAsync(json).ConfigureAwait(false);
        }
        else if (format == "text")
        {
            foreach (ScenarioDefinition scenario in pack.Document.Scenarios.OrderBy(static item => item.ScenarioId, StringComparer.Ordinal))
            {
                await standardOutput.WriteLineAsync($"{scenario.ScenarioId}\t{scenario.Family}\t{scenario.ProfileId}").ConfigureAwait(false);
            }
        }
        else
        {
            throw new ContractException("KSC.CLI.INVALID", "The requested output format is not supported.");
        }

        return 0;
    }

    private static async Task<int> PackAsync(string[] args, TextWriter standardOutput, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
        {
            throw new ContractException("KSC.CLI.INVALID", "A pack subcommand is required.");
        }

        return args[0] switch
        {
            "validate" => await ValidatePackAsync(args[1..], standardOutput, cancellationToken).ConfigureAwait(false),
            "inspect" => await InspectPackAsync(args[1..], standardOutput, cancellationToken).ConfigureAwait(false),
            _ => throw new ContractException("KSC.CLI.INVALID", "The requested pack subcommand is not supported.")
        };
    }

    private static async Task<int> ValidatePackAsync(string[] args, TextWriter standardOutput, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> options = OptionParser.Parse(args, "--pack", "--format");
        string packPath = PackLocator.Resolve(OptionParser.Require(options, "--pack"));
        string format = options.TryGetValue("--format", out string? formatValue) ? formatValue : "text";
        byte[] packBytes = await BoundedFile.ReadAllBytesAsync(packPath, ContractValidator.MaximumPackBytes, "KSC.PACK.INVALID", cancellationToken).ConfigureAwait(false);
        ValidatedPack pack = new ScenarioCheckEngine().ValidatePack(packBytes);

        if (format == "json")
        {
            string json = JsonSerializer.Serialize(new
            {
                valid = true,
                packId = pack.Document.Manifest.PackId,
                packVersion = pack.Document.Manifest.PackVersion,
                packDigest = pack.Digest
            }, new JsonSerializerOptions { WriteIndented = true });
            await standardOutput.WriteLineAsync(json).ConfigureAwait(false);
        }
        else if (format == "text")
        {
            await standardOutput.WriteLineAsync($"VALID\t{pack.Document.Manifest.PackId}\t{pack.Document.Manifest.PackVersion}\t{pack.Digest}").ConfigureAwait(false);
        }
        else
        {
            throw new ContractException("KSC.CLI.INVALID", "The requested output format is not supported.");
        }

        return 0;
    }

    private static async Task<int> InspectPackAsync(string[] args, TextWriter standardOutput, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> options = OptionParser.Parse(args, "--pack", "--as-of", "--format");
        string packPath = PackLocator.Resolve(OptionParser.Require(options, "--pack"));
        string evaluatedAt = OptionParser.Require(options, "--as-of");
        string format = options.TryGetValue("--format", out string? formatValue) ? formatValue : "text";
        DateTimeOffset asOf = ContractValidator.ParseUtc(evaluatedAt, "KSC.INPUT.INVALID");
        byte[] packBytes = await BoundedFile.ReadAllBytesAsync(packPath, ContractValidator.MaximumPackBytes, "KSC.PACK.INVALID", cancellationToken).ConfigureAwait(false);
        ValidatedPack pack = new ScenarioCheckEngine().ValidatePack(packBytes);
        PackManifest manifest = pack.Document.Manifest;
        bool stale = asOf > ContractValidator.ParseUtc(manifest.ReviewDueAt, "KSC.PACK.INVALID");

        if (format == "json")
        {
            string json = JsonSerializer.Serialize(new
            {
                packId = manifest.PackId,
                packVersion = manifest.PackVersion,
                packDigest = pack.Digest,
                sourceSnapshot = manifest.SourceSnapshot,
                lastVerified = manifest.LastVerified,
                reviewDueAt = manifest.ReviewDueAt,
                reviewer = manifest.Reviewer,
                stale,
                lifecycleStatus = manifest.LifecycleStatus,
                verificationStatus = manifest.VerificationStatus
            }, new JsonSerializerOptions { WriteIndented = true });
            await standardOutput.WriteLineAsync(json).ConfigureAwait(false);
        }
        else if (format == "text")
        {
            await standardOutput.WriteLineAsync($"Pack: {manifest.PackId} {manifest.PackVersion}").ConfigureAwait(false);
            await standardOutput.WriteLineAsync($"Digest: {pack.Digest}").ConfigureAwait(false);
            await standardOutput.WriteLineAsync($"Source: {manifest.SourceSnapshot}").ConfigureAwait(false);
            await standardOutput.WriteLineAsync($"Last verified: {manifest.LastVerified}").ConfigureAwait(false);
            await standardOutput.WriteLineAsync($"Review due: {manifest.ReviewDueAt}").ConfigureAwait(false);
            await standardOutput.WriteLineAsync($"Stale: {stale.ToString().ToLowerInvariant()}").ConfigureAwait(false);
        }
        else
        {
            throw new ContractException("KSC.CLI.INVALID", "The requested output format is not supported.");
        }

        return stale ? 4 : 0;
    }

    private static async Task<int> VersionAsync(string[] args, TextWriter standardOutput)
    {
        IReadOnlyDictionary<string, string> options = OptionParser.Parse(args, "--format");
        string format = options.TryGetValue("--format", out string? formatValue) ? formatValue : "text";

        if (format == "json")
        {
            string json = JsonSerializer.Serialize(new
            {
                product = "KSeF ScenarioCheck",
                version = EngineVersion,
                reportSchemaVersion = "1.0"
            }, new JsonSerializerOptions { WriteIndented = true });
            await standardOutput.WriteLineAsync(json).ConfigureAwait(false);
        }
        else if (format == "text")
        {
            await standardOutput.WriteLineAsync($"KSeF ScenarioCheck {EngineVersion}").ConfigureAwait(false);
        }
        else
        {
            throw new ContractException("KSC.CLI.INVALID", "The requested output format is not supported.");
        }

        return 0;
    }

    private static void EnsureDistinctPaths(string observationPath, string packPath, string outputPath)
    {
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        string observation = Path.GetFullPath(observationPath);
        string pack = Path.GetFullPath(packPath);
        string output = Path.GetFullPath(outputPath);

        if (string.Equals(output, observation, comparison) || string.Equals(output, pack, comparison))
        {
            throw new ContractException("KSC.OUTPUT.INVALID", "The output path must differ from the input and pack paths.");
        }
    }

    private static async Task WriteNewFileAsync(string outputPath, string content, CancellationToken cancellationToken)
    {
        try
        {
            string fullPath = Path.GetFullPath(outputPath);
            string? directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                throw new ContractException("KSC.OUTPUT.INVALID", "The output directory does not exist.");
            }

            await using var stream = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16_384,
                FileOptions.Asynchronous | FileOptions.WriteThrough);
            byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content);
            await stream.WriteAsync(bytes.AsMemory(), cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ContractException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new ContractException("KSC.OUTPUT.INVALID", "The output file could not be created safely.");
        }
    }
}
