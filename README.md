# KSeF Guard

Local deterministic KSeF 2.0 special-mode fixture and regression checks for developers and CI.

> **Probe status:** This repository contains the bounded public-core probe for Opportunity Radar candidate `O-0001`.

KSeF Guard is an independent developer tool. It is not affiliated with or endorsed by the Polish Ministry of Finance or CIRFMF. It does not provide tax, accounting, legal or compliance advice and does not certify that an implementation complies with KSeF requirements.

## What the probe does

`ksefguard` validates a versioned local scenario pack and writes deterministic JSON and HTML evidence reports. The first public/basic edition supports no more than five synthetic fixture types:

- certificate metadata;
- QR verification-link inputs;
- offline24 timelines;
- retry/recovery sequences;
- unresolved or contradictory statuses.

It never transmits production invoices and does not host customer data.

## Quick start

Download a self-contained binary from GitHub Releases, then:

```bash
ksefguard init --output sample-pack
ksefguard validate --pack sample-pack --output evidence
```

A successful validation returns exit code `0`. Findings return `1`, an invalid pack returns `2`, runtime/I/O errors return `3`, and unsafe or prohibited input returns `4`.

See:

- [Getting started / Pierwsze kroki](docs/getting-started.md)
- [Scenario pack v1](docs/scenario-pack-v1.md)
- [Limitations / Ograniczenia](docs/limitations.md)
- [Privacy and telemetry / Prywatność i telemetria](docs/privacy-and-telemetry.md)

## Explicit exclusions

KSeF Guard is not:

- a production KSeF gateway;
- an invoicing or accounting application;
- a tax/legal certification service;
- a customer-specific ERP integration;
- a hosted store for invoices, certificates, keys, tokens or credentials.

## Probe evidence

Use the issue forms only after real product use. Do not upload invoices, NIPs, certificates, keys, tokens, credentials, customer files, or production payloads.

Stars and generic interest are weak signals. Qualified issues and requests tied to actual use are more useful.

## License

MIT. See [LICENSE](LICENSE).

Implementation governance and probe metrics are maintained in [`xtcsystems/opportunity-radar`](https://github.com/xtcsystems/opportunity-radar).
