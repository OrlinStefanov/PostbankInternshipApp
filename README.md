# BinTool - Card BIN Classification & Commission Configuration Tool

A .NET 8 application for classifying payment card BINs (Bank Identification Numbers) and managing commission rules for card transactions. Built as an internship project to demonstrate full-stack development with ASP.NET Core Web API and Blazor Server UI.

## What it does

- **BIN Classification**: Detects card scheme and looks up detailed card attributes (product type, funding type, region) from a BIN database
- **Commission Management**: Stores and resolves tiered commission rules based on card attributes
- **Fee Calculation**: Calculates transaction fees based on classification and applicable commission rules
- **Bulk Processing**: Imports BIN data from CSV files and classifies cards in bulk
- **BIN Maintenance**: Lets an administrator add, edit, withdraw and restore individual BIN ranges without a CSV
- **Audit Trail**: Records every change to BIN data — before and after values, and who made it

## Who it's for

- **Bank administrators**: Manage BIN data and set pricing rules without touching code
- **Business users**: Look up card attributes and calculate fees
- **Developers**: Use the REST API to integrate BIN classification into other systems

## Technology Stack

- **.NET 8** - Application runtime
- **ASP.NET Core** - Web API and Blazor Server
- **Entity Framework Core** - ORM with SQLite for development
- **xUnit** - Automated testing
- **Swagger/OpenAPI** - API documentation

## Getting Started

### Prerequisites

- .NET 8 SDK or later
- Git
- Visual Studio 2022, VS Code with C# extension, or Rider

### Build

```bash
dotnet build BinTool.sln
```

### Run the API

```bash
cd BinTool.Api
dotnet run
```

The API starts at `https://localhost:5001` and `http://localhost:5000`.  
Swagger UI is available at `http://localhost:5000/swagger/ui/index.html` in development.

### Run the UI

```bash
cd BinTool.UI
dotnet run
```

### Run Tests

```bash
dotnet test BinTool.sln
```

## Project Structure

```
BinTool/
├── BinTool.Api/              # ASP.NET Core Web API
│   ├── Controllers/          # API endpoints
│   ├── Extensions/           # Dependency injection setup
│   ├── Program.cs            # Application entry point
│   └── appsettings.*.json    # Configuration
├── BinTool.Core/             # Business logic and models
│   ├── Entities/             # Domain models
│   └── Services/             # Business services
├── BinTool.Infrastructure/   # Data access layer
│   ├── Data/                 # Entity Framework DbContext
│   └── Repositories/         # Data access patterns
├── BinTool.Shared/           # Shared DTOs and constants
├── BinTool.UI/               # Blazor Server application
│   ├── Components/           # Blazor components
│   └── Pages/                # UI pages
├── BinTool.Tests/            # xUnit test suite
└── BinTool.sln               # Solution file
```

## Database Setup

The database is initialized on first run using EF Core migrations. Local SQLite databases are excluded from Git via `.gitignore`.

To manually create/update the database:

```bash
dotnet ef database update --project BinTool.Infrastructure
```

## Git Workflow

1. Create a feature branch: `git checkout -b feature/JIRA-123-short-description`
2. Make your changes and commit with descriptive messages
3. Push to origin and open a Pull Request
4. After review and approval, merge to `main` and delete the branch

The `main` branch is protected: direct pushes are blocked, and all merges require a reviewed Pull Request.

## API Documentation

### Endpoints

Implemented:

| Endpoint | Access |
|---|---|
| `GET /api/health` — Health check | Anonymous |
| `POST /api/auth/login` — Exchange credentials for a token | Anonymous |
| `GET /api/auth/me` — Identity behind the token | Any signed-in user |
| `POST /api/bin/classify` — Classify a BIN | Any signed-in user |
| `GET /api/binranges` — Browse stored BIN ranges | Any signed-in user |
| `GET /api/binranges/filters` — Reference values for filters | Any signed-in user |
| `GET /api/binranges/{id}` — One BIN range | Any signed-in user |
| `POST /api/binranges` — Add a BIN range by hand | **Admin** |
| `PUT /api/binranges/{id}` — Edit a BIN range | **Admin** |
| `DELETE /api/binranges/{id}` — Withdraw a BIN range (soft) | **Admin** |
| `POST /api/binranges/{id}/restore` — Bring a withdrawn range back | **Admin** |
| `POST /api/BinCsvImport/import` — Import a CSV | **Admin** |
| `GET /api/BinCsvImport/conflicts` — Conflicts awaiting a decision | **Admin** |
| `POST /api/BinCsvImport/resolve-conflicts` — Apply decisions | **Admin** |

