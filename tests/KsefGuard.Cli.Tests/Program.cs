using KsefGuard.Cli;

var failures = new List<string>();
await RunAsync("version", async () =>
{
    var result = await InvokeAsync(["version"]);
    Assert(result.ExitCode == 0, result.Error);
    Assert(!string.IsNullOrWhiteSpace(result.Output), "Version output is empty.");
});

await RunAsync("scenarios list", async () =>
{
    var result = await InvokeAsync(["scenarios", "list"]);
    Assert(result.ExitCode == 0, result.Error);
    Assert(result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length == 5, "Expected five scenarios.");
});

await RunAsync("init and validate", async () =>
{
    var temp = Path.Combine(Path.GetTempPath(), $"ksefguard-cli-tests-{Guid.NewGuid():N}");
    var pack = Path.Combine(temp, "pack");
    var output = Path.Combine(temp, "report");

    var init = await InvokeAsync(["init", "--output", pack]);
    Assert(init.ExitCode == 0, init.Error);

    var validate = await InvokeAsync(["validate", "--pack", pack, "--output", output]);
    Assert(validate.ExitCode == 0, validate.Error + validate.Output);
    Assert(File.Exists(Path.Combine(output, "report.json")), "JSON report missing.");
    Assert(File.Exists(Path.Combine(output, "report.html")), "HTML report missing.");
    Directory.Delete(temp, true);
});

await RunAsync("unsafe zip exit code", async () =>
{
    var temp = Path.Combine(Path.GetTempPath(), $"ksefguard-cli-tests-{Guid.NewGuid():N}");
    Directory.CreateDirectory(temp);
    var zip = Path.Combine(temp, "unsafe.zip");
    using (var archive = System.IO.Compression.ZipFile.Open(zip, System.IO.Compression.ZipArchiveMode.Create))
    {
        archive.CreateEntry("../escape.txt");
    }

    var result = await InvokeAsync(["validate", "--pack", zip, "--output", Path.Combine(temp, "out")]);
    Assert(result.ExitCode == 4, $"Expected exit 4, got {result.ExitCode}. {result.Error}");
    Directory.Delete(temp, true);
});

if (failures.Count == 0)
{
    Console.WriteLine("CLI test harness passed.");
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

static async Task<(int ExitCode, string Output, string Error)> InvokeAsync(string[] args)
{
    using var output = new StringWriter();
    using var error = new StringWriter();
    var exitCode = await CliApplication.RunAsync(args, output, error, CancellationToken.None);
    return (exitCode, output.ToString(), error.ToString());
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
