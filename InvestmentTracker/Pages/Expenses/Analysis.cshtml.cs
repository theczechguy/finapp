using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace InvestmentTracker.Pages.Expenses;

public class AnalysisModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public AnalysisModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? SelectedDate { get; set; }

    public string ChartDataJson { get; private set; } = "{}";
    public string RegularChartDataJson { get; private set; } = "{}";
    public DateTime FinancialMonthStartDate { get; private set; }
    public DateTime FinancialMonthEndDate { get; private set; }
    public FinancialScheduleConfig? ScheduleConfig { get; private set; }
    public string TrendsChartDataJson { get; private set; } = "{}";

    public async Task OnGetAsync()
    {
        DateTime selectedDate;

        if (SelectedDate.HasValue)
        {
            selectedDate = SelectedDate.Value;
        }
        else
        {
            // When no date is specified, determine the default based on schedule configuration
            selectedDate = await _expenseService.GetDefaultSelectedDateAsync();
        }

        SelectedDate = selectedDate;

        // Load schedule config for display
        ScheduleConfig = await _expenseService.GetFinancialScheduleConfigAsync();

        // Calculate the actual date range for the financial month
        var (startDate, endDate) = await _expenseService.CalculateFinancialMonthDatesAsync(selectedDate.Year, selectedDate.Month);
        FinancialMonthStartDate = startDate;
        FinancialMonthEndDate = endDate;

        var analysisData = await _expenseService.GetIrregularExpenseAnalysisAsync(FinancialMonthStartDate, FinancialMonthEndDate);

        var colors = new[] { "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0", "#9966FF", "#FF9F40", "#FF6384", "#C9CBCF", "#4BC0C0", "#FF6384" };

        var chartData = new
        {
            labels = analysisData.Select(d => d.CategoryName),
            datasets = new[]
            {
                new
                {
                    label = "Expenses by Category",
                    data = analysisData.Select(d => d.TotalAmount),
                    backgroundColor = analysisData.Select((d, index) => colors[index % colors.Length])
                }
            }
        };

        ChartDataJson = JsonSerializer.Serialize(chartData);

        // Get trend data for the last 12 months
        var trendData = await _expenseService.GetMonthlyExpenseTrendsAsync(12);

        var trendsChartData = new
        {
            labels = trendData.Select(t => t.PeriodLabel).ToArray(),
            datasets = new object[]
            {
                new
                {
                    label = "Regular Expenses",
                    data = trendData.Select(t => t.RegularExpenses).ToArray(),
                    borderColor = "#36A2EB",
                    backgroundColor = "rgba(54, 162, 235, 0.1)",
                    tension = 0.4
                },
                new
                {
                    label = "Irregular Expenses",
                    data = trendData.Select(t => t.IrregularExpenses).ToArray(),
                    borderColor = "#FF6384",
                    backgroundColor = "rgba(255, 99, 132, 0.1)",
                    tension = 0.4
                },
                new
                {
                    label = "Total Expenses",
                    data = trendData.Select(t => t.TotalExpenses).ToArray(),
                    borderColor = "#FFCE56",
                    backgroundColor = "rgba(255, 206, 86, 0.1)",
                    borderDash = new[] { 5, 5 },
                    tension = 0.4
                }
            }
        };

        TrendsChartDataJson = JsonSerializer.Serialize(trendsChartData);

        // Get regular expense analysis data
        var regularAnalysisData = await _expenseService.GetRegularExpenseAnalysisAsync(FinancialMonthStartDate, FinancialMonthEndDate);

        var regularChartData = new
        {
            labels = regularAnalysisData.Select(d => d.CategoryName),
            datasets = new[]
            {
                new
                {
                    label = "Regular Expenses by Category",
                    data = regularAnalysisData.Select(d => d.TotalAmount),
                    backgroundColor = analysisData.Select((d, index) => colors[(index + 3) % colors.Length]) // Offset colors to differentiate from irregular
                }
            }
        };

        RegularChartDataJson = JsonSerializer.Serialize(regularChartData);
    }


}