Planned:

- `POST /api/fees/calculate` — Calculate transaction fee
- `GET /api/commissions/rules` — List all commission rules

Full documentation is available in Swagger UI when running the API in development mode.

### Authentication

The API uses **JWT bearer tokens**. `POST /api/auth/login` exchanges credentials for a signed
token; send it on every other call as `Authorization: Bearer <accessToken>`. The token carries
the user's roles and expires after 60 minutes (`Jwt:ExpiryMinutes`). There is no refresh token —
when it expires, log in again.

```bash
curl -k -X POST https://localhost:7258/api/auth/login -H "Content-Type: application/json" -d "{\"userName\":\"admin\",\"password\":\"Admin@123\"}"
```

In Swagger UI, use the **Authorize** button and paste the `accessToken` — Swagger adds the
`Bearer ` prefix itself.

#### If a correct password stops working

**Five failed sign-ins lock the account for five minutes.** During that window the *right*
password is refused too, and — because every rejection has to read the same, or the response
would reveal which accounts exist — it is refused with the same message. Credentials that were
working therefore appear to have gone bad.

Retrying does not extend the lockout, but it does not clear it either. Wait it out. The API logs
a warning naming the account and when the lockout ends, which is the only place it is visible:

```
warn: Sign-in refused for admin: the account is locked out until 2026-08-05 08:13:11Z.
```

Both numbers are set explicitly in `ServiceExtensions.AddApplicationServices`.

#### Why a session ends after an hour

There is no refresh token and no sliding expiration, both on purpose. The consequences are worth
knowing before they look like bugs:

- The token lasts **60 minutes** from sign-in (`Jwt:ExpiryMinutes`). An hour of inactivity ends
  the session, and the next page load goes to `/login`.
- The UI cookie's lifetime is pinned to the token's expiry, so the two always end together.
- The cookie is **not persistent**, so closing the browser signs you out regardless of the hour
  remaining.

Restarting the API or the UI does *not* sign you out: the Data Protection key ring that encrypts
the cookie is persisted per user under `%LOCALAPPDATA%\ASP.NET\DataProtection-Keys`, so the
cookie still decrypts against a fresh process.

Roles are enforced by the API, not by the client: reading (classify, browse) is open to any
signed-in user, while anything that writes BIN data — importing, resolving conflicts, and
adding, editing, deleting or restoring a range — requires **Admin**. A `403` means the token is
valid but the role is not enough; a `401` means no token, or an expired one.

#### Demo accounts

Created automatically on an empty database so the app can be signed into straight after a clone.

| User name | Password | Role | Can do |
|---|---|---|---|
| `admin` | `Admin@123` | Admin | Everything: CSV import, conflict resolution, and maintaining BIN ranges by hand |
| `viewer` | `Viewer@123` | Viewer | Browse BIN ranges and classify BINs; cannot change anything |

> **These are development credentials, published here on purpose so the project runs out of the
> box.** They are not suitable for any shared or deployed environment. Existing accounts are never
> overwritten, so changing a password sticks across restarts. Set `Seed:DemoUsers` to `false` to
> skip seeding entirely, or override per role with `Seed:Admin:UserName` / `Seed:Admin:Password`
> (and the same under `Seed:Viewer`).

#### Signing key

`Jwt:Key` signs the tokens. A development-only key is committed in
`BinTool.Api/appsettings.Development.json` so the project runs after a clone — it is not a secret.
Anywhere else, supply your own via user secrets or an environment variable:

```bash
dotnet user-secrets set "Jwt:Key" "<at least 32 bytes of random text>" --project BinTool.Api
```

The API refuses to start if `Jwt:Key` is missing or shorter than 32 bytes, rather than issuing
tokens that are cheap to forge.

#### How the UI holds the token

The Blazor UI signs in against the API and stores the returned JWT as a claim inside **its own
authentication cookie** — encrypted by ASP.NET Core Data Protection, `HttpOnly` so no script can
read it, and `Secure` over HTTPS. That is deliberately not local storage, which any injected
script could read. The cookie's lifetime is set to the token's own expiry, so the session cannot
outlive the token it carries.

