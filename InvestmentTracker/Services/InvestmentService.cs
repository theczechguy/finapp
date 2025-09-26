using InvestmentTracker.Data;
using InvestmentTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentTracker.Services;

public class InvestmentService : IInvestmentService
{
    private readonly AppDbContext _db;
    private readonly ILogger<InvestmentService> _logger;

    public InvestmentService(AppDbContext db, ILogger<InvestmentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<InvestmentSummary>> GetAllInvestmentsAsync()
    {
        _logger.LogInformation("Fetching all investments");
        return await _db.Investments
            .Select(i => new InvestmentSummary
            {
                Id = i.Id,
                Name = i.Name,
                Category = i.Category,
                Type = i.Type,
                Currency = i.Currency,
                Provider = i.Provider,
                ChargeAmount = i.ChargeAmount
            })
            .ToListAsync();
    }

    public async Task<Investment?> GetInvestmentAsync(int id)
    {
        _logger.LogInformation("Fetching investment with ID: {InvestmentId}", id);
        var investment = await _db.Investments
            .Include(i => i.Values)
            .Include(i => i.Schedules)
            .Include(i => i.OneTimeContributions)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (investment == null)
        {
            _logger.LogWarning("Investment with ID: {InvestmentId} not found", id);
            return null;
        }

        return investment;
    }

    public async Task<IEnumerable<string>> GetProvidersAsync(string? query)
    {
        _logger.LogInformation("Fetching providers with query: {Query}", query);
        var q = _db.InvestmentProviders.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(p => p.Name.ToLower().Contains(query.ToLower()));
        }

        return await q.OrderBy(p => p.Name)
                      .Select(p => p.Name)
                      .Take(50)
                      .ToListAsync();
    }

