using Microsoft.AspNetCore.Mvc.RazorPages;
using InvestmentTracker.Services;
using InvestmentTracker.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvestmentTracker.Pages.Analytics
{
    public class IndexModel : PageModel
    {
        private readonly IExpenseService _expenseService;

        public IndexModel(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        public List<CategoryExpenseData> CategoryExpenses { get; set; } = new();
        public List<MonthlyExpenseData> MonthlyExpenses { get; set; } = new();
        public MonthlyComparisonData MonthlyComparison { get; set; } = new();
        public decimal TotalExpenses { get; set; }
        public int TotalTransactions { get; set; }

        public async Task OnGetAsync()
        {
            CategoryExpenses = await _expenseService.GetCategoryExpenseDataAsync();
            MonthlyExpenses = await _expenseService.GetMonthlyExpenseTrendsAsync();
            MonthlyComparison = await _expenseService.GetMonthlyComparisonAsync();
            
            TotalExpenses = CategoryExpenses.Sum(c => c.Amount);
            TotalTransactions = CategoryExpenses.Count + MonthlyExpenses.Count(m => m.Amount > 0);
        }
    }
}