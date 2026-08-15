using System.Text;

namespace KsefGuard;

public static class StarterPackWriter
{
    private static readonly IReadOnlyDictionary<string, string> Files = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["scenario-pack.json"] = """
{
  "schemaVersion": 1,
  "id": "ksefguard-starter",
  "version": "1.0.0",
  "title": {
    "en": "KSeF Guard starter scenario pack",
    "pl": "Pakiet startowy scenariuszy KSeF Guard"
  },
  "sourceReferences": [
    {
      "title": "KSeF 2.0 API and integration material",
      "url": "https://github.com/CIRFMF/ksef-api"
    },
    {
      "title": "KSeF certificates",
      "url": "https://ksef.podatki.gov.pl/informacje-ogolne-ksef-20/certyfikaty-ksef/"
    },
    {
      "title": "KSeF offline24",
      "url": "https://ksef.podatki.gov.pl/informacje-ogolne-ksef-20/tryb-offline24/"
    }
  ],
  "scenarios": [
    {
      "id": "certificate-starter",
      "type": "certificate-metadata",
      "fixture": "fixtures/certificate.json",
      "expected": "expected/certificate.json"
    },
    {
      "id": "qr-starter",
      "type": "qr-verification-link",
      "fixture": "fixtures/qr.json",
      "expected": "expected/qr.json"
    },
    {
      "id": "offline-starter",
      "type": "offline24-timeline",
      "fixture": "fixtures/offline.json",
      "expected": "expected/offline.json"
    },
    {
      "id": "retry-starter",
      "type": "retry-recovery-sequence",
      "fixture": "fixtures/retry.json",
      "expected": "expected/retry.json"
    },
    {
      "id": "status-starter",
      "type": "unresolved-status",
      "fixture": "fixtures/status.json",
      "expected": "expected/status.json"
    }
  ]
}
""",
        ["fixtures/certificate.json"] = """
{
  "certificateDerBase64": "MIIDbzCCAlegAwIBAgIUNu+2fO+zKDdGxHGAJdcuhIiqDBYwDQYJKoZIhvcNAQELBQAwRzEdMBsGA1UEAwwUS1NlRiBHdWFyZCBTeW50aGV0aWMxGTAXBgNVBAoMEHh0Y3N5c3RlbXMgUHJvYmUxCzAJBgNVBAYTAlBMMB4XDTI2MDgxNTIyMDY0M1oXDTI4MDgxNDIyMDY0M1owRzEdMBsGA1UEAwwUS1NlRiBHdWFyZCBTeW50aGV0aWMxGTAXBgNVBAoMEHh0Y3N5c3RlbXMgUHJvYmUxCzAJBgNVBAYTAlBMMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvyb0FVP2XJ5Y/ZxLs1oMKKac7onDlc28mic8JvBfMkz5OsozVtkZI5xcLk7MlkVn23NSUfflKiyjA8GRikwoGF9jWF38rGlnkf/e799DjXGF0BDZcWS0Q86y3NKHDmraftyHWODJc8tIMnmIXBgncufYoI+DcVe18mYlgYpeCET1rLMVqHsTI9lNQOlN36pQshcpmPcT6jb1hhGpFdO8J4GsQ0MpRvZ87l4/Bk/j8U8vdYSfKP1OyYgxSWbySrwRWeIKc7gQ6aw1ql5kzeXs8M/au8twhKQXztgS/Nq5r2iKi4oXZBYGKnbct7mm1YTtOpLJJYQrysctd/EqgsqoGwIDAQABo1MwUTAdBgNVHQ4EFgQUm/a1BVY45zJKIGNZ+HTxE4inEjUwHwYDVR0jBBgwFoAUm/a1BVY45zJKIGNZ+HTxE4inEjUwDwYDVR0TAQH/BAUwAwEB/zANBgkqhkiG9w0BAQsFAAOCAQEAfDrC4+t5jCFlSFJFdEPN/aIfbXMMSkXwxcbCQmyfoIYW6I39910eTQl+3zhPwIG7UPjmUH6cfJXse+cdt5E2OTjzzS9dv5fM1ODjszeH2PaPiZ65SzmkY6CZ3z3KG0Er6AWVY02uAZh2nYRd3CawPQP5dU8BvY0LeZCdNVDcTRwVJVvpZfYuJhc49Rb4mC8qwDoIRDkqSQUtKy6BrGMjjOIWt+yy9FRP/RvaxHzpzsepstYFF8MrGo6D6fI1jUCjEEnzRhOISdsD46N8aPXSqpMRv7qmgpdEOxOJRyQ36wgiiYUO31bIyKewAvaBpDGvq1x9irR/rGfHGvNn2ecjNA=="
}
""",
        ["expected/certificate.json"] = """
{
  "subjectContains": "CN=KSeF Guard Synthetic",
  "issuerContains": "CN=KSeF Guard Synthetic",
  "validAt": "2027-01-01T00:00:00Z",
  "sha256Thumbprint": "D1E198F9CEA2FD467D1DB78DD65C20EE3FC316A55FB3FF74B04B613FA525449B"
}
""",
        ["fixtures/qr.json"] = """
{
  "hashSourceUtf8": "KSeF Guard synthetic invoice content v1",
  "declaredHash": "GTJGd4ebQ5WV1qkBi_3oZsPvqm56qRYhXTyvUt5WxcA",
  "declaredLink": "https://qr.ksef.mf.gov.pl/invoice/GTJGd4ebQ5WV1qkBi_3oZsPvqm56qRYhXTyvUt5WxcA"
}
""",
        ["expected/qr.json"] = """
{
  "linkPrefix": "https://qr.ksef.mf.gov.pl/invoice/"
}
""",
        ["fixtures/offline.json"] = """
{
  "events": [
    {
      "type": "issued-offline",
      "at": "2026-08-15T08:00:00Z"
    },
    {
      "type": "submitted",
      "at": "2026-08-15T12:00:00Z"
    }
  ]
}
""",
        ["expected/offline.json"] = """
{
  "requiredSequence": [
    "issued-offline",
    "submitted"
  ],
  "maxElapsedMinutes": 1440
}
""",
        ["fixtures/retry.json"] = """
{
  "outcomes": [
    "transient-failure",
    "transient-failure",
    "accepted"
  ]
}
""",
        ["expected/retry.json"] = """
{
  "maxAttempts": 3,
  "requiredTerminalOutcome": "accepted",
  "allowedIntermediateOutcomes": [
    "transient-failure",
    "rate-limited"
  ]
}
""",
        ["fixtures/status.json"] = """
{
  "records": [
    {
      "reference": "REF-001",
      "status": "accepted"
    },
    {
      "reference": "REF-002",
      "status": "rejected"
    }
  ]
}
""",
        ["expected/status.json"] = """
{
  "terminalStatuses": [
    "accepted",
    "rejected",
    "cancelled"
  ]
}
"""
    };

    public static async Task WriteAsync(string outputDirectory, CancellationToken cancellationToken = default)
    {
        if (Directory.Exists(outputDirectory) && Directory.EnumerateFileSystemEntries(outputDirectory).Any())
        {
            throw new IOException($"Output directory is not empty: {outputDirectory}");
        }

        Directory.CreateDirectory(outputDirectory);
        foreach (var pair in Files)
        {
            var target = Path.Combine(outputDirectory, pair.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            await File.WriteAllTextAsync(target, pair.Value + Environment.NewLine, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        }
    }
}