    public async Task EnsureProviderExistsAsync(string? providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName)) return;

        var normalized = providerName.Trim();
        var exists = await _db.InvestmentProviders.AnyAsync(p => p.Name == normalized);
        if (!exists)
        {
            _db.InvestmentProviders.Add(new Models.InvestmentProvider { Name = normalized });
            await _db.SaveChangesAsync();
            _logger.LogInformation("Added new provider to lookup table: {Provider}", normalized);
        }
    }

    public async Task<Investment> AddInvestmentAsync(Investment investment)
    {
        _logger.LogInformation("Adding new investment: {InvestmentName}", investment.Name);

        // Persist provider name to lookup table (silent upsert)
        await EnsureProviderExistsAsync(investment.Provider);

        _db.Investments.Add(investment);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Successfully added new investment with ID: {InvestmentId}", investment.Id);
        return investment;
    }

    public async Task<bool> UpdateInvestmentAsync(int id, Investment update)
    {
        _logger.LogInformation("Updating investment with ID: {InvestmentId}", id);
        var existing = await _db.Investments.FindAsync(id);
        if (existing is null)
        {
            _logger.LogWarning("Update failed. Investment with ID: {InvestmentId} not found", id);
            return false;
        }
        
        existing.Name = update.Name;
        existing.Provider = update.Provider;
        existing.Type = update.Type;
        existing.Category = update.Category;
        existing.Currency = update.Currency;
        existing.ChargeAmount = update.ChargeAmount;
        
        await _db.SaveChangesAsync();
        _logger.LogInformation("Successfully updated investment with ID: {InvestmentId}", id);
        return true;
    }

    public async Task<bool> DeleteInvestmentAsync(int id)
    {
        _logger.LogInformation("Deleting investment with ID: {InvestmentId}", id);
        var existing = await _db.Investments.FindAsync(id);
        if (existing is null)
        {
            _logger.LogWarning("Delete failed. Investment with ID: {InvestmentId} not found", id);
            return false;
        }
        
        _db.Remove(existing);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Successfully deleted investment with ID: {InvestmentId}", id);
        return true;
    }

    public async Task<IEnumerable<object>> GetInvestmentValuesAsync(int investmentId)
    {
        _logger.LogInformation("Fetching values for investment ID: {InvestmentId}", investmentId);
        return await _db.InvestmentValues
            .Where(v => v.InvestmentId == investmentId)
            .OrderBy(v => v.AsOf)
            .Select(v => new { v.Id, v.AsOf, v.Value })
            .ToListAsync();
    }

    public async Task AddInvestmentValueAsync(int investmentId, InvestmentValue value)
    {
        _logger.LogInformation("Adding new value for investment ID: {InvestmentId}", investmentId);
        value.InvestmentId = investmentId;
        _db.InvestmentValues.Add(value);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Successfully added new value with ID: {ValueId}", value.Id);
    }

    public async Task DeleteInvestmentValueAsync(int investmentId, int valueId)
    {
        _logger.LogInformation("Deleting value with ID: {ValueId} for investment ID: {InvestmentId}", valueId, investmentId);
        var value = await _db.InvestmentValues.FirstOrDefaultAsync(v => v.Id == valueId && v.InvestmentId == investmentId);
        if (value is not null)
        {
            _db.InvestmentValues.Remove(value);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully deleted value with ID: {ValueId}", valueId);
        }
        else
        {
            _logger.LogWarning("Delete failed. Value with ID: {ValueId} not found for investment ID: {InvestmentId}", valueId, investmentId);
        }
    }

    public async Task<IEnumerable<object>> GetOneTimeContributionsAsync(int investmentId)
    {
        _logger.LogInformation("Fetching one-time contributions for investment ID: {InvestmentId}", investmentId);
        return await _db.OneTimeContributions
            .Where(c => c.InvestmentId == investmentId)
            .OrderBy(c => c.Date)
            .Select(c => new { c.Id, c.Date, c.Amount })
            .ToListAsync();
    }

    public async Task AddOneTimeContributionAsync(int investmentId, OneTimeContribution contribution)
    {
        _logger.LogInformation("Adding one-time contribution for investment ID: {InvestmentId}", investmentId);
        contribution.InvestmentId = investmentId;
        _db.OneTimeContributions.Add(contribution);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Successfully added one-time contribution with ID: {ContributionId}", contribution.Id);
    }

    public async Task DeleteOneTimeContributionAsync(int investmentId, int contributionId)
    {
        _logger.LogInformation("Deleting one-time contribution with ID: {ContributionId} for investment ID: {InvestmentId}", contributionId, investmentId);
        var c = await _db.OneTimeContributions.FirstOrDefaultAsync(x => x.Id == contributionId && x.InvestmentId == investmentId);
        if (c is not null)
        {
            _db.OneTimeContributions.Remove(c);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully deleted one-time contribution with ID: {ContributionId}", contributionId);
        }
        else
        {
            _logger.LogWarning("Delete failed. One-time contribution with ID: {ContributionId} not found for investment ID: {InvestmentId}", contributionId, investmentId);
        }
    }

    public async Task<IEnumerable<object>> GetContributionSchedulesAsync(int investmentId)
    {
        _logger.LogInformation("Fetching contribution schedules for investment ID: {InvestmentId}", investmentId);
        return await _db.ContributionSchedules
            .Where(s => s.InvestmentId == investmentId)
            .OrderBy(s => s.StartDate)
            .Select(s => new { s.Id, s.StartDate, s.EndDate, s.Amount, s.Frequency, s.DayOfMonth })
            .ToListAsync();
    }

    public async Task<(ContributionSchedule? schedule, string? error)> AddContributionScheduleAsync(int investmentId, ContributionSchedule schedule)
    {
        _logger.LogInformation("Adding contribution schedule for investment ID: {InvestmentId}", investmentId);
        schedule.InvestmentId = investmentId;
        
        var newStart = schedule.StartDate.Date;
        var newEnd = (schedule.EndDate?.Date) ?? DateTime.MaxValue.Date;
        var overlaps = await _db.ContributionSchedules.AnyAsync(s => s.InvestmentId == investmentId &&
                                                                    newStart <= (s.EndDate == null ? DateTime.MaxValue.Date : s.EndDate.Value.Date) && s.StartDate.Date <= newEnd);
        if (overlaps)
        {
            const string error = "Overlaps existing schedule.";
            _logger.LogWarning("Failed to add contribution schedule for investment ID: {InvestmentId}. Reason: {Error}", investmentId, error);
            return (null, error);
        }

        schedule.Frequency = ContributionFrequency.Monthly;
        if (schedule.DayOfMonth is null or < 1 or > 31)
            schedule.DayOfMonth = schedule.StartDate.Day;
        
        _db.ContributionSchedules.Add(schedule);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Successfully added contribution schedule with ID: {ScheduleId}", schedule.Id);
        return (schedule, null);
    }

    public async Task<bool> DeleteContributionScheduleAsync(int investmentId, int scheduleId)
    {
        _logger.LogInformation("Deleting contribution schedule with ID: {ScheduleId} for investment ID: {InvestmentId}", scheduleId, investmentId);
        var sched = await _db.ContributionSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId && s.InvestmentId == investmentId);
        if (sched is null)
        {
            _logger.LogWarning("Delete failed. Contribution schedule with ID: {ScheduleId} not found for investment ID: {InvestmentId}", scheduleId, investmentId);
            return false;
        }
        
        _db.ContributionSchedules.Remove(sched);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Successfully deleted contribution schedule with ID: {ScheduleId}", scheduleId);
        return true;
    }

    public Task<List<InvestmentValue>> GetInvestmentValuesFromDateAsync(DateTime fromDate)
    {
        return _db.InvestmentValues
            .Where(v => v.AsOf.Date >= fromDate)
            .ToListAsync();
    }
}
