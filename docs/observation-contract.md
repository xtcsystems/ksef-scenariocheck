
# Observation contract v1

An observation is bounded UTF-8 JSON with `schemaVersion: "1.0"`, one accepted profile, one scenario and 1–256 closed event objects.

Initial event kinds are `invoice-issued`, `offline-mode-declared` and `transmission-attempt`. Every event also has a bounded synthetic ID, UTC `Z` timestamp, unique sequence and a reference beginning with `syn:`.

Unknown fields, duplicate JSON properties, non-UTC offsets and production-oriented fields are rejected.
