using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace InvestmentTracker.Pages.Investments;

public class ListModel(IInvestmentService investmentService) : PageModel
{
    public IList<InvestmentSummary> Investments { get;set; } = default!;

    public async Task OnGetAsync()
    {
        var investments = await investmentService.GetAllInvestmentsAsync();
        Investments = investments.ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await investmentService.DeleteInvestmentAsync(id);
        return RedirectToPage();
    }
}
