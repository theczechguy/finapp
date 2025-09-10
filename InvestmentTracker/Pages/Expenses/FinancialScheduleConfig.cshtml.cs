using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvestmentTracker.Pages.Expenses
{
    public class FinancialScheduleConfigModel : PageModel
    {
        [BindProperty]
        public string ScheduleType { get; set; } = "Calendar";

        [BindProperty]
        public int StartDay { get; set; } = 1;

    // ScheduleLength removed; only StartDay is needed for custom schedule

        public void OnGet()
        {
            // Load existing config if available (future enhancement)
        }

        public IActionResult OnPost()
        {
            // Save config (future: persist to DB or user profile)
            TempData["ToastSuccess"] = "Financial schedule configuration saved.";
            return RedirectToPage();
        }
    }
}
