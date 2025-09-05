using InvestmentTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<InvestmentValue> InvestmentValues => Set<InvestmentValue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Investment>()
            .HasMany(i => i.Values)
            .WithOne(v => v.Investment!)
            .HasForeignKey(v => v.InvestmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InvestmentValue>()
            .HasIndex(v => new { v.InvestmentId, v.AsOf })
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
