using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvestmentTracker.Pages.Investments;

public class EditModel(IInvestmentService investmentService) : PageModel
{
    [BindProperty]
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
        var entity = await investmentService.GetInvestmentAsync(id);
        if (entity is null) return RedirectToPage("./List");
        Investment = entity;
        Schedules = entity.Schedules.OrderBy(s => s.StartDate).ToList();
        Contributions = entity.OneTimeContributions.OrderByDescending(c => c.Date).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        ModelState.Remove("NewContribution.Amount");
        ModelState.Remove("NewSchedule.Amount");
        ModelState.Remove("NewSchedule.StartDate");
        ModelState.Remove("NewSchedule.DayOfMonth");
        
        var investmentToUpdate = await investmentService.GetInvestmentAsync(id);
        if (investmentToUpdate == null)
        {
            return NotFound();
        }

        if (await TryUpdateModelAsync<Investment>(
                investmentToUpdate,
                "Investment",
                i => i.Name, i => i.Provider, i => i.Type, i => i.Category, i => i.Currency, i => i.ChargeAmount))
        {
            await investmentService.UpdateInvestmentAsync(id, investmentToUpdate);
            return RedirectToPage("./List");
        }

        // If TryUpdateModelAsync fails, we need to reload the ancillary data
        Schedules = investmentToUpdate.Schedules.OrderBy(s => s.StartDate).ToList();
        Contributions = investmentToUpdate.OneTimeContributions.OrderByDescending(c => c.Date).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAddScheduleAsync(int id)
    {
        var inv = await investmentService.GetInvestmentAsync(id);
        if (inv is null) return RedirectToPage("Index");

        ModelState.Clear();
        ModelState.Remove("Investment");
        ModelState.Remove("Investment.Name");
        ModelState.Remove("Investment.Type");
        ModelState.Remove("Investment.Currency");
        ModelState.Remove("Investment.Provider");

        if (!NewSchedule.StartDate.HasValue)
            ModelState.AddModelError("NewSchedule.StartDate", "Start date is required.");
        if (!NewSchedule.Amount.HasValue || NewSchedule.Amount.Value <= 0)
            ModelState.AddModelError("NewSchedule.Amount", "Amount must be greater than 0.");
        if (NewSchedule.EndDate.HasValue && NewSchedule.StartDate.HasValue && NewSchedule.EndDate.Value.Date < NewSchedule.StartDate.Value.Date)
            ModelState.AddModelError("NewSchedule.EndDate", "End date must be on or after Start date.");
        if (NewSchedule.DayOfMonth.HasValue && (NewSchedule.DayOfMonth < 1 || NewSchedule.DayOfMonth > 31))
            ModelState.AddModelError("NewSchedule.DayOfMonth", "Day of month must be between 1 and 31.");

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

            var (schedule, error) = await investmentService.AddContributionScheduleAsync(id, ns);

            if (error is not null)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            else
            {
                return RedirectToPage(new { id });
            }
        }

        Investment = inv;
        Schedules = inv.Schedules.OrderBy(s => s.StartDate).ToList();
        Contributions = inv.OneTimeContributions.OrderByDescending(c => c.Date).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteScheduleAsync(int id, int scheduleId)
    {
        await investmentService.DeleteContributionScheduleAsync(id, scheduleId);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddContributionAsync(int id)
    {
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
        
        await investmentService.AddOneTimeContributionAsync(id, NewContribution);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteContributionAsync(int id, int contributionId)
    {
        await investmentService.DeleteOneTimeContributionAsync(id, contributionId);
        return RedirectToPage(new { id });
    }
}
