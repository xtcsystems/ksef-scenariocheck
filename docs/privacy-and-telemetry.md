# Privacy and telemetry / Prywatność i telemetria

## Current release policy

The first release does not require an account and does not upload scenario packs. Validation is local.

Minimal custom telemetry was approved for the probe only as an explicit opt-in feature, but it is not a release requirement. If a compliant zero-cost endpoint is not available, v0.1 ships without custom telemetry.

If telemetry is later included under the existing approval, it may contain only:

- random installation identifier;
- tool version;
- command/scenario identifier;
- broad success/failure category;
- rounded timestamp.

It must never contain invoices, values, NIPs, paths, payloads, URLs, free text, certificates, keys, credentials, customer files, or endpoint data. The tool must work fully when telemetry is disabled.

GitHub and package registries may publish ordinary repository/release/download metadata under their own policies.

## Polski

Pierwsza wersja nie wymaga konta i nie wysyła pakietów scenariuszy. Walidacja jest lokalna.

Minimalna telemetria niestandardowa została zatwierdzona wyłącznie jako jawny opt-in, ale nie jest warunkiem wydania. Jeżeli zgodny bezpłatny endpoint nie będzie dostępny, v0.1 zostanie wydany bez telemetrii niestandardowej.

Dozwolone pola i bezwzględnie zabronione dane są identyczne z listą angielską powyżej. Narzędzie musi działać w pełni po wyłączeniu telemetrii.
