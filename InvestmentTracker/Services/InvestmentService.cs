using InvestmentTracker.Data;
using InvestmentTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Services;

public class InvestmentService : IInvestmentService
{
    private readonly AppDbContext _db;

    public InvestmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Investment>> GetAllInvestmentsAsync()
    {
        return await _db.Investments
            .Include(i => i.Values)
            .ToListAsync();
    }

    public async Task<Investment?> GetInvestmentAsync(int id)
    {
        return await _db.Investments
            .Include(i => i.Values)
            .Include(i => i.Schedules)
            .Include(i => i.OneTimeContributions)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Investment> AddInvestmentAsync(Investment investment)
    {
        _db.Investments.Add(investment);
        await _db.SaveChangesAsync();
        return investment;
    }

    public async Task<bool> UpdateInvestmentAsync(int id, Investment update)
    {
        var existing = await _db.Investments.FindAsync(id);
        if (existing is null) return false;
        
        existing.Name = update.Name;
        existing.Provider = update.Provider;
        existing.Type = update.Type;
        existing.Currency = update.Currency;
        existing.ChargeAmount = update.ChargeAmount;
        
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteInvestmentAsync(int id)
    {
        var existing = await _db.Investments.FindAsync(id);
        if (existing is null) return false;
        
        _db.Remove(existing);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<object>> GetInvestmentValuesAsync(int investmentId)
    {
        return await _db.InvestmentValues
            .Where(v => v.InvestmentId == investmentId)
            .OrderBy(v => v.AsOf)
            .Select(v => new { v.Id, v.AsOf, v.Value })
            .ToListAsync();
    }

    public async Task AddInvestmentValueAsync(int investmentId, InvestmentValue value)
    {
        value.InvestmentId = investmentId;
        _db.InvestmentValues.Add(value);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteInvestmentValueAsync(int investmentId, int valueId)
    {
        var value = await _db.InvestmentValues.FirstOrDefaultAsync(v => v.Id == valueId && v.InvestmentId == investmentId);
        if (value is not null)
        {
            _db.InvestmentValues.Remove(value);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<object>> GetOneTimeContributionsAsync(int investmentId)
    {
        return await _db.OneTimeContributions
            .Where(c => c.InvestmentId == investmentId)
            .OrderBy(c => c.Date)
            .Select(c => new { c.Id, c.Date, c.Amount })
            .ToListAsync();
    }

    public async Task AddOneTimeContributionAsync(int investmentId, OneTimeContribution contribution)
    {
        contribution.InvestmentId = investmentId;
        _db.OneTimeContributions.Add(contribution);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteOneTimeContributionAsync(int investmentId, int contributionId)
    {
        var c = await _db.OneTimeContributions.FirstOrDefaultAsync(x => x.Id == contributionId && x.InvestmentId == investmentId);
        if (c is not null)
        {
            _db.OneTimeContributions.Remove(c);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<object>> GetContributionSchedulesAsync(int investmentId)
    {
        return await _db.ContributionSchedules
            .Where(s => s.InvestmentId == investmentId)
            .OrderBy(s => s.StartDate)
            .Select(s => new { s.Id, s.StartDate, s.EndDate, s.Amount, s.Frequency, s.DayOfMonth })
            .ToListAsync();
    }

    public async Task<(ContributionSchedule? schedule, string? error)> AddContributionScheduleAsync(int investmentId, ContributionSchedule schedule)
    {
        schedule.InvestmentId = investmentId;
        
        var newStart = schedule.StartDate.Date;
        var newEnd = (schedule.EndDate?.Date) ?? DateTime.MaxValue.Date;
        var overlaps = await _db.ContributionSchedules.AnyAsync(s => s.InvestmentId == investmentId &&
                                                                    newStart <= (s.EndDate == null ? DateTime.MaxValue.Date : s.EndDate.Value.Date) && s.StartDate.Date <= newEnd);
        if (overlaps) return (null, "Overlaps existing schedule.");

        schedule.Frequency = ContributionFrequency.Monthly;
        if (schedule.DayOfMonth is null or < 1 or > 31)
            schedule.DayOfMonth = schedule.StartDate.Day;
        
        _db.ContributionSchedules.Add(schedule);
        await _db.SaveChangesAsync();
        
        return (schedule, null);
    }

    public async Task<bool> DeleteContributionScheduleAsync(int investmentId, int scheduleId)
    {
        var sched = await _db.ContributionSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId && s.InvestmentId == investmentId);
        if (sched is null) return false;
        
        _db.ContributionSchedules.Remove(sched);
        await _db.SaveChangesAsync();
        return true;
    }
}
