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

        // If the value being added is an Interest entry, treat the entered Value as an interest amount
        // and compute the new stored Value as previousTotal + interestAmount.
        if (NewValue.ChangeType == ValueChangeType.Interest)
        {
            var invPrev = await investmentService.GetInvestmentAsync(id);
            decimal previousTotal = 0m;
            if (invPrev is not null)
            {
                var latest = invPrev.Values.OrderByDescending(v => v.AsOf).FirstOrDefault();
                if (latest is not null)
                {
                    previousTotal = latest.Value;
                }
                else if (invPrev.OneTimeContributions?.Any() == true)
                {
                    previousTotal = invPrev.OneTimeContributions.Sum(c => c.Amount);
                }
            }

            // Validate interest amount is non-negative
            if (NewValue.Value < 0)
            {
                ModelState.AddModelError("NewValue.Value", "Interest amount must be non-negative.");
                Investment = invPrev;
                if (Investment is not null)
                    Investment.Values = Investment.Values.OrderByDescending(v => v.AsOf).ToList();
                return Page();
            }

            // Store total value (previous + interest) but keep ChangeType = Interest so we know the reason
            var interestAmount = NewValue.Value;
            NewValue.Value = previousTotal + interestAmount;
        }

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
        TempData["ToastSuccess"] = $"Value added for {NewValue.AsOf:d}.";
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
        TempData["ToastSuccess"] = $"Contribution added for {NewContribution.Date:d}.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteContributionAsync(int id, int contributionId)
    {
        await investmentService.DeleteOneTimeContributionAsync(id, contributionId);
        TempData["ToastSuccess"] = "Contribution deleted.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, int valueId)
    {
        await investmentService.DeleteInvestmentValueAsync(id, valueId);
        TempData["ToastSuccess"] = "Value deleted.";
        return RedirectToPage(new { id });
    }
}