Pages are guarded by `AuthorizeRouteView`, so typing a URL is refused the same way a hidden link
is: a signed-out visitor is sent to `/login` and returned afterwards, and a signed-in user
without the role gets `/access-denied`. Nav links are hidden to match, but hiding is presentation
— the route guard and the API's own role checks are the control.

### BIN classification

`POST /api/bin/classify` takes `{ "bin": "400001" }` and returns the card scheme, product
type, funding type, issuing country and region.

The lookup is a **longest-prefix match**: an 8-digit range is more specific than the 6-digit
range it sits inside, so it wins; failing that the 7-digit range is tried, then the 6-digit
one. Only ranges valid today are considered — expired, not-yet-started and deleted ranges are
skipped, and a shorter range may match in their place. No scheme ranges are hard-coded; every
answer comes from imported data.

A full card number may be sent instead of a BIN. Only the leading 8 digits are used — the rest
is discarded before the lookup runs, is never stored or logged, and the response echoes back
only the truncated value.

When no range covers the BIN the response is still `200` with `"matched": false` and the card
attributes null. A malformed BIN (non-digits, or outside 6–19 digits) returns `400`.

### Browsing BIN ranges

`GET /api/binranges` lists what is stored, filtered and paged. Filters are optional and combine
with AND: `prefix` (starts-with), plus `cardScheme`, `productType`, `fundingType` and
`countryCode` matched case-insensitively.

Each row carries a `status` derived from its dates and delete flag rather than stored, so it
can't fall out of step with them:

| Status | Meaning |
|---|---|
| `Active` | Valid today — this is what classification will match. |
| `Scheduled` | `validFrom` is in the future. |
| `Expired` | `validTo` has passed. |
| `Deleted` | Soft-deleted. |

`status=<value>` narrows to one of those. Left unset, deleted ranges are excluded and everything
else is returned — so `status=Deleted` is the only way to see them. `pageSize` is capped at 200,
and `totalCount` counts every match rather than just the page, so a client can render a pager
without a second call.

Every row also reports **who added it** (`createdBy`, `createdAt`) and who last changed it
(`updatedBy`, `updatedAt`) — normally the user who ran the import, and whoever later applied a
conflict over it. The UI shows this as an **Added by** column, with the last change on hover.

A `createdBy` of `system` is not an account: it means no user was signed in when the row was
written, which is how anything imported before authentication existed is recorded. The UI renders
it as a *System* badge rather than a user name.

`GET /api/binranges/filters` returns the card schemes, product types, funding types and
countries currently in the database, so a client populates its dropdowns from data.

The Blazor UI exposes this at **`/bin-ranges`**.

### Maintaining BIN ranges by hand

A CSV is the right tool for a batch from a scheme. It is the wrong tool for a single correction,
a range that arrives on its own, or withdrawing one that should no longer match. **Admins** can do
those one at a time, from the same `/bin-ranges` page — an **Add BIN range** button, and **Edit**,
**Delete** and **Restore** per row. A viewer sees none of these controls, and the API refuses them
with a `403` regardless of what the client shows.

| Action | Endpoint | Result |
|---|---|---|
| Add | `POST /api/binranges` | `201` with the stored range |
| Edit | `PUT /api/binranges/{id}` | `200` with the stored range |
| Withdraw | `DELETE /api/binranges/{id}` | `200`; the range is soft-deleted |
| Restore | `POST /api/binranges/{id}/restore` | `200` with the range back |

The body names reference data exactly as a CSV row does — `cardScheme`, `productType`,
`fundingType`, `countryCode` — matched case-insensitively against what already exists. Naming
something that does not exist is rejected rather than created, and all the bad names come back at
once rather than one per attempt:

```bash
curl -k -X POST https://localhost:7258/api/binranges \
  -H "Authorization: Bearer <accessToken>" -H "Content-Type: application/json" \
  -d "{\"prefix\":\"400009\",\"cardScheme\":\"Visa\",\"productType\":\"Consumer\",\"fundingType\":\"Credit\",\"countryCode\":\"BG\",\"validFrom\":\"2024-01-01\"}"
```

Rules worth knowing:

- **`PUT` replaces the whole range.** Send every field; anything omitted is cleared, not left
  alone. The range keeps its id, and the signed-in user is recorded as its last editor.
- **The prefix can be changed**, as long as no other range owns it. `prefixLength` is derived from
  it, never supplied.
- **Deletes are soft.** The range stops matching classification and drops out of the default
  listing, but nothing is erased — `?status=Deleted` finds it and restore brings it back with the
  values it had.
