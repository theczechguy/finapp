using System.Collections.Generic;
using System.Threading.Tasks;
using InvestmentTracker.Models;
using InvestmentTracker.Services.ImportProfiles;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvestmentTracker.Pages.Expenses.ImportProfiles
{
    public class IndexModel : PageModel
    {
        private readonly ImportProfileService _service;

        public IndexModel(ImportProfileService service)
        {
            _service = service;
        }

        public IReadOnlyList<ImportProfile> Profiles { get; set; } = new List<ImportProfile>();

        public async Task OnGetAsync()
        {
            Profiles = await _service.GetAllEntitiesAsync();
        }
    }
}
