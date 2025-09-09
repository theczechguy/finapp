using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace InvestmentTracker.Pages.Investments;

public class CreateModel(IInvestmentService investmentService) : PageModel
{
    [BindProperty]
    public Investment Investment { get; set; } = new() { ChargeAmount = default };

    public void OnGet()
    {
        // Initialize with empty values to prevent default model values from showing in form
        Investment = new Investment
        {
            Name = string.Empty,
            Provider = string.Empty,
            ChargeAmount = 0 // Explicitly set to 0, but we'll handle display differently
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Handle empty ChargeAmount field
        if (string.IsNullOrWhiteSpace(Request.Form["Investment.ChargeAmount"]))
        {
            Investment.ChargeAmount = 0;
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var createdInvestment = await investmentService.AddInvestmentAsync(Investment);
        return RedirectToPage("./Edit", new { id = createdInvestment.Id });
    }
}
