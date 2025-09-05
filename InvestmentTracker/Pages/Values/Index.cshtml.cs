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

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 25;

    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }

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
    public decimal? Invested { get; set; }
    public decimal? PercentDiff { get; set; }
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
        if (From.HasValue)
        {
            var from = From.Value.Date;
            query = query.Where(v => v.AsOf >= from);
        }
        if (To.HasValue)
        {
            // include the whole day
            var to = To.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(v => v.AsOf <= to);
        }

        // Count before paging
        TotalCount = await query.CountAsync();
        if (PageSize <= 0) PageSize = 25;
    TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
    if (PageNumber < 1) PageNumber = 1;
    if (PageNumber > TotalPages) PageNumber = TotalPages;

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

        var pageItems = await query
            .Select(v => new Row
            {
                Id = v.Id,
                InvestmentId = v.InvestmentId,
                Provider = v.Investment!.Provider ?? string.Empty,
                InvestmentName = v.Investment!.Name,
                AsOf = v.AsOf,
                Value = v.Value
            })
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        // Compute Invested and PercentDiff per row
        var invIds = pageItems.Select(i => i.InvestmentId).Distinct().ToList();
        var invMeta = await db.Investments
            .Where(i => invIds.Contains(i.Id))
            .Select(i => new
            {
                i.Id,
                i.Type,
                i.RecurringAmount,
                i.RecurringStartDate
            })
            .ToDictionaryAsync(x => x.Id);

        // Fetch earliest recorded value per investment (initial principal for one-time or fallback start)
        var firstValues = new Dictionary<int, (DateTime date, decimal value)>();
        foreach (var id in invIds)
        {
            var fv = await db.InvestmentValues
                .Where(v => v.InvestmentId == id)
                .OrderBy(v => v.AsOf)
                .Select(v => new { v.AsOf, v.Value })
                .FirstOrDefaultAsync();
            if (fv != null)
            {
                firstValues[id] = (fv.AsOf, fv.Value);
            }
        }

        foreach (var r in pageItems)
        {
            if (!invMeta.TryGetValue(r.InvestmentId, out var meta)) continue;

            if (meta.Type == InvestmentType.Recurring)
            {
                var start = meta.RecurringStartDate ?? (firstValues.TryGetValue(r.InvestmentId, out var fv) ? fv.date : (DateTime?)null);
                var amount = meta.RecurringAmount ?? 0m;
                if (start.HasValue && amount > 0m && r.AsOf.Date >= start.Value.Date)
                {
                    int months = MonthsContributed(start.Value.Date, r.AsOf.Date);
                    r.Invested = months * amount;
                }
                else
                {
                    r.Invested = 0m;
                }
            }
            else // OneTime
            {
                if (firstValues.TryGetValue(r.InvestmentId, out var fv))
                {
                    r.Invested = fv.value;
                }
                else
                {
                    r.Invested = 0m;
                }
            }

            if (r.Invested.HasValue && r.Invested.Value > 0m)
            {
                r.PercentDiff = (r.Value - r.Invested.Value) / r.Invested.Value;
            }
            else
            {
                r.PercentDiff = null;
            }
        }

        Items = pageItems;

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

    private static int MonthsContributed(DateTime startDate, DateTime asOfDate)
    {
        if (asOfDate < startDate) return 0;
        int months = (asOfDate.Year - startDate.Year) * 12 + (asOfDate.Month - startDate.Month);
        // Count a contribution in the asOf month if day has passed or equals
        int startDay = startDate.Day;
        int asOfMonthDays = DateTime.DaysInMonth(asOfDate.Year, asOfDate.Month);
        int effectiveDay = Math.Min(startDay, asOfMonthDays);
        if (asOfDate.Day >= effectiveDay) months += 1;
        return months;
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
