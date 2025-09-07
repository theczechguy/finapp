using System;
using System.Threading.Tasks;
using InvestmentTracker.Services;
using InvestmentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvestmentTracker.Pages.Expenses
{
    public class IndexModel : PageModel
    {
        private readonly IExpenseService _expenseService;

        public IndexModel(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [BindProperty(SupportsGet = true)]
        public int? Year { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Month { get; set; }

        public MonthlyExpenseViewModel ViewModel { get; set; } = new();

        public async Task OnGetAsync()
        {
            var selectedYear = Year ?? DateTime.Today.Year;
            var selectedMonth = Month ?? DateTime.Today.Month;

            ViewModel = await _expenseService.GetMonthlyDataAsync(selectedYear, selectedMonth);
        }

        public async Task<IActionResult> OnPostUpdateIncomeAsync(int incomeSourceId, decimal actualAmount, int year, int month)
        {
            await _expenseService.LogOrUpdateMonthlyIncomeAsync(incomeSourceId, year, month, actualAmount);
            return RedirectToPage(new { year, month });
        }
    }
}
