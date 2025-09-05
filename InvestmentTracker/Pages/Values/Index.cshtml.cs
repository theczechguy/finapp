using InvestmentTracker.Data;
using InvestmentTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Pages.Values;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Row> Items { get; private set; } = new();

    public class Row
    {
        public int Id { get; set; }
        public int InvestmentId { get; set; }
        public string InvestmentName { get; set; } = string.Empty;
        public DateTime AsOf { get; set; }
        public decimal Value { get; set; }
    }

    public async Task OnGetAsync()
    {
        Items = await db.InvestmentValues
            .OrderByDescending(v => v.AsOf)
            .Include(v => v.Investment)
            .Select(v => new Row
            {
                Id = v.Id,
                InvestmentId = v.InvestmentId,
                InvestmentName = v.Investment!.Name,
                AsOf = v.AsOf,
                Value = v.Value
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var value = await db.InvestmentValues.FirstOrDefaultAsync(v => v.Id == id);
        if (value is not null)
        {
            db.InvestmentValues.Remove(value);
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
