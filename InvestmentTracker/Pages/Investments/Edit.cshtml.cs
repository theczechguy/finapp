using InvestmentTracker.Data;
using InvestmentTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Pages.Investments;

public class EditModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public Investment Investment { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await db.Investments.FirstOrDefaultAsync(i => i.Id == id);
        if (entity is null) return RedirectToPage("Index");
        Investment = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var existing = await db.Investments.FirstOrDefaultAsync(i => i.Id == Investment.Id);
        if (existing is null) return RedirectToPage("Index");

        existing.Name = Investment.Name;
        existing.Provider = Investment.Provider;
        existing.Type = Investment.Type;
        existing.RecurringAmount = Investment.Type == InvestmentType.OneTime ? null : Investment.RecurringAmount;

        await db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
