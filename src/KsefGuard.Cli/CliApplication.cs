using System.Reflection;

namespace KsefGuard.Cli;

public static class CliApplication
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            if (args.Length == 0 || IsHelp(args[0]))
            {
                WriteHelp(output);
                return 0;
            }

            return args[0] switch
            {
                "version" => WriteVersion(output),
                "scenarios" => WriteScenarios(args, output),
                "init" => await InitAsync(args, output, cancellationToken).ConfigureAwait(false),
                "validate" => await ValidateAsync(args, output, cancellationToken).ConfigureAwait(false),
                _ => UnknownCommand(args[0], error)
            };
        }
        catch (UnsafePackException exception)
        {
            await error.WriteLineAsync($"Unsafe input refused: {exception.Message}").ConfigureAwait(false);
            return 4;
        }
        catch (PackValidationException exception)
        {
            await error.WriteLineAsync($"Invalid scenario pack: {exception.Message}").ConfigureAwait(false);
            return 2;
        }
        catch (OperationCanceledException)
        {
            await error.WriteLineAsync("Operation cancelled.").ConfigureAwait(false);
            return 3;
        }
        catch (Exception exception)
        {
            await error.WriteLineAsync($"Runtime or I/O failure: {exception.Message}").ConfigureAwait(false);
            return 3;
        }
    }

    private static int WriteVersion(TextWriter output)
    {
        var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "0.0.0-unknown";
        output.WriteLine(version);
        return 0;
    }

    private static int WriteScenarios(string[] args, TextWriter output)
    {
        if (args.Length != 2 || !string.Equals(args[1], "list", StringComparison.Ordinal))
        {
            output.WriteLine("Usage: ksefguard scenarios list");
            return 2;
        }

        foreach (var scenario in ScenarioCatalog.All)
        {
            output.WriteLine($"{scenario.Id}\t{scenario.DescriptionEn}");
        }

        return 0;
    }

    private static async Task<int> InitAsync(string[] args, TextWriter output, CancellationToken cancellationToken)
    {
        var outputPath = GetOption(args, "--output") ?? throw new PackValidationException("init requires --output <directory>.");
        await StarterPackWriter.WriteAsync(outputPath, cancellationToken).ConfigureAwait(false);
        await output.WriteLineAsync($"Starter scenario pack written to {Path.GetFullPath(outputPath)}").ConfigureAwait(false);
        return 0;
    }

    private static async Task<int> ValidateAsync(string[] args, TextWriter output, CancellationToken cancellationToken)
    {
        var packPath = GetOption(args, "--pack") ?? throw new PackValidationException("validate requires --pack <directory-or-zip>.");
        var outputPath = GetOption(args, "--output") ?? throw new PackValidationException("validate requires --output <directory>.");

        var engine = new ValidationEngine();
        var report = await engine.ValidateAsync(packPath, cancellationToken: cancellationToken).ConfigureAwait(false);
        await ReportWriters.WriteAsync(report, outputPath, cancellationToken).ConfigureAwait(false);

        await output.WriteLineAsync($"Status: {report.Status}").ConfigureAwait(false);
        await output.WriteLineAsync($"Passed: {report.Summary.Passed}; failed: {report.Summary.Failed}; needs review: {report.Summary.NeedsReview}").ConfigureAwait(false);
        await output.WriteLineAsync($"JSON: {Path.GetFullPath(Path.Combine(outputPath, "report.json"))}").ConfigureAwait(false);
        await output.WriteLineAsync($"HTML: {Path.GetFullPath(Path.Combine(outputPath, "report.html"))}").ConfigureAwait(false);
        return report.Status == CheckStatus.Pass ? 0 : 1;
    }

    private static string? GetOption(string[] args, string option)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], option, StringComparison.Ordinal))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static int UnknownCommand(string command, TextWriter error)
    {
        error.WriteLine($"Unknown command: {command}");
        WriteHelp(error);
        return 2;
    }

    private static bool IsHelp(string value) => value is "help" or "--help" or "-h";

    private static void WriteHelp(TextWriter writer)
    {
        writer.WriteLine("KSeF Guard — independent, unofficial KSeF developer fixture checks");
        writer.WriteLine();
        writer.WriteLine("Commands:");
        writer.WriteLine("  ksefguard version");
        writer.WriteLine("  ksefguard scenarios list");
        writer.WriteLine("  ksefguard init --output <directory>");
        writer.WriteLine("  ksefguard validate --pack <directory-or-zip> --output <directory>");
        writer.WriteLine();
        writer.WriteLine("No production invoice transport. No tax, accounting, legal or compliance certification.");
    }
}
