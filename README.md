# KSeF Guard

KSeF Guard is being designed as a permanent free developer edition for local KSeF special-mode fixture and regression checks.

> **Current state:** two independent architecture reports have been reconciled. The proposed architecture is awaiting explicit owner approval. No supported release is available and no implementation is authorized.

KSeF Guard is an independent product. It is not affiliated with or endorsed by the Polish Ministry of Finance or CIRFMF. It will not provide tax, accounting, legal or compliance advice and will not certify that an implementation complies with KSeF requirements.

## Proposed free-edition job

The proposed architecture would let a developer produce a normalized synthetic observation from their own KSeF integration test harness, evaluate it locally against a small source-grounded baseline, and obtain deterministic console and JSON evidence suitable for local development or single-repository CI.

The proposed first release is deliberately narrow:

- one `offline24-timeline` vertical slice;
- one `retry-recovery-trace` family containing bounded retry, transition, terminal, duplicate, contradictory and unresolved-status assertions;
- no direct KSeF or ERP driving;
- no live environment execution;
- no production invoice payloads, credentials, certificates or private keys;
- no runtime telemetry.

These decisions are proposals until owner approval.

## Long-term product model

This repository remains the intended permanent public product, documentation and community surface.

The free edition should remain genuinely useful. Future paid expansion, if justified by adoption, may focus on faster and broader maintained content, compatibility matrices, organization governance, multi-repository orchestration, evidence retention, notifications, private content and commercial support.

No private, hosted or paid infrastructure is authorized now.

## Proposed technical direction

Subject to owner approval:

- C# on the current supported .NET LTS target;
- NuGet global tool and GitHub Releases;
- tested GitHub Actions and Azure Pipelines YAML examples;
- Apache-2.0;
- JSON-only declarative non-executable scenario packs;
- no initial GitHub Action wrapper, Azure DevOps task, container or hosted service;
- official-primary-source provenance, freshness and freeze/archive metadata.

## Explicit non-goals

KSeF Guard is not intended to become:

- a production KSeF gateway;
- an invoicing or accounting application;
- a tax/legal certification service;
- a customer-specific ERP integration consultancy;
- a hosted store for invoices, certificates, keys, tokens or credentials.

## Development status

A prior implementation attempt is preserved in closed PR #1 as an unaccepted spike. It was not used by either independent architect and is not the implementation baseline.

New implementation will begin only after:

1. explicit owner approval of the reconciled architecture;
2. preparation of the official-source rule ledger and exact implementation/review handoffs;
3. a second explicit owner decision authorizing Codex implementation.

No installation instructions, package, license choice or release should be treated as supported until this README states otherwise.
