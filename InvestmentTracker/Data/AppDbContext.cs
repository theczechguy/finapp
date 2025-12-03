using InvestmentTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<InvestmentValue> InvestmentValues => Set<InvestmentValue>();
    public DbSet<ContributionSchedule> ContributionSchedules => Set<ContributionSchedule>();
    public DbSet<OneTimeContribution> OneTimeContributions => Set<OneTimeContribution>();

    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<IncomeSource> IncomeSources => Set<IncomeSource>();
    public DbSet<MonthlyIncome> MonthlyIncomes => Set<MonthlyIncome>();
    public DbSet<OneTimeIncome> OneTimeIncomes => Set<OneTimeIncome>();
    public DbSet<RegularExpense> RegularExpenses => Set<RegularExpense>();
    public DbSet<IrregularExpense> IrregularExpenses => Set<IrregularExpense>();
    public DbSet<ExpenseSchedule> ExpenseSchedules => Set<ExpenseSchedule>();
    public DbSet<FamilyMember> FamilyMember => Set<FamilyMember>();
    public DbSet<CategoryBudget> CategoryBudgets => Set<CategoryBudget>();
    public DbSet<FinancialScheduleConfig> FinancialScheduleConfigs => Set<FinancialScheduleConfig>();
    public DbSet<FinancialMonthOverride> FinancialMonthOverrides => Set<FinancialMonthOverride>();
    public DbSet<InvestmentProvider> InvestmentProviders => Set<InvestmentProvider>();
    public DbSet<ImportProfile> ImportProfiles => Set<ImportProfile>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure DateTime properties for PostgreSQL compatibility
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamp without time zone");
                }
            }
        }

        modelBuilder.Entity<Investment>()
            .HasMany(i => i.Values)
            .WithOne(v => v.Investment!)
            .HasForeignKey(v => v.InvestmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Investment>()
            .HasMany(i => i.Schedules)
            .WithOne(s => s.Investment!)
            .HasForeignKey(s => s.InvestmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Investment>()
            .HasMany(i => i.OneTimeContributions)
            .WithOne(c => c.Investment!)
            .HasForeignKey(c => c.InvestmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InvestmentValue>()
            .HasIndex(v => new { v.InvestmentId, v.AsOf })
            .IsUnique();

        modelBuilder.Entity<ContributionSchedule>()
            .HasIndex(s => new { s.InvestmentId, s.StartDate });

        modelBuilder.Entity<OneTimeContribution>()
            .HasIndex(c => new { c.InvestmentId, c.Date });

        // Expense Tracker Models
        modelBuilder.Entity<MonthlyIncome>()
            .HasOne(mi => mi.IncomeSource)
            .WithMany()
            .HasForeignKey(mi => mi.IncomeSourceId);

        modelBuilder.Entity<MonthlyIncome>()
            .HasIndex(mi => new { mi.IncomeSourceId, mi.Month })
            .IsUnique();

        modelBuilder.Entity<OneTimeIncome>()
            .HasOne(oti => oti.IncomeSource)
            .WithMany()
            .HasForeignKey(oti => oti.IncomeSourceId);

        modelBuilder.Entity<OneTimeIncome>()
            .HasIndex(oti => oti.Date);

        modelBuilder.Entity<RegularExpense>()
            .HasOne(re => re.Category)
            .WithMany()
            .HasForeignKey(re => re.ExpenseCategoryId);

        modelBuilder.Entity<RegularExpense>()
            .HasMany(re => re.Schedules)
            .WithOne(s => s.RegularExpense)
            .HasForeignKey(s => s.RegularExpenseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExpenseSchedule>()
            .HasIndex(s => new { s.RegularExpenseId, s.StartYear, s.StartMonth });

        // Performance optimization indexes for temporal expense queries
        modelBuilder.Entity<ExpenseSchedule>()
            .HasIndex(s => new { s.StartYear, s.StartMonth, s.EndYear, s.EndMonth })
            .HasDatabaseName("IX_ExpenseSchedules_TemporalLookup");

        // Covering index for month-based expense calculations
        modelBuilder.Entity<ExpenseSchedule>()
            .HasIndex(s => new { s.StartYear, s.StartMonth, s.EndYear, s.EndMonth, s.RegularExpenseId, s.Amount, s.Frequency })
            .HasDatabaseName("IX_ExpenseSchedules_CoveringQuery");

        modelBuilder.Entity<ExpenseSchedule>()
            .Ignore(s => s.StartDate)
            .Ignore(s => s.EndDate);

        modelBuilder.Entity<IrregularExpense>()
            .HasOne(ie => ie.Category)
            .WithMany()
            .HasForeignKey(ie => ie.ExpenseCategoryId);

        // Optimize irregular expense queries by date and category
        modelBuilder.Entity<IrregularExpense>()
            .HasIndex(ie => ie.Date)
            .HasDatabaseName("IX_IrregularExpenses_Date");

        modelBuilder.Entity<IrregularExpense>()
            .HasIndex(ie => new { ie.Date, ie.ExpenseCategoryId, ie.Amount })
            .HasDatabaseName("IX_IrregularExpenses_DateCategoryAmount");

        // Budgets
        modelBuilder.Entity<CategoryBudget>()
            .HasOne(cb => cb.ExpenseCategory)
            .WithMany()
            .HasForeignKey(cb => cb.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CategoryBudget>()
            .HasIndex(cb => new { cb.ExpenseCategoryId, cb.StartYear, cb.StartMonth });

        // Investment provider lookup table to persist provider names and avoid duplication
        modelBuilder.Entity<InvestmentProvider>()
            .HasIndex(p => p.Name)
            .IsUnique();

        // Performance optimization indexes for temporal queries
        modelBuilder.Entity<CategoryBudget>()
            .HasIndex(cb => new { cb.StartYear, cb.StartMonth, cb.EndYear, cb.EndMonth, cb.ExpenseCategoryId, cb.Amount })
            .HasDatabaseName("IX_CategoryBudgets_CoveringQuery");

        modelBuilder.Entity<FinancialMonthOverride>()
            .HasIndex(fmo => new { fmo.TargetMonth, fmo.UserId })
            .IsUnique();

        modelBuilder.Entity<UserAccount>()
            .HasIndex(u => u.AccountNumber)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
