# KSeF Guard

KSeF Guard is being designed as a permanent free developer edition for local KSeF special-mode fixture and regression checks.

> **Current state:** architecture review. No supported release is available yet.

KSeF Guard is an independent product. It is not affiliated with or endorsed by the Polish Ministry of Finance or CIRFMF. It will not provide tax, accounting, legal or compliance advice and will not certify that an implementation complies with KSeF requirements.

## Intended free-edition job

The free edition should let a developer run a small, versioned set of local KSeF special-mode checks and obtain deterministic evidence suitable for local development or CI without transmitting production invoices or hosting customer data.

Candidate scenario areas include:

- certificate metadata and lifecycle assumptions;
- QR verification-link inputs;
- offline24 timelines;
- retry and recovery sequences;
- unresolved or contradictory status evidence.

The exact architecture, runtime, distribution channels, license and scenario-pack model are under independent review and are not yet accepted.

## Long-term product model

This repository is intended to remain the permanent public product, documentation and community surface.

The free edition should remain genuinely useful. A future paid expansion, if justified by adoption, would focus on organization-scale automation, governance, maintained scenario content, integrations, evidence, hosted coordination or commercial support without replacing this public repository.

## Explicit non-goals

KSeF Guard is not intended to become:

- a production KSeF gateway;
- an invoicing or accounting application;
- a tax/legal certification service;
- a customer-specific ERP integration consultancy;
- a hosted store for invoices, certificates, keys, tokens or credentials.

## Development status

A prior implementation attempt is preserved as an unaccepted spike and is not the architecture baseline. New implementation will begin only after independent architecture proposals, reconciliation and explicit approval.

No installation instructions, package or release should be treated as supported until this README states otherwise.
