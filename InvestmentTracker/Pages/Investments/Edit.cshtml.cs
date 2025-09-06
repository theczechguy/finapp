using InvestmentTracker.Data;
using InvestmentTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Pages.Investments;

public class EditModel(AppDbContext db) : PageModel
{
    [BindProperty]
    [ValidateNever]
    public Investment Investment { get; set; } = new();

    public List<ContributionSchedule> Schedules { get; set; } = new();

    public List<OneTimeContribution> Contributions { get; set; } = new();

    [BindProperty]
    public ScheduleInput NewSchedule { get; set; } = new();

    [BindProperty]
    public OneTimeContribution NewContribution { get; set; } = new() { Date = DateTime.Today };

    public class ScheduleInput
    {
        [BindProperty]
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? DayOfMonth { get; set; }
        public decimal? Amount { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await db.Investments
            .Include(i => i.Schedules.OrderBy(s => s.StartDate))
            .Include(i => i.OneTimeContributions)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (entity is null) return RedirectToPage("Index");
        Investment = entity;
        Schedules = entity.Schedules.OrderBy(s => s.StartDate).ToList();
        Contributions = entity.OneTimeContributions.OrderByDescending(c => c.Date).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Clear();
        if (!TryValidateModel(Investment, nameof(Investment)))
        {
            var reloaded = await db.Investments
                .Include(i => i.Schedules)
                .Include(i => i.OneTimeContributions)
                .FirstOrDefaultAsync(i => i.Id == Investment.Id);
            if (reloaded is not null)
            {
                Investment = reloaded;
                Schedules = reloaded.Schedules.OrderBy(s => s.StartDate).ToList();
                Contributions = reloaded.OneTimeContributions.OrderByDescending(c => c.Date).ToList();
            }
            return Page();
        }

        var existing = await db.Investments.FirstOrDefaultAsync(i => i.Id == Investment.Id);
        if (existing is null) return RedirectToPage("Index");

    existing.Name = Investment.Name;
    existing.Provider = Investment.Provider;
    existing.Type = Investment.Type;
    existing.Currency = Investment.Currency;
    existing.ChargeAmount = Investment.ChargeAmount;

        await db.SaveChangesAsync();
        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostAddScheduleAsync(int id)
    {
    var inv = await db.Investments.Include(i => i.Schedules).FirstOrDefaultAsync(i => i.Id == id);
        if (inv is null) return RedirectToPage("Index");

    // Clear any unrelated model state (e.g., Investment.Name required)
    ModelState.Clear();

    // Ignore unrelated Investment validation during schedule add
    ModelState.Remove("Investment");
    ModelState.Remove("Investment.Name");
    ModelState.Remove("Investment.Type");
    ModelState.Remove("Investment.Currency");
    ModelState.Remove("Investment.Provider");

        // Basic validation
        if (!NewSchedule.StartDate.HasValue)
            ModelState.AddModelError("NewSchedule.StartDate", "Start date is required.");
        if (!NewSchedule.Amount.HasValue || NewSchedule.Amount.Value <= 0)
            ModelState.AddModelError("NewSchedule.Amount", "Amount must be greater than 0.");
        if (NewSchedule.EndDate.HasValue && NewSchedule.StartDate.HasValue && NewSchedule.EndDate.Value.Date < NewSchedule.StartDate.Value.Date)
            ModelState.AddModelError("NewSchedule.EndDate", "End date must be on or after Start date.");
        if (NewSchedule.DayOfMonth.HasValue && (NewSchedule.DayOfMonth < 1 || NewSchedule.DayOfMonth > 31))
            ModelState.AddModelError("NewSchedule.DayOfMonth", "Day of month must be between 1 and 31.");

        // Overlap validation
    if (ModelState.IsValid)
        {
            var ns = new ContributionSchedule
            {
                InvestmentId = inv.Id,
                StartDate = NewSchedule.StartDate!.Value.Date,
                EndDate = NewSchedule.EndDate?.Date,
                Amount = NewSchedule.Amount!.Value,
                Frequency = ContributionFrequency.Monthly,
                DayOfMonth = NewSchedule.DayOfMonth ?? NewSchedule.StartDate!.Value.Day
            };

            var newStart = ns.StartDate;
            var newEnd = ns.EndDate ?? DateTime.MaxValue.Date;

            bool overlaps = inv.Schedules.Any(s =>
                newStart <= (s.EndDate?.Date ?? DateTime.MaxValue.Date) && s.StartDate.Date <= newEnd);
            if (overlaps)
            {
                ModelState.AddModelError(string.Empty, "New schedule overlaps an existing schedule.");
            }
            else
            {
                db.ContributionSchedules.Add(ns);
                await db.SaveChangesAsync();
                return RedirectToPage(new { id });
            }
        }

        // Reload page data on validation errors
    Investment = inv;
    Schedules = inv.Schedules.OrderBy(s => s.StartDate).ToList();
    Contributions = await db.OneTimeContributions.Where(c => c.InvestmentId == inv.Id).OrderByDescending(c => c.Date).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteScheduleAsync(int id, int scheduleId)
    {
        var sched = await db.ContributionSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId && s.InvestmentId == id);
        if (sched is not null)
        {
            db.ContributionSchedules.Remove(sched);
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateScheduleAsync(int id, int scheduleId, DateTime startDate, DateTime? endDate, int? dayOfMonth, decimal amount)
    {
    var inv = await db.Investments.Include(i => i.Schedules).FirstOrDefaultAsync(i => i.Id == id);
        if (inv is null) return RedirectToPage("Index");
        var sched = inv.Schedules.FirstOrDefault(s => s.Id == scheduleId);
        if (sched is null) return RedirectToPage(new { id });

    // Clear any unrelated model state (e.g., Investment.Name required)
    ModelState.Clear();

    // Ignore unrelated Investment validation during schedule update
    ModelState.Remove("Investment");
    ModelState.Remove("Investment.Name");
    ModelState.Remove("Investment.Type");
    ModelState.Remove("Investment.Currency");
    ModelState.Remove("Investment.Provider");

        if (amount <= 0) ModelState.AddModelError(string.Empty, "Amount must be > 0");
        if (dayOfMonth.HasValue && (dayOfMonth < 1 || dayOfMonth > 31)) ModelState.AddModelError(string.Empty, "Day must be 1-31");
        if (endDate.HasValue && endDate.Value.Date < startDate.Date) ModelState.AddModelError(string.Empty, "End before Start");

        var newStart = startDate.Date;
        var newEnd = endDate?.Date ?? DateTime.MaxValue.Date;
        bool overlaps = inv.Schedules.Any(s => s.Id != scheduleId && newStart <= (s.EndDate?.Date ?? DateTime.MaxValue.Date) && s.StartDate.Date <= newEnd);
        if (overlaps) ModelState.AddModelError(string.Empty, "Overlaps another schedule");

        if (!ModelState.IsValid)
        {
            Investment = inv;
            Schedules = inv.Schedules.OrderBy(s => s.StartDate).ToList();
            Contributions = await db.OneTimeContributions.Where(c => c.InvestmentId == inv.Id).OrderByDescending(c => c.Date).ToListAsync();
            return Page();
        }

        sched.StartDate = newStart;
        sched.EndDate = endDate?.Date;
        sched.DayOfMonth = dayOfMonth ?? startDate.Day;
        sched.Amount = amount;
        await db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddContributionAsync(int id)
    {
        // Avoid unrelated Investment validation
        ModelState.Clear();
        ModelState.Remove(nameof(Investment));

        if (NewContribution.Amount <= 0)
        {
            ModelState.AddModelError("NewContribution.Amount", "Amount must be greater than 0.");
        }
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
}
