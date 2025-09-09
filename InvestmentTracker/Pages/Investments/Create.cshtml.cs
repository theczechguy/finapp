using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace InvestmentTracker.Pages.Investments;

public class CreateModel(IInvestmentService investmentService) : PageModel
{
    [BindProperty]
    public Investment Investment { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var createdInvestment = await investmentService.AddInvestmentAsync(Investment);
        return RedirectToPage("./Edit", new { id = createdInvestment.Id });
    }
}
