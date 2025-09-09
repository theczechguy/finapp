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
        if (Investment is null) return RedirectToPage("./List");
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

        // Validate the date is not in the future
        if (NewValue.AsOf.Date > DateTime.Today)
        {
            ModelState.AddModelError("NewValue.AsOf", "Cannot add a value for a future date.");
            Investment = await investmentService.GetInvestmentAsync(id);
            if (Investment is not null)
                Investment.Values = Investment.Values.OrderByDescending(v => v.AsOf).ToList();
            return Page();
        }

        // Check for duplicate value on the same date
        var existingValue = await investmentService.GetInvestmentAsync(id);
        if (existingValue != null && existingValue.Values.Any(v => v.AsOf.Date == NewValue.AsOf.Date))
        {
            ModelState.AddModelError("NewValue.AsOf", $"A value already exists for {NewValue.AsOf.ToShortDateString()}. Please choose a different date or edit the existing value.");
            // reload investment list for view
            Investment = existingValue;
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

        // Validate the date is not in the future
        if (NewContribution.Date.Date > DateTime.Today)
        {
            ModelState.AddModelError("NewContribution.Date", "Cannot add a contribution for a future date.");
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
