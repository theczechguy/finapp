# FinApp / InvestmentTracker

A comprehensive financial tracking application built with ASP.NET Core 8, featuring expense tracking, investment portfolio management, and budgeting capabilities with Docker deployment support.

## Tech Stack
- .NET 8, ASP.NET Core Razor Pages
- EF Core 8 with PostgreSQL
- Bootstrap 5, jQuery + unobtrusive validation
- Docker & Docker Compose for containerized deployment

## Current Features

### Investment Management
- Create and edit investments with comprehensive details:
  - Name, provider, type (OneTime/Recurring), recurring amount, currency
  - Investment categories and types for better organization
  - Value tracking over time with historical analysis
- Portfolio overview with latest values and performance metrics
- Global values view with sorting, filtering, and pagination

### Expense Tracking
- **Regular Expenses**: Monthly, quarterly, semi-annual, and annual recurring expenses
- **Irregular Expenses**: One-time or sporadic expense tracking
- **Expense Categories**: Organized categorization for better budgeting
- **Frequency Handling**: Smart scheduling system for different payment frequencies
- **Visual Indicators**: Clear badges and icons for alternative schedules (quarterly, semi-annual, annual)

### Budgeting & Planning
- Category-based budget management
- Monthly income tracking with multiple sources
- Expense scheduling with month-based calculation logic
- Budget vs. actual expense analysis

### User Interface
- **Responsive Design**: Works seamlessly on desktop and mobile devices
- **Keyboard Shortcuts**: Quick navigation and actions (Ctrl+N for new items, etc.)
- **Modal Management**: Centered modals with consistent styling
- **Alternative Schedule Indicators**: Visual cues for non-monthly frequencies
- **Dedicated Management Pages**: Comprehensive CRUD operations for all entities

### Technical Features
- Minimal APIs for programmatic access
- Culture-friendly decimal input (supports comma or dot)
- Database migrations with automatic application
- Error handling with friendly pages
- Docker containerization for easy deployment

## Data Model

### Core Entities
- **Investment**: Portfolio tracking with providers, types, currencies, and value history
- **InvestmentValue**: Time-series data for investment performance
- **RegularExpense**: Recurring expenses with frequency support (Monthly, Quarterly, Semi-Annual, Annual)
- **IrregularExpense**: One-time or sporadic expenses
- **ExpenseCategory**: Categorization for budget organization
- **CategoryBudget**: Budget allocations per category
- **MonthlyIncome**: Income tracking with multiple sources
- **FamilyMember**: Family-based expense and income attribution

### Frequency System
- **ExpenseSchedule**: Month-based calculation logic for recurring expenses
- **Frequency Enum**: Monthly, Quarterly, SemiAnnual, Annual
- **ContributionSchedule**: Investment contribution planning

### Database Support
- **PostgreSQL**: Production-ready database with full ACID compliance
- **High Performance**: Optimized for concurrent access and complex queries
- **Scalability**: Handles growing data volumes efficiently

## Pages & Navigation
- **Dashboard** (`/`) — Portfolio overview and recent activity
- **Investments** (`/Investments`) — Investment portfolio management
- **Portfolio** (`/Portfolio`) — Portfolio analysis and performance metrics
- **Regular Expenses** (`/Expenses/Regular`) — Comprehensive recurring expense management
- **Irregular Expenses** (`/Expenses/Irregular`) — One-time expense tracking
- **Values** (`/Values`) — Global investment values with advanced filtering
- **Budgets** (`/Budgets`) — Budget planning and category management

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

## Docker Deployment

FinApp is designed for production deployment with PostgreSQL. All deployment tools are organized in the `deployment/` directory.

### Quick Deployment Commands
```bash
# Check what's currently deployed
./finapp assess user@your-docker-host

# Regular deployment (preserves data)
./finapp deploy user@your-docker-host

# Fresh deployment (destroys ALL data)
./finapp fresh user@your-docker-host

# Check deployment status
./finapp check user@your-docker-host
```

### Deployment Tools
- **`./finapp`** - Main deployment manager with simple commands
- **`deployment/`** - Directory containing all deployment scripts and Docker files
- **`deployment/README.md`** - Detailed deployment documentation

### Direct Script Usage
```bash
cd deployment

# Assess current deployment
./check-deployment.sh user@your-docker-host

# Deploy application  
./deploy.sh user@your-docker-host

# Fresh install (destroys data)
./deploy-fresh.sh user@your-docker-host

# Check status
./check-status.sh user@your-docker-host
```

### Configuration
The application is configured for PostgreSQL by default. Connection strings are managed through:
- `appsettings.json`: Local development
- `appsettings.Development.json`: Development environment 
- `appsettings.Production.json`: Production deployment

## Run Locally
```bash
cd InvestmentTracker
dotnet restore
dotnet build
dotnet run
```

### Development Configuration
The application uses PostgreSQL for all environments. To run locally:

1. Start PostgreSQL (using Docker):
   ```bash
   docker run --name postgres-finapp -e POSTGRES_PASSWORD=<YOUR_PASSWORD> -e POSTGRES_DB=finapp_dev -e POSTGRES_USER=finapp -p 5432:5432 -d postgres:16-alpine
   ```

2. Run the application:
   ```bash
   cd InvestmentTracker
   dotnet run
   ```

The application will automatically create and migrate the database on startup.

## Persistence & Migrations
- PostgreSQL database with automatic migration application at startup
- Production-ready with full ACID compliance and performance optimization

To add migrations:
```bash
cd InvestmentTracker
dotnet ef migrations add <MigrationName> -o Data/Migrations
dotnet ef database update
```

## Project Structure
```
FinApp.sln
finapp                          # Main deployment manager script
deployment/                     # All deployment tools and Docker files
  ├── deploy.sh                # Regular deployment script
  ├── deploy-fresh.sh          # Fresh deployment (destroys data)
  ├── check-deployment.sh      # Pre-deployment assessment
  ├── check-status.sh          # Status monitoring
  ├── docker-compose.yml       # Service orchestration
  ├── Dockerfile               # Application container
  └── README.md                # Detailed deployment guide
InvestmentTracker/
  Program.cs                    # Startup configuration
  appsettings*.json            # Environment configurations
  Data/
    AppDbContext.cs            # EF Core context
    Migrations/                # Database migrations
  Models/                      # Core domain models
    Investment.cs, InvestmentValue.cs
    RegularExpense.cs, IrregularExpense.cs
    ExpenseSchedule.cs, CategoryBudget.cs
    Frequency.cs, Currency.cs
  Services/                    # Business logic services
    InvestmentService.cs, ExpenseService.cs
  Pages/                       # Razor Pages
    Investments/, Expenses/, Portfolio/, Values/
    Shared/
  Infrastructure/              # Cross-cutting concerns
    InvariantDecimalModelBinder*.cs
  wwwroot/                     # Static assets
docs/                          # Project documentation
```

## Roadmap & Future Enhancements
- **Analytics Dashboard**: Charts and graphs for expense trends and investment performance
- **Data Export**: CSV/JSON export capabilities with filtering
- **Advanced Budgeting**: Budget vs. actual reporting and alerts
- **Multi-Currency Support**: Enhanced currency handling and conversion rates
- **Mobile App**: React Native or Flutter companion app
- **API Extensions**: Enhanced REST API with authentication
- **Backup & Restore**: Automated backup solutions for different database providers
- **User Management**: Multi-user support with role-based access

---
*This README reflects the current implemented state and serves as comprehensive documentation for contributors and deployment teams.*
