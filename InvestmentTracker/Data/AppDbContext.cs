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
    public DbSet<RegularExpense> RegularExpenses => Set<RegularExpense>();
    public DbSet<IrregularExpense> IrregularExpenses => Set<IrregularExpense>();
    public DbSet<ExpenseSchedule> ExpenseSchedules => Set<ExpenseSchedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

        modelBuilder.Entity<ExpenseSchedule>()
            .Ignore(s => s.StartDate)
            .Ignore(s => s.EndDate);

        modelBuilder.Entity<IrregularExpense>()
            .HasOne(ie => ie.Category)
            .WithMany()
            .HasForeignKey(ie => ie.ExpenseCategoryId);

        base.OnModelCreating(modelBuilder);
    }
}
