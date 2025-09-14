# FinApp – AI Assistant Working Notes

Purpose: give AI coding agents the context and conventions needed to be productive in this repo without guesswork.

## Core directives
- Be concise: prefer brevity over verbosity.
- Be pragmatic: prefer practical solutions over theoretical purity.
- Be consistent: follow existing patterns unless there’s a strong reason not to.
- Prioritize clarity: write code that is easy to read and understand.
- Favor maintainability: write code that is easy to modify and extend.
- Always make sure to not break existing functionality unless explicitly asked to refactor or improve it.

- For expense section, the core page is the expense dashboard
- When implementing a new feature always summarize the requirement and ask for confirmation before proceeding.

## Architecture Overview
- App: ASP.NET Core 8 Razor Pages app in `InvestmentTracker/` with EF Core 8 + PostgreSQL.
- Data: EF Core models in `InvestmentTracker/Models`, DbContext in `Data/AppDbContext.cs`, migrations in `Data/Migrations`.
- Services: Business logic via DI services (`Services/InvestmentService.cs`, `Services/ExpenseService.cs`, `Services/CsvImportService.cs`). Pages call services, not DbContext directly (except a few query-heavy pages).
- UI: Razor Pages under `Pages/` with Bootstrap 5 (Darkly theme). Global JS/CSS in `wwwroot/`. Client helpers in `wwwroot/js/site.js` (validation tweaks, keyboard shortcuts, toasts, loading states, animations).
- API: Minimal API endpoints in `Endpoints/InvestmentApi.cs` (investments + values) for scripts/imports.
- Deployment: Dockerized via `deployment/` with a convenience wrapper `./finapp` at repo root. Production host runs `postgres:16-alpine` + app container.

## Key Pages Structure
- **Main Dashboard**: `Pages/Index.cshtml` - Quick expense tracking overview
- **Expenses**: 
  - `Pages/Expenses/Index.cshtml` - Monthly expense dashboard with quick entry
  - `Pages/Expenses/Regular.cshtml` - Regular expense management  
  - `Pages/Expenses/Budgets.cshtml` - Budget management and tracking
  - Category and family member management pages
- **Investments**: 
  - `Pages/Investments/List.cshtml` - Investment list and management
  - `Pages/Investments/Edit.cshtml` - Investment details, schedules, contributions
- **Portfolio**: `Pages/Portfolio/Index.cshtml` - Portfolio overview with charts
- **Values**: `Pages/Values/Index.cshtml` - Cross-investment value tracking

## Core Domain Concepts
- Investments: `Investment`, `InvestmentValue`, `InvestmentType` (OneTime vs Recurring), `InvestmentCategory`, `ValueChangeType`, `OneTimeContribution`.
- Expenses: `RegularExpense` (+ `ExpenseSchedule` with month-based logic), `IrregularExpense`, `ExpenseCategory`, `MonthlyIncome`, `IncomeSource`, `FamilyMember`, `OneTimeIncome`.
- Budgets: `CategoryBudget` with date range support and validation.
- Analytics: `CategoryExpenseData`, `MonthlyExpenseData`, `CategoryComparisonData`, `MonthlyComparisonData` for expense analytics.
- Scheduling: `FinancialScheduleConfig`, `Frequency` for configurable financial schedules.
- Currency: `Currency` enum + `.ToCultureCode()` extension for formatting.

## Important Patterns
- Razor PageModels call services; keep DB logic in `Services/` unless there is a deliberate performance reason (see `Pages/Values/Index.cshtml.cs`).
- TempData + Toasts: Success messages are set via `TempData["ToastSuccess"]` in POST handlers; `_Layout.cshtml` reads and displays with `Toast.success(...)`.
- Decimals & Culture: Client-side number/range validation accepts comma separators; server-side uses `InvariantDecimalModelBinder*` to normalize commas to dots.
- Keyboard shortcuts: Centralized in `wwwroot/js/site.js` with a guard to ignore modifier keys (Ctrl/Cmd/Alt/Shift). `?` opens help overlay. ESC closes modals/overlays.
- Portfolio charts: Chart.js wired in `Pages/Portfolio/Index.cshtml`. Zero-total currencies are filtered out to avoid empty charts.
- Loading states: `LoadingManager` in `site.js` provides overlay, button loading, skeleton screens, and animations.
- CSV Import: `CsvImportService` handles investment portfolio CSV imports with automatic investment creation and value mapping.