- **A deleted range cannot be edited.** Restore it first, so bringing it back is never an
  accidental side effect of a correction.
- **A deleted prefix stays reserved.** The unique index on `prefix` spans deleted rows, so adding
  that prefix again revives the existing record with the new values and answers `Restored` rather
  than creating a second one. This is the same rule the CSV import follows.

Every write answers with the same body — a `status`, an `error` when it was refused, and the
stored `range` when it was not — so one shape covers `200`, `201`, `400`, `404` and `409`. A
refused write is reported, not thrown: `409` for a prefix already in use or a range already in
that state, `400` for a broken rule or an unknown reference name, `404` for an id that is not
there.

### Audit trail

Every change to live BIN data writes an `AuditEntry` recording **what changed, from what, to
what, by whom and when**:

| Column | Holds |
|---|---|
| `entityType`, `entityId` | Which row changed — `BinRange` plus its id |
| `action` | `Created`, `Updated`, `Deleted` or `Imported` |
| `oldValues` | JSON snapshot before the change; null for an insert |
| `newValues` | JSON snapshot after |
| `performedByUserId` | The account that made it, or null if nobody was signed in |
| `performedAt` | When |

The snapshots hold reference data **by name** and dates as `yyyy-MM-dd`, because an audit trail
is read by people — and the names are copied at the time of the change, so an entry keeps saying
what the range was even if the reference data is renamed later:

```json
{"prefix":"888777","cardScheme":"Visa","productType":"Consumer","fundingType":"Credit",
 "countryCode":"US","validFrom":"2024-01-01","validTo":null,"isDeleted":false}
```

What is recorded, and what deliberately is not:

| Event | Entry |
|---|---|
| Range added by hand | `Created` — no old values |
| Range edited | `Updated` — both sides |
| Range deleted / restored | `Deleted` / `Updated` — the snapshots differ only in `isDeleted` |
| Row inserted by an import | `Imported`, so the trail tells a file apart from a hand edit |
| Import revives a deleted prefix | `Imported`, with the values it replaced |
| Conflict applied | `Updated` — the values the import overwrote, and the ones it wrote |
| Conflict discarded | *None.* No BIN data changed; who decided and when is on the conflict itself |
| Row rejected or unchanged by an import | *None.* Rejections are kept, with reasons, on the import history |
| A refused write (`400`/`404`/`409`) | *None.* Nothing changed, so there is nothing to account for |

Entries are written in the **same transaction** as the change they describe. An insert has no id
until it is saved, so those paths save twice inside one transaction rather than letting a range
commit without its audit row — a silently incomplete trail is the one failure an audit trail
cannot have.

There is no read endpoint for the trail yet; query the `AuditEntries` table directly.

## CSV Import Format

### BIN Import

The file must have a header row, be comma-delimited, and use UTF-8 encoding.

| # | Column | Required | Type | Maps to | Validation |
|---|---|---|---|---|---|
| 1 | `Prefix` | Yes | string | `BinRange.Prefix` | 6–8 digits, unique |
| 2 | `CardScheme` | Yes | string | `CardScheme.Name` (lookup) | Must match an existing scheme name |
| 3 | `ProductType` | Yes | string | `ProductType.Name` (lookup) | Must match an existing product type name |
| 4 | `FundingType` | Yes | string | `FundingType.Name` (lookup) | Must match an existing funding type name |
| 5 | `CountryCode` | Yes | string | `Country.IsoCode` (lookup) | ISO 3166-1 alpha-2, must exist in `Countries` |
| 6 | `ValidFrom` | Yes | date | `BinRange.ValidFrom` | Format `yyyy-MM-dd` |
| 7 | `ValidTo` | No | date | `BinRange.ValidTo` | Format `yyyy-MM-dd`, blank = open-ended |

`PrefixLength` is not a CSV column — it is derived from `Prefix.Length` during import.

**Currently valid lookup values** (seeded reference data):

- `CardScheme`: `Visa`, `Mastercard`, `American Express`, `Diners Club`
- `ProductType`: `Consumer`, `Commercial`, `Prepaid`
- `FundingType`: `Credit`, `Debit`
- `CountryCode`: `BG`, `AT`, `BE`, `FR`, `DE`, `IT`, `ES`, `NL`, `SE`, `GB`, `US`, `CA`, `JP`, `CN`

Example:
```csv
Prefix,CardScheme,ProductType,FundingType,CountryCode,ValidFrom,ValidTo
456123,Mastercard,Consumer,Credit,US,2024-01-01,
451234,Mastercard,Commercial,Debit,BG,2024-01-01,2026-12-31
```

