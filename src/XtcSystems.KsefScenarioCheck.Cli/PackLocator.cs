using XtcSystems.KsefScenarioCheck.Core.Contracts;

namespace XtcSystems.KsefScenarioCheck.Cli;

internal static class PackLocator
{
    public static string Resolve(string value)
    {
        if (string.Equals(value, "baseline-pl", StringComparison.Ordinal))
        {
            string? baseline = FindBaseline();
            return baseline ?? throw new ContractException("KSC.PACK.NOT_FOUND", "The bundled baseline pack could not be located.");
        }

        string directory;
        try
        {
            directory = Path.GetFullPath(value);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new ContractException("KSC.PACK.INVALID_PATH", "The selected pack path is invalid.");
        }

        if (!Directory.Exists(directory))
        {
            throw new ContractException("KSC.PACK.NOT_FOUND", "The selected pack directory does not exist.");
        }

        try
        {
            FileAttributes directoryAttributes = File.GetAttributes(directory);
            if ((directoryAttributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new ContractException("KSC.PACK.INVALID_PATH", "Pack directories reached through a reparse point are not accepted.");
            }

            string[] entries = Directory.EnumerateFileSystemEntries(directory, "*", SearchOption.TopDirectoryOnly).ToArray();
            if (entries.Length != 1 ||
                !string.Equals(Path.GetFileName(entries[0]), "pack.json", StringComparison.Ordinal) ||
                !File.Exists(entries[0]))
            {
                throw new ContractException("KSC.PACK.INVALID", "A pack directory must contain exactly one file named pack.json.");
            }

            FileAttributes packAttributes = File.GetAttributes(entries[0]);
            if ((packAttributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new ContractException("KSC.PACK.INVALID_PATH", "Pack files reached through a reparse point are not accepted.");
            }

            return entries[0];
        }
        catch (ContractException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new ContractException("KSC.PACK.INVALID_PATH", "The selected pack path could not be inspected safely.");
        }
    }

    private static string? FindBaseline()
    {
        string direct = Path.Combine(AppContext.BaseDirectory, "packs", "baseline-pl", "pack.json");
        if (File.Exists(direct))
        {
            return direct;
        }

        foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(start);
            for (int level = 0; level < 10 && current is not null; level++, current = current.Parent)
            {
                string candidate = Path.Combine(current.FullName, "packs", "baseline-pl", "pack.json");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
