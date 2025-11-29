using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace InvestmentTracker.Pages.Investments;

public class CreateModel(IInvestmentService investmentService, InvestmentTracker.Data.AppDbContext db) : PageModel
{
    [BindProperty]
    public Investment Investment { get; set; } = new() { ChargeAmount = default };
    public SelectList? FamilyMembers { get; set; }

    public async Task OnGetAsync()
    {
        // Initialize with empty values to prevent default model values from showing in form
        Investment = new Investment
        {
            Name = string.Empty,
            Provider = string.Empty,
            ChargeAmount = 0 // Explicitly set to 0, but we'll handle display differently
        };
        
        var members = await db.FamilyMember.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
        FamilyMembers = new SelectList(members, "Id", "Name");
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
            var members = await db.FamilyMember.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
            FamilyMembers = new SelectList(members, "Id", "Name");
            return Page();
        }

        var createdInvestment = await investmentService.AddInvestmentAsync(Investment);
        return RedirectToPage("./Edit", new { id = createdInvestment.Id });
    }
}
