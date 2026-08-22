
# Lifecycle and freshness

Pack lifecycle values are `SUPPORTED`, `MAINTENANCE_ONLY`, `FROZEN` and `ARCHIVED`.

Every run takes an explicit UTC `--as-of` instant. Content is stale after `reviewDueAt`. Stale, frozen, archived, unsupported or unverified content cannot produce an ordinary success exit.

`lastVerified` changes only after actual review and is never updated automatically.
