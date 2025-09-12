using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvestmentTracker.Pages.Expenses
{
    public class FinancialScheduleConfigModel : PageModel
    {
        private readonly InvestmentTracker.Data.AppDbContext _context;

        public FinancialScheduleConfigModel(InvestmentTracker.Data.AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string ScheduleType { get; set; } = "Calendar";

        [BindProperty]
        public int StartDay { get; set; } = 1;

        public void OnGet()
        {
            var config = _context.FinancialScheduleConfigs.FirstOrDefault();
            if (config != null)
            {
                ScheduleType = config.ScheduleType;
                StartDay = config.StartDay;
            }
        }

        public IActionResult OnPost()
        {
            var config = _context.FinancialScheduleConfigs.FirstOrDefault();
            if (config == null)
            {
                config = new InvestmentTracker.Models.FinancialScheduleConfig
                {
                    ScheduleType = ScheduleType,
                    StartDay = StartDay
                };
                _context.FinancialScheduleConfigs.Add(config);
            }
            else
            {
                config.ScheduleType = ScheduleType;
                config.StartDay = StartDay;
                _context.FinancialScheduleConfigs.Update(config);
            }
            _context.SaveChanges();
            TempData["ToastSuccess"] = "Financial schedule configuration saved.";
            return RedirectToPage();
        }
    }
}
