# BinTool - Card BIN Classification & Commission Configuration Tool

A .NET 8 application for classifying payment card BINs (Bank Identification Numbers) and managing commission rules for card transactions. Built as an internship project to demonstrate full-stack development with ASP.NET Core Web API and Blazor Server UI.

## What it does

- **BIN Classification**: Detects card scheme and looks up detailed card attributes (product type, funding type, region) from a BIN database
- **Commission Management**: Stores and resolves tiered commission rules based on card attributes
- **Fee Calculation**: Calculates transaction fees based on classification and applicable commission rules
- **Bulk Processing**: Imports BIN data from CSV files and classifies cards in bulk
- **Audit Trail**: Records all configuration changes for compliance

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

- `GET /api/health` — Health check (returns HTTP 200 if running)
- `POST /api/bin/classify` — Classify a BIN
- `POST /api/BinCsvImport/import` — Import a CSV of BIN ranges
- `GET /api/BinCsvImport/conflicts` — List the conflicts awaiting a decision
- `POST /api/BinCsvImport/resolve-conflicts` — Apply update/discard decisions

Planned:

- `POST /api/fees/calculate` — Calculate transaction fee
- `GET /api/commissions/rules` — List all commission rules

Full documentation is available in Swagger UI when running the API in development mode.

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
