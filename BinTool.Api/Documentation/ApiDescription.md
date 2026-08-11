Card BIN classification and commission configuration.

**Importing BIN ranges** is a two-step workflow:

1. `POST /api/BinCsvImport/import` uploads a CSV. Rows are sorted into four outcomes: new prefixes are inserted, invalid rows are recorded as rejected, rows identical to the stored record are skipped, and rows whose prefix already exists with *different* values are staged as conflicts rather than overwriting anything.
2. `POST /api/BinCsvImport/resolve-conflicts` applies a decision to each staged conflict - overwrite the stored record, or keep it and discard the imported row.

Conflicts are persisted, so `GET /api/BinCsvImport/conflicts` can rebuild the outstanding worklist at any time (for example after the client reloads).

**Classifying a BIN** with `POST /api/Bin/classify` matches it against the imported ranges, longest prefix first, and returns the card scheme, product type, funding type, issuing country and region. No scheme ranges are hard-coded - every answer comes from data in the database. Pass an optional `amount` to also get the commission: the most specific rule that matches the card wins, falling back to the configured default rule, and the fee is percentage + fixed amount raised to a minimum.

**Configuring commission** is `GET/POST/PUT/DELETE /api/CommissionRules`, plus `POST /api/CommissionRules/{id}/default` to set the fallback rule. A rule is keyed on a scheme/product/funding/region combination where any field may be a wildcard, and a save that overlaps an existing rule's validity on the same key is refused.

**Browsing what is stored** is `GET /api/BinRanges`, which filters and pages the BIN ranges and reports each one's status (active, scheduled, expired or deleted). `GET /api/BinRanges/filters` returns the reference values to filter by.

**Maintaining ranges one at a time** covers what a CSV does not: `POST`, `PUT` and `DELETE` on `/api/BinRanges` add, edit and withdraw a single range, and `POST /api/BinRanges/{id}/restore` brings a withdrawn one back. Deletes are soft, so nothing is erased and every change records who made it.

**Authentication.** Every endpoint except `POST /api/Auth/login` and `GET /api/Health` needs a bearer token. Call login, then use the **Authorize** button above with the `accessToken` it returns.

**Authorization is permission-based.** Each endpoint requires a permission (`binranges.read`, `binranges.write`, `binranges.import`, `bin.classify`, `roles.manage`, `audit.read`, `referencedata.manage`, `commissionrules.read`, `commissionrules.write`, `currencies.read`), granted by holding a role that carries it. An admin composes roles from these permissions and assigns them to users via `/api/roles` and `/api/users`. The **Admin** role is a superuser that holds every permission and is protected from being weakened. Permissions are baked into the token, so a change to a role takes effect the next time the affected user signs in.
