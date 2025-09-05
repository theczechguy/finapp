# FinApp / InvestmentTracker

A self‑hosted investment portfolio tracker built with ASP.NET Core 8 Razor Pages and EF Core (SQLite). It lets you define investments, record their values over time, analyze history, and exposes minimal APIs for future integrations.

## Tech Stack
- .NET 8, ASP.NET Core Razor Pages
- EF Core 8 + SQLite
- Bootstrap (from template), jQuery + unobtrusive validation

## Current Features
- Investments
  - Create and edit investments:
    - Name (required)
    - Provider (e.g., brokerage/bank)
    - Type: OneTime or Recurring (required)
    - RecurringAmount (decimal; used when Type=Recurring)
    - Currency: CZK, EUR, USD (required)
  - List page shows Provider, Type, Currency, RecurringAmount, latest value, and links to manage values or edit the investment
- Values
  - Per‑investment values page: view history, add value entries (date + amount), delete entries
  - Global values page:
    - Provider as the first column
    - Sorting by Provider, Investment, Date, Value (with direction indicators)
    - Filters by Provider, Investment, and date range
    - Pagination with configurable page size
- Minimal APIs (under `/api`) for investments and values
- Error handling & routing
  - Development: detailed exception page
  - Production: friendly `/Error` and 404 `/NotFound`
  - Root `/` redirects to `/Investments`
- Culture‑friendly decimal input (supports comma or dot as decimal separator)

## Data Model
- Investment
  - Id: int
  - Name: string (required)
  - Provider: string? (max length 200)
  - Type: InvestmentType (OneTime|Recurring)
  - RecurringAmount: decimal?
  - Currency: Currency (CZK|EUR|USD)
  - Values: ICollection<InvestmentValue>
- InvestmentValue
  - Id: int
  - InvestmentId: int
  - AsOf: DateTime
  - Value: decimal
  - Unique index on (InvestmentId, AsOf)
- Enums
  - InvestmentType { OneTime, Recurring }
  - Currency { CZK, EUR, USD }

## Pages
- `/Investments` — list investments; links to create, edit, and manage values
- `/Investments/Create` and `/Investments/Edit/{id}` — forms for investment definition
- `/Investments/Values/{id}` — manage values for a specific investment (add/delete, view history)
- `/Values` — global values table with sorting, filters, and pagination
- Shared: navbar has links to Investments and Values; root redirects to `/Investments`

## API Endpoints
Base path: `/api`
- `GET /api/investments` — list investments (id, name, provider, type, currency, recurringAmount)
- `GET /api/investments/{id}` — single investment including values
- `POST /api/investments` — create investment
- `PUT /api/investments/{id}` — update investment
- `DELETE /api/investments/{id}` — delete investment
- `GET /api/investments/{id}/values` — list values for investment (ordered by date)
- `POST /api/investments/{id}/values` — add a value entry

## Decimal Localization
- Client: jQuery Validation overridden to accept commas for decimals
- Server: custom model binder normalizes commas to dot before parsing
- Inputs use `inputmode="decimal"` and `step="0.01"`

## Persistence & Migrations
- SQLite database configured via `ConnectionStrings:DefaultConnection` in `appsettings.json`
- Migrations are applied at startup (`Database.Migrate()` in `Program.cs`)
- To add a new migration and update the DB:
  ```bash
  cd InvestmentTracker
  # optional if not installed:
  # dotnet tool install --global dotnet-ef
  dotnet ef migrations add <MigrationName> -o Data/Migrations
  dotnet ef database update
  ```

## Project Structure (trimmed)
```
FinApp.sln
InvestmentTracker/
  Program.cs
  Data/
    AppDbContext.cs
    Migrations/
  Models/
    Investment.cs, InvestmentValue.cs, InvestmentType.cs, Currency.cs
  Infrastructure/
    InvariantDecimalModelBinder*.cs
  Pages/
    Investments/ (Index, Create, Edit, Values)
    Values/ (Index)
    Shared/_Layout.cshtml
    Error.cshtml, NotFound.cshtml
  wwwroot/
    css, js/site.js, lib
```

## Run Locally
```bash
cd InvestmentTracker
 dotnet restore
 dotnet build
 dotnet run
```
- Use the URLs printed on startup (or see `Properties/launchSettings.json`)
- Navigate to `/Investments` or `/Values`

## Roadmap Ideas
- Charts for value history (per investment and aggregate)
- CSV/JSON export for global values with current filters
- Validation: ensure `RecurringAmount` required when `Type=Recurring`
- Delete investment (with cascade) and optional soft‑delete
- Currency‑aware formatting per selected currency
- Authentication and API keys (if needed)

---
This README tracks the current implemented state and serves as quick context for contributors and LLMs.
