## Search

Sample request:

    GET /api/binranges?prefix=4000&cardScheme=Visa&page=1&pageSize=25

Every filter is optional and they combine with AND. `prefix` is a starts-with
match, so `4000` finds both 400001 and 40000123. The name filters
(`cardScheme`, `productType`, `fundingType`, `countryCode`) are matched
case-insensitively against the reference data. `createdBy` narrows to the ranges a
given account added, matched case-insensitively; `system` selects rows written with
no user signed in.

Each row carries a `status` derived from its dates and delete flag rather than
stored, so it cannot fall out of step with them:

- **Active** - valid today, and what classification will match.
- **Scheduled** - `validFrom` is in the future.
- **Expired** - `validTo` has passed.
- **Deleted** - soft-deleted.

Filtering by `status` narrows to one of those. Left unset, deleted ranges are
excluded and everything else is returned - so `status=Deleted` is the only way
to see them.

Each row also reports who added it (`createdBy`, `createdAt`) and who last changed
it (`updatedBy`, `updatedAt`) - normally the user who ran the import, and whoever
later applied a conflict over it. A `createdBy` of `system` is not an account: it
means no user was signed in when the row was written.

Results are ordered by prefix. `pageSize` is capped at 200; `totalCount` counts
every match rather than just this page, so a client can render a pager without
a second call.

## Filters

The card schemes, product types, funding types and countries currently in the
database, so a client can populate its filter dropdowns from data instead of
hard-coding the lists. Soft-deleted reference values are omitted. `creators` lists
the accounts that have added a range - the values the `createdBy` filter takes.

## SchemeMismatches

One row per live range where the digits say one network and the stored
`cardScheme` names another. Each item carries the detector's opinion in
`detectedScheme`. Rows the detector cannot judge (a prefix in no known IIN
range) are not returned - only genuine contradictions.

## SchemeMismatchCount

Just the number, for a badge. The same scan as `scheme-mismatches` without
building the projection, so a client polling for the badge does not pay for a
page of rows it will not render. Zero is the healthy answer and comes back as
`0`, not as an empty body.

## DetectScheme

Answers two questions about a prefix a user is still typing: which network its
digits belong to, and whether that is the scheme they have chosen. It reads no
data, so the prefix need not exist as a range.

`detectedScheme` is null when the prefix falls outside every published IIN range
the detector knows - an absence of an opinion, not a fault. `matchesDeclared` is
false whenever nothing has been declared yet, so a client can use
`detectedScheme` as a suggestion and `matchesDeclared` as the warning.

Agreement is decided here rather than by comparing the two names on the client,
because the detector accepts aliases: reference data naming a scheme `Amex`
agrees with a detected `American Express`.

## GetById

Soft-deleted ranges are returned too, with `status: Deleted` - otherwise there
would be no way to look at one before deciding whether to restore it.

## Create

For the ranges a CSV does not cover - a one-off correction, or a range that arrives
on its own. Sample request:

    POST /api/BinRanges
    {
      "prefix": "400001",
      "cardScheme": "Visa",
      "productType": "Consumer",
      "fundingType": "Credit",
      "countryCode": "US",
      "validFrom": "2024-01-01",
      "validTo": null
    }

`cardScheme`, `productType`, `fundingType` and `countryCode` name existing reference
data, matched case-insensitively, exactly as in the CSV. Naming something that does
not exist is rejected rather than creating it. Leave `validTo` null for an
open-ended range.

The prefix must be free. One exception: if its only record was soft-deleted, that
row is revived with these values and the response comes back as `Restored` - the
prefix is unique across deleted rows too, so there is no second record to insert.

The range records the signed-in user as having added it, which is what the browse
listing's "added by" reports.

The prefix's leading digits are cross-checked against the declared `cardScheme`.
A mismatch is refused with `409 Conflict` and `status: SchemeMismatch` so the
caller can show the reason - the body's `error` names the network the digits belong
to. To save anyway (co-brand block, new allocation, deliberate correction), resend
with `acknowledgeSchemeMismatch: true`; then the row is stored as declared.

## Update

The whole range is replaced, so send every field - anything omitted is treated as
cleared, not left alone. The prefix may be changed as long as no other range owns
it. The range keeps its id, and the signed-in user is recorded as its last editor.

A soft-deleted range cannot be edited: restore it first, so bringing it back is
never a side effect of a correction.

The same prefix-vs-scheme cross-check as on insert: a mismatch is refused with
`409 Conflict` and `status: SchemeMismatch`. Resend with
`acknowledgeSchemeMismatch: true` to save it anyway.

## Delete

The delete is soft. The range stops matching classification and drops out of the
default listing, but the record stays - `GET /api/BinRanges?status=Deleted` still
finds it, and `POST /api/BinRanges/{id}/restore` brings it back with its history.

The prefix stays reserved while the range is deleted. Adding it again revives this
record rather than creating a second one.

## Restore

The range returns with the values it had when it was deleted, and starts matching
classification again from the moment it is restored, subject to its own dates.
