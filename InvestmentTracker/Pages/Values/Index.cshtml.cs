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
        public Currency Currency { get; set; }
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
                Currency = v.Investment!.Currency,
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
            .Select(i => new { i.Id, i.Type, i.ChargeAmount })
            .ToDictionaryAsync(x => x.Id);

        // Fetch schedules per investment for invested calc
        var schedules = await db.ContributionSchedules
            .Where(s => invIds.Contains(s.InvestmentId))
            .OrderBy(s => s.StartDate)
            .GroupBy(s => s.InvestmentId)
            .ToDictionaryAsync(g => g.Key, g => g.ToList());

        // Fetch one-time contributions per investment
        var oneTimes = await db.OneTimeContributions
            .Where(c => invIds.Contains(c.InvestmentId))
            .OrderBy(c => c.Date)
            .GroupBy(c => c.InvestmentId)
            .ToDictionaryAsync(g => g.Key, g => g.ToList());

        // Fetch earliest recorded value per investment (initial principal for one-time)
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
                decimal total = 0m;
                if (schedules.TryGetValue(r.InvestmentId, out var scheds))
                {
                    foreach (var s in scheds)
                    {
                        var end = s.EndDate.HasValue && s.EndDate.Value.Date < r.AsOf.Date ? s.EndDate.Value.Date : r.AsOf.Date;
                        if (end < s.StartDate.Date) continue;
                        int months = MonthsContributedWithDom(s.StartDate.Date, end, s.DayOfMonth ?? s.StartDate.Day);
                        if (months > 0 && s.Amount > 0)
                        {
                            total += months * s.Amount;
                        }
                    }
                }
                if (oneTimes.TryGetValue(r.InvestmentId, out var lumps))
                {
                    foreach (var c in lumps)
                    {
                        if (c.Date.Date <= r.AsOf.Date)
                            total += c.Amount;
                    }
                }
                if (meta.ChargeAmount > 0 && total > 0)
                {
                    total -= meta.ChargeAmount;
                }
                r.Invested = total;
            }
            else // OneTime
            {
                if (firstValues.TryGetValue(r.InvestmentId, out var fv))
                {
                    var invested = fv.value;
                    if (invMeta.TryGetValue(r.InvestmentId, out var m) && m.ChargeAmount > 0)
                    {
                        invested -= m.ChargeAmount;
                    }
                    r.Invested = invested;
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

    private static int MonthsContributedWithDom(DateTime startDate, DateTime endDateInclusive, int dayOfMonth)
    {
        if (endDateInclusive < startDate) return 0;
        int months = (endDateInclusive.Year - startDate.Year) * 12 + (endDateInclusive.Month - startDate.Month);
        int effectiveDayEnd = Math.Min(dayOfMonth, DateTime.DaysInMonth(endDateInclusive.Year, endDateInclusive.Month));
        if (endDateInclusive.Day >= effectiveDayEnd) months += 1;
        return Math.Max(0, months);
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
