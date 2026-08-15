using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace KsefGuard;

public sealed class ScenarioPackLoader
{
    public const int MaximumEntries = 200;
    public const long MaximumSingleFileBytes = 2 * 1024 * 1024;
    public const long MaximumTotalBytes = 10 * 1024 * 1024;

    private static readonly string[] ProhibitedExtensions =
    [
        ".exe", ".dll", ".com", ".scr", ".msi", ".bat", ".cmd", ".ps1",
        ".sh", ".jar", ".so", ".dylib", ".vbs", ".js"
    ];

    private static readonly string[] PrivateKeyMarkers =
    [
        "-----BEGIN PRIVATE KEY-----",
        "-----BEGIN ENCRYPTED PRIVATE KEY-----",
        "-----BEGIN RSA PRIVATE KEY-----",
        "-----BEGIN EC PRIVATE KEY-----",
        "-----BEGIN OPENSSH PRIVATE KEY-----"
    ];

    private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = false
    };

    public async Task<LoadedScenarioPack> LoadAsync(string inputPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        var fullPath = Path.GetFullPath(inputPath);
        if (Directory.Exists(fullPath))
        {
            await ValidateDirectorySafetyAsync(fullPath, cancellationToken).ConfigureAwait(false);
            var manifest = await LoadManifestAsync(fullPath, cancellationToken).ConfigureAwait(false);
            ValidateManifest(fullPath, manifest);
            return new LoadedScenarioPack(fullPath, DeleteOnDispose: false, manifest);
        }

        if (!File.Exists(fullPath))
        {
            throw new PackValidationException($"Scenario pack does not exist: {inputPath}");
        }

        if (!string.Equals(Path.GetExtension(fullPath), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new PackValidationException("Scenario packs must be a directory or a .zip archive.");
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"ksefguard-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempPath);

        try
        {
            await ExtractSafelyAsync(fullPath, tempPath, cancellationToken).ConfigureAwait(false);
            await ValidateDirectorySafetyAsync(tempPath, cancellationToken).ConfigureAwait(false);
            var manifest = await LoadManifestAsync(tempPath, cancellationToken).ConfigureAwait(false);
            ValidateManifest(tempPath, manifest);
            return new LoadedScenarioPack(tempPath, DeleteOnDispose: true, manifest);
        }
        catch
        {
            try
            {
                Directory.Delete(tempPath, recursive: true);
            }
            catch
            {
                // Preserve the original exception.
            }

            throw;
        }
    }

    private static async Task ExtractSafelyAsync(string zipPath, string destination, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(zipPath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

        if (archive.Entries.Count > MaximumEntries)
        {
            throw new UnsafePackException($"Archive has more than {MaximumEntries} entries.");
        }

        var destinationPrefix = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
        long totalBytes = 0;

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            ValidateExtension(entry.FullName);

            if (entry.Length > MaximumSingleFileBytes)
            {
                throw new UnsafePackException($"Archive entry is too large: {entry.FullName}");
            }

            totalBytes = checked(totalBytes + entry.Length);
            if (totalBytes > MaximumTotalBytes)
            {
                throw new UnsafePackException($"Archive expands beyond {MaximumTotalBytes} bytes.");
            }

            var target = Path.GetFullPath(Path.Combine(destination, entry.FullName));
            if (!target.StartsWith(destinationPrefix, PathComparison))
            {
                throw new UnsafePackException($"Archive entry escapes the scenario-pack root: {entry.FullName}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            await using var source = entry.Open();
            await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await source.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task ValidateDirectorySafetyAsync(string rootPath, CancellationToken cancellationToken)
    {
        var normalizedRoot = Path.GetFullPath(rootPath);
        var rootAttributes = File.GetAttributes(normalizedRoot);
        if ((rootAttributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new UnsafePackException("The scenario-pack root cannot be a symbolic link or reparse point.");
        }

        var rootPrefix = normalizedRoot + Path.DirectorySeparatorChar;
        var pendingDirectories = new Stack<string>();
        var files = new List<string>();
        pendingDirectories.Push(normalizedRoot);

        var entryCount = 0;
        while (pendingDirectories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pendingDirectories.Pop();

            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                cancellationToken.ThrowIfCancellationRequested();

                entryCount++;
                if (entryCount > MaximumEntries)
                {
                    throw new UnsafePackException($"Scenario pack has more than {MaximumEntries} entries.");
                }

                var fullPath = Path.GetFullPath(entry);
                if (!fullPath.StartsWith(rootPrefix, PathComparison))
                {
                    throw new UnsafePackException($"Entry escapes the scenario-pack root: {entry}");
                }

                var attributes = File.GetAttributes(fullPath);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new UnsafePackException($"Symbolic links/reparse points are not allowed: {Path.GetRelativePath(normalizedRoot, fullPath)}");
                }

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    pendingDirectories.Push(fullPath);
                    continue;
                }

                files.Add(fullPath);
            }
        }

        long totalBytes = 0;
        foreach (var fullPath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateExtension(fullPath);

            var info = new FileInfo(fullPath);
            if (info.Length > MaximumSingleFileBytes)
            {
                throw new UnsafePackException($"File is too large: {Path.GetRelativePath(normalizedRoot, fullPath)}");
            }

            totalBytes = checked(totalBytes + info.Length);
            if (totalBytes > MaximumTotalBytes)
            {
                throw new UnsafePackException($"Scenario pack exceeds {MaximumTotalBytes} bytes.");
            }

            if (IsPotentiallyText(fullPath))
            {
                var text = await File.ReadAllTextAsync(fullPath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
                if (PrivateKeyMarkers.Any(marker => text.Contains(marker, StringComparison.Ordinal)))
                {
                    throw new UnsafePackException($"Private-key material is prohibited: {Path.GetRelativePath(normalizedRoot, fullPath)}");
                }
            }
        }
    }

    private static bool IsPotentiallyText(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pem", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".csv", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateExtension(string path)
    {
        var extension = Path.GetExtension(path);
        if (ProhibitedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnsafePackException($"Executable/script content is not allowed in scenario packs: {path}");
        }
    }

    private static async Task<ScenarioPackManifest> LoadManifestAsync(string rootPath, CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(rootPath, "scenario-pack.json");
        if (!File.Exists(manifestPath))
        {
            throw new PackValidationException("scenario-pack.json is missing.");
        }

        try
        {
            await using var stream = File.OpenRead(manifestPath);
            return await JsonSerializer.DeserializeAsync<ScenarioPackManifest>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new PackValidationException("scenario-pack.json is empty or invalid.");
        }
        catch (JsonException exception)
        {
            throw new PackValidationException("scenario-pack.json is not valid JSON.", exception);
        }
    }

    private static void ValidateManifest(string rootPath, ScenarioPackManifest manifest)
    {
        if (manifest.SchemaVersion != 1)
        {
            throw new PackValidationException($"Unsupported schemaVersion {manifest.SchemaVersion}. Only version 1 is supported.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Id) || string.IsNullOrWhiteSpace(manifest.Version))
        {
            throw new PackValidationException("Pack id and version are required.");
        }

        if (manifest.Title is null || string.IsNullOrWhiteSpace(manifest.Title.En) || string.IsNullOrWhiteSpace(manifest.Title.Pl))
        {
            throw new PackValidationException("Pack title must include English and Polish text.");
        }

        if (manifest.Scenarios is null || manifest.Scenarios.Count == 0)
        {
            throw new PackValidationException("At least one scenario is required.");
        }

        if (manifest.Scenarios.Count > 5)
        {
            throw new PackValidationException("the public probe supports no more than five scenarios per pack.");
        }

        var duplicateId = manifest.Scenarios
            .GroupBy(item => item.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateId is not null)
        {
            throw new PackValidationException($"Scenario id is duplicated: {duplicateId.Key}");
        }

        foreach (var scenario in manifest.Scenarios)
        {
            if (string.IsNullOrWhiteSpace(scenario.Id) || string.IsNullOrWhiteSpace(scenario.Type))
            {
                throw new PackValidationException($"Every scenario requires id and type.");
            }

            if (!ScenarioCatalog.IsSupported(scenario.Type))
            {
                throw new PackValidationException($"Unsupported scenario type: {scenario.Type}");
            }

            ValidateReferencedFile(rootPath, scenario.Fixture, $"{fixture for {scenario.Id}");
            ValidateReferencedFile(rootPath, scenario.Expected, $"{expected data for {scenario.Id}");
        }
    }

    private static void ValidateReferencedFile(string rootPath, string relativePath, string description)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new PackValidationException($"Invalid {description} path.");
        }

        var rootPrefix = Path.GetFullPath(rootPath) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(Path.Combine(rootPath, relativePath));
        if (!target.StartsWith(rootPrefix, PathComparison))
        {
            throw new UnsafePackException($"Referenced file escapes the pack root: {relativePath}");
        }

        if (!File.Exists(target))
        {
            throw new PackValidationException($"Missing {description}: {relativePath}");
        }
    }
}
