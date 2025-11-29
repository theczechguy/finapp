using System.Collections.Generic;
using System.Threading.Tasks;
using InvestmentTracker.Models.ImportProfiles;

namespace InvestmentTracker.Services.ImportProfiles
{
    public interface IBankImportProfileProvider
    {
        Task<IReadOnlyList<BankImportProfileSummary>> GetSummariesAsync();
        Task<BankImportProfile?> GetProfileAsync(string id);
        Task<IReadOnlyList<BankImportProfile>> GetAllProfilesAsync();
    }
}
