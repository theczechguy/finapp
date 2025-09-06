using InvestmentTracker.Models;

namespace InvestmentTracker.Services;

public interface IInvestmentService
{
    Task<IEnumerable<Investment>> GetAllInvestmentsAsync();
    Task<Investment?> GetInvestmentAsync(int id);
    Task<Investment> AddInvestmentAsync(Investment investment);
    Task<bool> UpdateInvestmentAsync(int id, Investment update);
    Task<bool> DeleteInvestmentAsync(int id);
    
    Task<IEnumerable<object>> GetInvestmentValuesAsync(int investmentId);
    Task AddInvestmentValueAsync(int investmentId, InvestmentValue value);
    Task DeleteInvestmentValueAsync(int investmentId, int valueId);

    Task<IEnumerable<object>> GetOneTimeContributionsAsync(int investmentId);
    Task AddOneTimeContributionAsync(int investmentId, OneTimeContribution contribution);
    Task DeleteOneTimeContributionAsync(int investmentId, int contributionId);
    
    Task<IEnumerable<object>> GetContributionSchedulesAsync(int investmentId);
    Task<(ContributionSchedule? schedule, string? error)> AddContributionScheduleAsync(int investmentId, ContributionSchedule schedule);
    Task<bool> DeleteContributionScheduleAsync(int investmentId, int scheduleId);
}