A sample 20-row file is available at [`samples/bin_import_sample.csv`](samples/bin_import_sample.csv).

### Import outcomes

Every data row lands in exactly one of four outcomes. A bad row never fails the whole file.

| Outcome | Meaning | Recorded in |
|---|---|---|
| **Inserted** | The prefix is new. A prefix whose only record was soft-deleted is revived in place rather than duplicated, and counts here. | `BinRange`; `ImportHistory.ImportedRows` |
| **Unchanged** | The prefix exists and every mapped column already matches. Skipped — nothing is written. | Counted only, in the import result |
| **Conflict** | The prefix exists with at least one differing column. Nothing is overwritten; the row is staged for a decision. | `PendingBinConflict` (`Status = Pending`) |
| **Rejected** | Unknown lookup value, invalid prefix length, bad date format, or a duplicate of an earlier row in the same file. | `RejectedImportRow` (reason + raw row); `ImportHistory.RejectedRows` |

Totals are summarized in `ImportHistory` (`ImportedRows`, `UpdatedRows`, `RejectedRows`, `Status`).
`Status` is `Failed` only when every row was rejected. Inserts, revivals, rejections and staged
conflicts are written in a single save, so an import run is all-or-nothing at the database level.

### Duplicate prefixes: the conflict workflow

A prefix that already exists but carries different values is neither rejected nor silently
overwritten — the import stages it and a person decides.

1. The import writes the incoming values to `PendingBinConflict` with the id of the BIN range it
   would overwrite and the raw CSV line. The stored record is left untouched.
2. `GET /api/BinCsvImport/conflicts` returns the outstanding conflicts, each with a per-field diff
   (field, current value, incoming value). The diff is recomputed on read, so it stays accurate if
   the stored record changed after the import.
3. `POST /api/BinCsvImport/resolve-conflicts` applies one decision per conflict — `update` writes the
   imported values onto the existing record, `discard` keeps the stored one. Either way the conflict
   is closed and stops being returned.

Because conflicts are persisted rather than held in memory, the review list survives a page reload or
a restart; the file does not have to be uploaded again. Resolution is idempotent — unknown or
already-resolved ids are counted under `notFoundCount` instead of failing the request, so a batch can
safely be retried.

The full column and validation spec, including the decisions pending mentor sign-off, is in
[`samples/BIN_Import_CSV_Layout.docx`](samples/BIN_Import_CSV_Layout.docx).

## Development Notes

- **No BIN data is hard-coded** — all reference data is stored in the database
- **No full card numbers are stored** — the tool works with BIN prefixes only (max 8 digits)
- **No card data is logged** — all logging excludes sensitive information
- **Program.cs stays clean** — dependency injection and middleware setup is delegated to `ServiceExtensions.cs`

## Known Limitations

- Demo accounts and the JWT signing key are committed for convenience — both must be replaced
  before this runs anywhere shared (see [Authentication](#authentication))
- There is no refresh token, no password reset and no self-registration; accounts are seeded
- The audit trail covers BIN ranges only; commission rules and reference data are not audited yet
- There is no endpoint or screen for reading the audit trail — the rows are written, but only
  reachable by querying `AuditEntries` directly
- There is no optimistic concurrency check on a hand edit: two admins editing the same range at
  once, last write wins
- The application uses ASP.NET Core built-in authentication for internship purposes
- Production deployment would require integration with the corporate identity provider
- Bulk classification files are limited to reasonable sizes (currently tested up to 1000 rows)

## Next Steps After Internship

To take this tool to production:

1. Replace demo authentication with corporate identity provider (Azure AD, etc.)
2. Add comprehensive logging and monitoring
3. Migrate from SQLite to production database (SQL Server, PostgreSQL)
4. Set up CI/CD pipeline for automated testing and deployment
5. Implement API rate limiting and caching
6. Add comprehensive audit logging for compliance

## License

This project is the property of [Bank Name]. Unauthorized copying or distribution is prohibited.

## Internship Context

**Duration**: 4 weeks (23 July – 21 August 2026)  
**Intern**: Orlin Stefanov  
**Stack**: .NET 8, ASP.NET Core, Blazor Server, EF Core, xUnit, Swagger  
**Repository**: Corporate GitHub Enterprise  

See the Jira backlog for detailed story acceptance criteria and technical task breakdowns.
