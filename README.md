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

- `GET /health` - Health check endpoint (returns HTTP 200 if running)
- `POST /api/bin/classify` - Classify a BIN
- `POST /api/fees/calculate` - Calculate transaction fee
- `GET /api/commissions/rules` - List all commission rules

Full documentation is available in Swagger UI when running the API in development mode.

## CSV Import Format

### BIN Import

Columns: `prefix`, `scheme`, `product_type`, `funding_type`, `country_code`

Example:
```
prefix,scheme,product_type,funding_type,country_code
456123,Mastercard,Consumer,Credit,US
451234,Mastercard,Commercial,Debit,BG
```

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
