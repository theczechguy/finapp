using InvestmentTracker.Data;
using InvestmentTracker.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Pages.Investments;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Investment> Items { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Items = await db.Investments
            .Include(i => i.Values)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task OnPostDeleteAsync(int id)
    {
        var inv = await db.Investments.FirstOrDefaultAsync(i => i.Id == id);
        if (inv is not null)
        {
            db.Investments.Remove(inv);
            await db.SaveChangesAsync();
        }
        await OnGetAsync();
    }
}
