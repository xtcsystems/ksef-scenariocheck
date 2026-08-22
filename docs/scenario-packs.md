
# Scenario packs

A v1 pack is one unpacked directory containing exactly `pack.json`. Packs are JSON-only, declarative, bounded and non-executable.

The vertical slice implements `event-present`, `field-equals`, `sequence-before`, `timestamp-before-or-equal` and `logical-all`.

The bundled pack is first-party reviewed. A user-supplied local pack is `UNVERIFIED` and cannot produce an ordinary success state.
