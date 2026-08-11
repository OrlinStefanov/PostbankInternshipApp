## Import

The file needs a header row and one BIN range per line:

    Prefix,CardScheme,ProductType,FundingType,CountryCode,ValidFrom,ValidTo
    400001,Visa,Consumer,Credit,US,2024-01-01,2026-12-31
    520082,Mastercard,Commercial,Debit,BG,2024-01-01,

`Prefix` is 6-8 digits. `CardScheme`, `ProductType`, `FundingType` and
`CountryCode` must match existing reference data (matched case-insensitively);
anything else is rejected rather than created. Dates are `yyyy-MM-dd`, and the
trailing `ValidTo` column is optional - leave it blank for an open-ended range.

Every row lands in exactly one bucket:

- **Inserted** - the prefix is new. A prefix whose only record was soft-deleted
  is revived in place rather than duplicated, and counts here.
- **Unchanged** - the prefix exists and every field already matches; skipped.
- **Conflict** - the prefix exists with different values. Nothing is overwritten:
  the row is staged with a per-field diff for a decision, via
  `POST /api/BinCsvImport/resolve-conflicts`.
- **Rejected** - malformed, a duplicate of an earlier row in the same file, or
  referencing reference data that does not exist. Recorded with the reason and
  the original line.

Inserts, revivals, rejections and staged conflicts are all written in a single
save, along with a summary in the import history.

## GetConflicts

Staged conflicts are persisted, so this rebuilds the outstanding worklist without
re-uploading the file - which is what lets a client restore the review screen after
a reload. Each diff is recomputed against the record as it stands now, so it stays
accurate if the underlying range changed since the import. Conflicts that have been
applied or discarded are not returned.

## GetHistory

Sample request:

    GET /api/BinCsvImport/history?status=Partial&from=2026-08-01T00:00:00Z&page=1&pageSize=25

Every filter is optional and they combine with AND. Dates are UTC: `from` is
inclusive, `to` is exclusive. `status` is one of `Success`,
`Partial` or `Failed`, matched case-insensitively.

Results are ordered by `importedAt` descending, then by id descending so two
imports recorded in the same tick stay in a stable order across pages.

`updatedRows` counts existing ranges the import ultimately changed, and grows
after the fact as the conflicts that import staged are resolved with an update -
so a fresh import shows zero updates until its conflicts are worked through.
`pageSize` is capped at 200; `totalCount` counts every match.

## ResolveConflicts

Send one entry per conflict:

    [
      { "pendingBinConflictId": 12, "update": true },
      { "pendingBinConflictId": 13, "update": false }
    ]

`update: true` writes the imported values onto the existing BIN range, keeping the
same record and its audit trail. `update: false` keeps the stored record as-is and
discards the imported row. Either way the conflict is closed and stops appearing in
`GET /api/BinCsvImport/conflicts`.

Ids that do not exist, or that were already resolved, are counted under
`notFoundCount` instead of failing the request - so retrying a batch is safe. If the
same id appears twice, the last decision wins.

