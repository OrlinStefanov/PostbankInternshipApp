## Search

Expired rules are hidden by default; pass `includeExpired=true` to keep them in
the listing (visually distinguished by their `Expired` status). Soft-deleted
rules only appear when `includeDeleted=true`.

## Create

A null scheme, product or region is a wildcard. The validity window must not overlap
an existing rule that shares the same key - the save is refused with 409 and the
conflicting rule is named.

