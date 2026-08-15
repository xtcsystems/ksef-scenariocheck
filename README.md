# KSeF Guard

Local deterministic KSeF 2.0 special-mode fixture and regression checks for developers and CI.

> **Probe status:** This repository contains a bounded public-core market probe. The first functional release is under development on `probe/o-0001-public-core`.

KSeF Guard is an independent developer tool. It is not affiliated with or endorsed by the Polish Ministry of Finance or CIRFMF. It does not provide tax, accounting, legal or compliance advice and does not certify that an implementation complies with KSeF requirements.

## Planned public/basic boundary

The probe will provide a local CLI for up to five synthetic fixture scenarios:

- certificate metadata;
- QR verification-link inputs;
- offline24 timelines;
- retry/recovery sequences;
- unresolved or contradictory statuses.

It will produce deterministic JSON and HTML evidence reports without transmitting production invoices or hosting customer data.

## Explicit exclusions

KSeF Guard is not:

- a production KSeF gateway;
- an invoicing or accounting application;
- a tax/legal certification service;
- a customer-specific ERP integration;
- a hosted store for invoices, certificates, keys, tokens or credentials.

## License

The public/basic probe will be released under the MIT License.

Implementation governance and probe metrics are maintained in [`xtcsystems/opportunity-radar`](https://github.com/xtcsystems/opportunity-radar).
