using InvestmentTracker.Data;
using InvestmentTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Pages.Values;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Row> Items { get; private set; } = new();
    public string Sort { get; private set; } = "AsOf";
    public string Dir { get; private set; } = "desc";

    [BindProperty(SupportsGet = true)]
    public string? Provider { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? InvestmentId { get; set; }

    public List<SelectListItem> ProviderOptions { get; private set; } = new();
    public List<SelectListItem> InvestmentOptions { get; private set; } = new();

    public class Row
    {
        public int Id { get; set; }
        public int InvestmentId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string InvestmentName { get; set; } = string.Empty;
        public DateTime AsOf { get; set; }
        public decimal Value { get; set; }
    }

    public async Task OnGetAsync(string? sort, string? dir)
    {
        Sort = string.IsNullOrWhiteSpace(sort) ? "AsOf" : sort;
        Dir = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";

        IQueryable<InvestmentValue> query = db.InvestmentValues.Include(v => v.Investment);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(Provider))
        {
            query = query.Where(v => v.Investment!.Provider == Provider);
        }
        if (InvestmentId is int invId && invId > 0)
        {
            query = query.Where(v => v.InvestmentId == invId);
        }

        // Apply sorting
        switch (Sort)
        {
            case "Provider":
                query = Dir == "asc"
                    ? query.OrderBy(v => v.Investment!.Provider).ThenBy(v => v.Investment!.Name).ThenBy(v => v.AsOf)
                    : query.OrderByDescending(v => v.Investment!.Provider).ThenByDescending(v => v.Investment!.Name).ThenByDescending(v => v.AsOf);
                break;
            case "Investment":
                query = Dir == "asc"
                    ? query.OrderBy(v => v.Investment!.Name).ThenBy(v => v.AsOf)
                    : query.OrderByDescending(v => v.Investment!.Name).ThenByDescending(v => v.AsOf);
                break;
            case "Value":
                query = Dir == "asc"
                    ? query.OrderBy(v => v.Value).ThenBy(v => v.AsOf)
                    : query.OrderByDescending(v => v.Value).ThenByDescending(v => v.AsOf);
                break;
            case "AsOf":
            default:
                query = Dir == "asc"
                    ? query.OrderBy(v => v.AsOf)
                    : query.OrderByDescending(v => v.AsOf);
                break;
        }

        Items = await query
            .Select(v => new Row
            {
                Id = v.Id,
                InvestmentId = v.InvestmentId,
                Provider = v.Investment!.Provider ?? string.Empty,
                InvestmentName = v.Investment!.Name,
                AsOf = v.AsOf,
                Value = v.Value
            })
            .ToListAsync();

        // Load filter options
        ProviderOptions = await db.Investments
            .Select(i => i.Provider!)
            .Where(p => p != null && p != "")
            .Distinct()
            .OrderBy(p => p)
            .Select(p => new SelectListItem { Text = p!, Value = p! })
            .ToListAsync();
        ProviderOptions.Insert(0, new SelectListItem { Text = "All Providers", Value = "" });

        InvestmentOptions = await db.Investments
            .OrderBy(i => i.Name)
            .Select(i => new SelectListItem { Text = i.Name, Value = i.Id.ToString() })
            .ToListAsync();
        InvestmentOptions.Insert(0, new SelectListItem { Text = "All Investments", Value = "" });
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
