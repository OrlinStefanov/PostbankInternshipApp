## Search

Sample request:

    GET /api/audit?entityType=BinRange&from=2026-08-01T00:00:00Z&page=1&pageSize=25

Every filter is optional and they combine with AND. Dates are UTC: `from` is
inclusive, `to` is exclusive, so two adjacent day queries never claim the
same row twice. `entityType` is one of the values from
`GET /api/audit/entity-types`, matched case-insensitively.

`userName` is a starts-with match against the Identity user name. The literal
`system` selects rows written with no user signed in - it is not an account.

Results are ordered by `performedAt` descending, then by id descending so
two rows written in the same tick stay in a stable order across pages.

`oldValues` and `newValues` are the JSON snapshots the writer stored.
They are returned as raw strings - the client decides how to render them.

`pageSize` is capped at 200; `totalCount` counts every match rather
than just this page so a client can render a pager without a second call.

