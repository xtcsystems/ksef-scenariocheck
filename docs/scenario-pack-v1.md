# Scenario pack v1

A scenario pack is a directory or ZIP containing:

```text
scenario-pack.json
fixtures/
expected/
```

The manifest references one fixture JSON file and one expected JSON file per scenario. Paths must remain inside the pack root. Archives are limited to 200 files, 2 MiB per file, and 10 MiB uncompressed total. Scripts, executables, symbolic links, archive traversal, and private-key markers are refused.

The public probe supports schema version `1` and at most five scenarios, using the types listed by:

```bash
ksefguard scenarios list
```

The JSON Schema in [`schemas/scenario-pack-v1.schema.json`](../schemas/scenario-pack-v1.schema.json) documents the manifest shape. Runtime validation is authoritative for the probe.

Each scenario encodes an explicit fixture expectation. KSeF Guard does not infer tax law and does not turn a passing fixture into an official compliance certification.
