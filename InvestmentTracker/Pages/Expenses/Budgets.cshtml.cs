using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvestmentTracker.Services;
using InvestmentTracker.ViewModels;
using InvestmentTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvestmentTracker.Pages.Expenses
{
    public class BudgetsModel : PageModel
    {
        private readonly IExpenseService _expenseService;

        public BudgetsModel(IExpenseService expenseService)
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

            // Seed default categories if needed
            await _expenseService.SeedDefaultCategoriesAsync();

            ViewModel = await _expenseService.GetMonthlyDataAsync(selectedYear, selectedMonth);
        }

        public async Task<IActionResult> OnPostSetBudgetAsync(int categoryId, decimal amount, int year, int month, string scope)
        {
            var applyToFuture = string.Equals(scope, "future", StringComparison.OrdinalIgnoreCase);
            await _expenseService.SetCategoryBudgetAsync(categoryId, amount, year, month, applyToFuture);
            return RedirectToPage(new { year, month });
        }

        public async Task<IActionResult> OnPostDeleteBudgetAsync(int categoryId, int year, int month)
        {
            await _expenseService.DeleteCategoryBudgetAsync(categoryId, year, month);
            return RedirectToPage(new { year, month });
        }

        public async Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync()
        {
            return await _expenseService.GetExpenseCategoriesAsync();
        }
    }
}
