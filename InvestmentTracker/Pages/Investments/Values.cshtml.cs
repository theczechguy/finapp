using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvestmentTracker.Pages.Investments;

public class ValuesModel(IInvestmentService investmentService) : PageModel
{
    public Investment? Investment { get; private set; }

    [BindProperty]
    public InvestmentValue NewValue { get; set; } = new() { AsOf = DateTime.Today };

    [BindProperty]
    public OneTimeContribution NewContribution { get; set; } = new() { Date = DateTime.Today };

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Investment = await investmentService.GetInvestmentAsync(id);
        if (Investment is null) return RedirectToPage("Index");
        NewValue.InvestmentId = id;
        Investment.Values = Investment.Values.OrderByDescending(v => v.AsOf).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAddValueAsync(int id)
    {
    ModelState.Clear();
    if (!TryValidateModel(NewValue, nameof(NewValue)))
        {
            // reload investment list for view
            Investment = await investmentService.GetInvestmentAsync(id);
            if (Investment is not null)
                Investment.Values = Investment.Values.OrderByDescending(v => v.AsOf).ToList();
            return Page();
        }

        await investmentService.AddInvestmentValueAsync(id, NewValue);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddContributionAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            Investment = await investmentService.GetInvestmentAsync(id);
            if (Investment is not null)
            {
                Investment.Values = Investment.Values.OrderByDescending(v => v.AsOf).ToList();
            }
            return Page();
        }
        await investmentService.AddOneTimeContributionAsync(id, NewContribution);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteContributionAsync(int id, int contributionId)
    {
        await investmentService.DeleteOneTimeContributionAsync(id, contributionId);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, int valueId)
    {
        await investmentService.DeleteInvestmentValueAsync(id, valueId);
        return RedirectToPage(new { id });
    }
}
