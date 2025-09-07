using System;
using System.Threading.Tasks;
using InvestmentTracker.Services;
using InvestmentTracker.ViewModels;
using InvestmentTracker.Models;
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

            // Seed default categories if needed
            await _expenseService.SeedDefaultCategoriesAsync();

            ViewModel = await _expenseService.GetMonthlyDataAsync(selectedYear, selectedMonth);
        }

        public async Task<IActionResult> OnPostUpdateIncomeAsync(int incomeSourceId, decimal actualAmount, int year, int month)
        {
            await _expenseService.LogOrUpdateMonthlyIncomeAsync(incomeSourceId, year, month, actualAmount);
            return RedirectToPage(new { year, month });
        }

        public async Task<IActionResult> OnPostAddRegularExpenseAsync(string name, decimal amount, int categoryId, string frequency, int startYear, int startMonth, string currency)
        {
            var expense = new RegularExpense
            {
                Name = name,
                ExpenseCategoryId = categoryId,
                Currency = Enum.Parse<Currency>(currency)
            };

            // Create initial schedule with month-based fields
            expense.Schedules.Add(new ExpenseSchedule
            {
                StartYear = startYear,
                StartMonth = startMonth,
                Amount = amount,
                Frequency = Enum.Parse<Frequency>(frequency)
            });

            await _expenseService.AddRegularExpenseAsync(expense);
            return RedirectToPage(new { Year, Month });
        }

        public async Task<IActionResult> OnPostAddIrregularExpenseAsync(string name, decimal amount, int categoryId, DateTime date, string currency)
        {
            var expense = new IrregularExpense
            {
                Name = name,
                Amount = amount,
                ExpenseCategoryId = categoryId,
                Date = date,
                Currency = Enum.Parse<Currency>(currency)
            };

            await _expenseService.AddIrregularExpenseAsync(expense);
            return RedirectToPage(new { Year, Month });
        }

        public async Task<IActionResult> OnPostDeleteIrregularExpenseAsync(int expenseId)
        {
            await _expenseService.DeleteIrregularExpenseAsync(expenseId);
            return RedirectToPage(new { Year, Month });
        }

        public async Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync()
        {
            return await _expenseService.GetExpenseCategoriesAsync();
        }

        public async Task<IActionResult> OnPostUpdateRegularExpenseAsync(int id, string name, decimal amount, int categoryId, string frequency, int startYear, int startMonth, string currency)
        {
            var existingExpense = await _expenseService.GetRegularExpenseAsync(id);
            if (existingExpense == null)
            {
                return NotFound();
            }

            // Update basic properties
            existingExpense.Name = name;
            existingExpense.ExpenseCategoryId = categoryId;
            existingExpense.Currency = Enum.Parse<Currency>(currency);

            // Handle schedule updates with temporal logic
            var startDate = new DateTime(startYear, startMonth, 1);
            await _expenseService.UpdateRegularExpenseScheduleAsync(id, amount, Enum.Parse<Frequency>(frequency), startDate);
            return RedirectToPage(new { Year, Month });
        }

        public async Task<IActionResult> OnPostUpdateIrregularExpenseAsync(int id, string name, decimal amount, int categoryId, DateTime date, string currency)
        {
            var existingExpense = await _expenseService.GetIrregularExpenseAsync(id);
            if (existingExpense == null)
            {
                return NotFound();
            }

            existingExpense.Name = name;
            existingExpense.Amount = amount;
            existingExpense.ExpenseCategoryId = categoryId;
            existingExpense.Date = date;
            existingExpense.Currency = Enum.Parse<Currency>(currency);

            await _expenseService.UpdateIrregularExpenseAsync(existingExpense);
            return RedirectToPage(new { Year, Month });
        }

        public async Task<IEnumerable<RegularExpense>> GetAllRegularExpensesAsync()
        {
            return await _expenseService.GetAllRegularExpensesAsync();
        }
    }
}
