using InvestmentTracker.Data;
using InvestmentTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvestmentTracker.Pages.Investments;

public class CreateModel(AppDbContext db) : PageModel
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

    // No legacy recurring fields; schedules managed separately

        db.Investments.Add(Investment);
        await db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
