# Getting started / Pierwsze kroki

## English

1. Download the archive for your operating system from GitHub Releases.
2. Put `ksefguard` (or `ksefguard.exe`) on your `PATH`.
3. Create a synthetic starter pack:

```bash
ksefguard init --output sample-pack
```

4. Validate it:

```bash
ksefguard validate --pack sample-pack --output evidence
```

The command writes `report.json` and `report.html`.

Use only synthetic or sanitized artifacts. Do not put production invoices, NIPs, credentials, tokens, private keys, or customer data in a scenario pack or GitHub issue.

## Polski

1. Pobierz archiwum dla swojego systemu z GitHub Releases.
2. Umieść `ksefguard` (lub `ksefguard.exe`) w `PATH`.
3. Utwórz syntetyczny pakiet startowy:

```bash
ksefguard init --output sample-pack
```

4. Uruchom walidację:

```bash
ksefguard validate --pack sample-pack --output evidence
```

Polecenie zapisuje `report.json` i `report.html`.

Używaj wyłącznie danych syntetycznych lub zanonimizowanych. Nie umieszczaj faktur produkcyjnych, NIP-ów, danych uwierzytelniających, tokenów, kluczy prywatnych ani danych klientów w pakiecie scenariuszy lub zgłoszeniu GitHub.
