using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace InvestmentTracker.Pages;

public class IndexModel : PageModel
{
    private readonly IInvestmentService _investmentService;
    private readonly IExpenseService _expenseService;

    public IndexModel(IInvestmentService investmentService, IExpenseService expenseService)
    {
        _investmentService = investmentService;
        _expenseService = expenseService;
    }

    public Dictionary<Currency, decimal> TotalNetWorthByCurrency { get; set; } = new();
    public decimal CurrentMonthExpenses { get; set; }
    public List<TransactionViewModel> RecentTransactions { get; set; } = new();
    public string NetWorthHistoryJson { get; set; } = "{}";

    public async Task OnGetAsync()
    {
        await LoadNetWorthDataAsync();
        await LoadMonthlyExpensesAsync();
        await LoadRecentTransactionsAsync();
        await LoadNetWorthHistoryAsync();
    }

    private async Task LoadNetWorthDataAsync()
    {
        var investments = await _investmentService.GetAllInvestmentsAsync();
        foreach (var investment in investments)
        {
            var values = await _investmentService.GetInvestmentValuesAsync(investment.Id);
            var latestValue = values
                .Select(v => new { 
                    Value = (decimal)v.GetType().GetProperty("Value")?.GetValue(v)! 
                })
                .LastOrDefault();

            if (latestValue != null)
            {
                if (!TotalNetWorthByCurrency.ContainsKey(investment.Currency))
                {
                    TotalNetWorthByCurrency[investment.Currency] = 0;
                }
                TotalNetWorthByCurrency[investment.Currency] += latestValue.Value;
            }
        }
    }

    private async Task LoadMonthlyExpensesAsync()
    {
        var today = DateTime.Today;
        var monthlyData = await _expenseService.GetMonthlyDataAsync(today.Year, today.Month);
        CurrentMonthExpenses = monthlyData.TotalExpenses;
    }

    private async Task LoadRecentTransactionsAsync()
    {
        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-30);

        var expenses = await _expenseService.GetIrregularExpensesForDateRangeAsync(startDate, endDate);
        var incomes = await _expenseService.GetOneTimeIncomesForDateRangeAsync(startDate, endDate);

        var transactions = new List<TransactionViewModel>();

        transactions.AddRange(expenses.Select(e => new TransactionViewModel
        {
            Date = e.Date,
            Name = e.Name,
            Amount = e.Amount,
            Currency = e.Currency,
            Type = "Expense",
            Category = e.Category?.Name ?? "Uncategorized"
        }));

        transactions.AddRange(incomes.Select(i => new TransactionViewModel
        {
            Date = i.Date,
            Name = i.Name,
            Amount = i.Amount,
            Currency = i.Currency,
            Type = "Income",
            Category = i.IncomeSource?.Name ?? "One-Time Income"
        }));

        RecentTransactions = transactions
            .OrderByDescending(t => t.Date)
            .Take(5)
            .ToList();
    }

    private async Task LoadNetWorthHistoryAsync()
    {
        var fromDate = DateTime.Today.AddMonths(-6);
        var allValues = await _investmentService.GetInvestmentValuesFromDateAsync(fromDate);
        
        // Group values by investment ID and sort by date
        var valuesByInvestment = allValues
            .GroupBy(v => v.InvestmentId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(v => v.AsOf.Date).ToList()
            );

        // Get all unique dates in the range
        var allDates = allValues.Select(v => v.AsOf.Date).Distinct().OrderBy(d => d).ToList();
        
        var totalSeries = new Dictionary<string, decimal>();

        foreach (var date in allDates)
        {
            decimal total = 0;
            foreach (var investmentId in valuesByInvestment.Keys)
            {
                var investmentValues = valuesByInvestment[investmentId];
                var mostRecentValue = investmentValues
                    .Where(v => v.AsOf.Date <= date)
                    .OrderByDescending(v => v.AsOf.Date)
                    .FirstOrDefault();

                if (mostRecentValue != null)
                {
                    total += mostRecentValue.Value;
                }
            }
            totalSeries[date.ToString("yyyy-MM-dd")] = total;
        }

        var chartDataObj = new {
            labels = totalSeries.Keys.ToList(),
            data = totalSeries.Values.ToList()
        };

        NetWorthHistoryJson = JsonSerializer.Serialize(chartDataObj);
    }

    public class TransactionViewModel
    {
        public DateTime Date { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Currency Currency { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
