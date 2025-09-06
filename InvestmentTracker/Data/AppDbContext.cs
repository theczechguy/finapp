using InvestmentTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<InvestmentValue> InvestmentValues => Set<InvestmentValue>();
    public DbSet<ContributionSchedule> ContributionSchedules => Set<ContributionSchedule>();

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

        modelBuilder.Entity<InvestmentValue>()
            .HasIndex(v => new { v.InvestmentId, v.AsOf })
            .IsUnique();

        modelBuilder.Entity<ContributionSchedule>()
            .HasIndex(s => new { s.InvestmentId, s.StartDate });

        base.OnModelCreating(modelBuilder);
    }
}
