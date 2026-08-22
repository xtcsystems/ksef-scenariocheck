using XtcSystems.KsefScenarioCheck.Core.Contracts;

namespace XtcSystems.KsefScenarioCheck.Cli;

internal static class OptionParser
{
    public static IReadOnlyDictionary<string, string> Parse(string[] args, params string[] allowedOptions)
    {
        var allowed = new HashSet<string>(allowedOptions, StringComparer.Ordinal);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        for (int index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ContractException("KSC.CLI.INVALID", "The command-line arguments are invalid.");
            }

            string option = args[index];
            if (!allowed.Contains(option) || !result.TryAdd(option, args[index + 1]))
            {
                throw new ContractException("KSC.CLI.INVALID", "The command-line arguments contain an unknown or duplicate option.");
            }
        }

        return result;
    }

    public static string Require(IReadOnlyDictionary<string, string> options, string name)
        => options.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ContractException("KSC.CLI.INVALID", "A required command-line option is missing.");
}
