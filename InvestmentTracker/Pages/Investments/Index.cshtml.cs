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
}
