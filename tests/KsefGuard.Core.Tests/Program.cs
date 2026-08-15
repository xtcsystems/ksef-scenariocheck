using System.IO.Compression;
using KsefGuard;

var failures = new List<string>();
await RunAsync("all-pass pack passes", async () =>
{
    var engine = new ValidationEngine();
    var report = await engine.ValidateAsync(Path.Combine(AppContext.BaseDirectory, "examples", "scenario-packs", "all-pass"), DateTimeOffset.Parse("2026-08-15T00:00:00Z"));
    Assert(report.Status == CheckStatus.Pass, $"Expected Pass, got {report.Status}");
    Assert(report.Scenarios.Count == 5, "Expected five scenarios.");
});

await RunAsync("findings pack returns every scenario type", async () =>
{
    var engine = new ValidationEngine();
    var report = await engine.ValidateAsync(Path.Combine(AppContext.BaseDirectory, "examples", "scenario-packs", "findings"), DateTimeOffset.Parse("2026-08-15T00:00:00Z"));
    Assert(report.Status != CheckStatus.Pass, "Expected non-pass report.");
    Assert(report.Scenarios.Select(item => item.Type).Distinct().Count() == 5, "Expected five distinct scenario types.");
    Assert(report.Summary.Failed > 0, "Expected deterministic failures.");
    Assert(report.Summary.NeedsReview > 0, "Expected a needs-review finding.");
});

await RunAsync("zip traversal is refused", async () =>
{
    var temp = NewTemp();
    var zipPath = Path.Combine(temp, "unsafe.zip");
    using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
    {
        var entry = archive.CreateEntry("../escape.txt");
        await using var writer = new StreamWriter(entry.Open());
        await writer.WriteAsync("unsafe");
    }

    var loader = new ScenarioPackLoader();
    await AssertThrowsAsync<UnsafePackException>(() => loader.LoadAsync(zipPath));
    Directory.Delete(temp, true);
});

await RunAsync("private key marker is refused", async () =>
{
    var temp = NewTemp();
    await File.WriteAllTextAsync(Path.Combine(temp, "scenario-pack.json"), "{}");
    await File.WriteAllTextAsync(Path.Combine(temp, "secret.pem"), "-----BEGIN PRIVATE KEY-----\nnot-a-key");
    var loader = new ScenarioPackLoader();
    await AssertThrowsAsync<UnsafePackException>(() => loader.LoadAsync(temp));
    Directory.Delete(temp, true);
});


if (!OperatingSystem.IsWindows())
{
    await RunAsync("symbolic-link directories are refused", async () =>
    {
        var temp = NewTemp();
        var outside = NewTemp();
        await File.WriteAllTextAsync(Path.Combine(outside, "outside.json"), "{}");
        Directory.CreateSymbolicLink(Path.Combine(temp, "linked"), outside);

        try
        {
            var loader = new ScenarioPackLoader();
            await AssertThrowsAsync<UnsafePackException>(() => loader.LoadAsync(temp));
        }
        finally
        {
            Directory.Delete(temp, true);
            Directory.Delete(outside, true);
        }
    });
}

if (failures.Count == 0)
{
    Console.WriteLine("Core test harness passed.");
    return 0;
}

foreach (var failure in failures)
{
    Console.Error.WriteLine(failure);
}

return 1;

async Task RunAsync(string name, Func<Task> test)
{
    try
    {
        await test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failures.Add($"FAIL {name}: {exception}");
    }
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static async Task AssertThrowsAsync<T>(Func<Task> action) where T : Exception
{
    try
    {
        await action();
    }
    catch (T)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

static string NewTemp()
{
    var path = Path.Combine(Path.GetTempPath(), $"ksefguard-tests-{Guid.NewGuid():N}");
    Directory.CreateDirectory(path);
    return path;
}
