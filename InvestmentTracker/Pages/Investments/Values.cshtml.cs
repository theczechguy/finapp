using InvestmentTracker.Data;
using InvestmentTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Pages.Investments;

public class ValuesModel(AppDbContext db) : PageModel
{
    public Investment? Investment { get; private set; }

    [BindProperty]
    public InvestmentValue NewValue { get; set; } = new() { AsOf = DateTime.Today };

    [BindProperty]
    public OneTimeContribution NewContribution { get; set; } = new() { Date = DateTime.Today };

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Investment = await db.Investments
            .Include(i => i.Values)
            .Include(i => i.Schedules)
            .Include(i => i.OneTimeContributions)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (Investment is null) return RedirectToPage("Index");
        NewValue.InvestmentId = id;
        Investment.Values = Investment.Values.OrderByDescending(v => v.AsOf).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            // reload investment list for view
            Investment = await db.Investments
                .Include(i => i.Values)
                .Include(i => i.Schedules)
                .Include(i => i.OneTimeContributions)
                .FirstOrDefaultAsync(i => i.Id == NewValue.InvestmentId);
            if (Investment is not null)
                Investment.Values = Investment.Values.OrderByDescending(v => v.AsOf).ToList();
            return Page();
        }

        db.InvestmentValues.Add(NewValue);
        await db.SaveChangesAsync();
        return RedirectToPage(new { id = NewValue.InvestmentId });
    }

    public async Task<IActionResult> OnPostAddContributionAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return await OnGetAsync(id);
        }
        NewContribution.InvestmentId = id;
        db.OneTimeContributions.Add(NewContribution);
        await db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteContributionAsync(int id, int contributionId)
    {
        var c = await db.OneTimeContributions.FirstOrDefaultAsync(x => x.Id == contributionId && x.InvestmentId == id);
        if (c is not null)
        {
            db.OneTimeContributions.Remove(c);
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, int valueId)
    {
        var value = await db.InvestmentValues.FirstOrDefaultAsync(v => v.Id == valueId && v.InvestmentId == id);
        if (value is not null)
        {
            db.InvestmentValues.Remove(value);
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }
}