## Data Access & Calculations
- Investment “Invested” amounts:
  - Recurring: sum schedule months up to the value date plus one-time contributions; subtract `ChargeAmount` if present.
  - OneTime: sum one-time contributions up to date; if none, fall back to earliest recorded value; subtract `ChargeAmount`.
- Values page performance: Avoid N+1 by grouping queries (see `Pages/Values/Index.cshtml.cs` for earliest-value join pattern).

## Developer Workflows
- Local Run:
  - Requires PostgreSQL. Quick start (Docker):
    ```bash
    docker run --name postgres-finapp -e POSTGRES_PASSWORD=<YOUR_PASSWORD> -e POSTGRES_DB=finapp_dev -e POSTGRES_USER=finapp -p 5432:5432 -d postgres:16-alpine
    cd InvestmentTracker
    dotnet run
    ```
- Migrations:
  ```bash
  cd InvestmentTracker
  dotnet ef migrations add <Name> -o Data/Migrations
  dotnet ef database update
  ```
- Build & Lint: Standard `dotnet build`. No custom lint/format hooks.
- Tests: No test project present.

## Deployment (Production)
- Use the wrapper at repo root:
  ```bash
  ./finapp check misa@192.168.88.27   # status
  ./finapp assess misa@192.168.88.27  # inspect current deployment
  ./finapp deploy misa@192.168.88.27  # regular deploy (preserves data)
  ./finapp fresh misa@192.168.88.27   # DANGEROUS: nukes data
  ```
- Under the hood: builds the image on the host using `deployment/Dockerfile` and `docker-compose.yml`. App listens on `:5000`, Postgres on `:5432`.
- App auto-applies EF migrations on startup.

## Conventions & Tips
- Keep UI messages via `TempData["ToastSuccess"]`/`ToastError` to align with `_Layout.cshtml` toast renderer.
- Use currency-aware formatting: `ToString("C", CultureInfo.CreateSpecificCulture(currency.ToCultureCode()))`.
- For new keyboard shortcuts, register through `KeyboardShortcutsManager` in `site.js`. Always ignore modifier keys.
- Loading states: Use `LoadingManager` for overlays, button loading, skeleton screens, and animations.
- API payloads expect enums as integers (import scripts do this). See `Import-InvestmentPortfolio.ps1` for mapping examples.
- When adding values, prevent future dates; prevent duplicate `InvestmentValue` for same `AsOf` (see `Pages/Investments/Values.cshtml.cs`).

## Where to Look (Examples)
- Values page invested calc and query pattern: `Pages/Values/Index.cshtml.cs`.
- Portfolio summary + charts: `Pages/Portfolio/Index.cshtml*`.
- Investment edit + schedules + contributions: `Pages/Investments/Edit.cshtml*`.
- Budget management: `Pages/Expenses/Budgets.cshtml*`.
- CSV import functionality: `Services/CsvImportService.cs`.
- Client behavior (toasts, shortcuts, validation, loading states): `wwwroot/js/site.js`.
- Deployment scripts & docs: `./finapp`, `deployment/*.sh`, `deployment/README.md`.

## Gotchas
- DataProtection keys in prod are ephemeral; expect occasional antiforgery warnings in logs (non-blocking).
- Use `TempData` for post-redirect messaging; inline ModelState errors are fine when staying on the same page.
- Portfolio page now hides currencies where total is 0; ensure ChartDataJson mirrors that.

If anything here is unclear or outdated, tell me where you got stuck and I’ll refine this doc.
