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
    public DateTime FinancialMonthStartDate { get; private set; }
    public DateTime FinancialMonthEndDate { get; private set; }
    public FinancialScheduleConfig? ScheduleConfig { get; private set; }

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
            selectedDate = await GetDefaultSelectedDateAsync();
        }

        SelectedDate = selectedDate;

        // Load schedule config for display
        ScheduleConfig = await _expenseService.GetFinancialScheduleConfigAsync();

        // Calculate the actual date range for the financial month
        await CalculateFinancialMonthDatesAsync(selectedDate);

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
    }

    private async Task<DateTime> GetDefaultSelectedDateAsync()
    {
        // Load financial schedule config to determine default view
        var config = await _expenseService.GetFinancialScheduleConfigAsync();
        string scheduleType = config?.ScheduleType ?? "Calendar";
        int startDay = config?.StartDay ?? 1;

        var today = DateTime.Today;

        if (scheduleType == "Custom")
        {
            // For custom schedules, find the start date of the period containing today
            int currentYear = today.Year;
            int currentMonth = today.Month; // 1-based
            int currentDay = today.Day;

            if (currentDay >= startDay)
            {
                // Current date is in the period starting this month
                return new DateTime(currentYear, currentMonth, startDay);
            }
            else
            {
                // Current date is in the period starting last month
                if (currentMonth == 1)
                {
                    // January, so go to December of previous year
                    return new DateTime(currentYear - 1, 12, startDay);
                }
                else
                {
                    return new DateTime(currentYear, currentMonth - 1, startDay);
                }
            }
        }
        else
        {
            // For calendar months, use the first day of current month
            return new DateTime(today.Year, today.Month, 1);
        }
    }

    private async Task CalculateFinancialMonthDatesAsync(DateTime selectedDate)
    {
        // Load financial schedule config
        var config = await _expenseService.GetFinancialScheduleConfigAsync();
        string scheduleType = config?.ScheduleType ?? "Calendar";
        int startDay = config?.StartDay ?? 1;

        if (scheduleType == "Custom")
        {
            // Custom schedule: period starts on startDay of selected month
            FinancialMonthStartDate = new DateTime(selectedDate.Year, selectedDate.Month, startDay);

            // End date is the day before the next period starts
            var nextPeriodStart = FinancialMonthStartDate.AddMonths(1);
            FinancialMonthEndDate = nextPeriodStart.AddDays(-1);
        }
        else
        {
            // Calendar month: standard month boundaries
            FinancialMonthStartDate = new DateTime(selectedDate.Year, selectedDate.Month, 1);
            FinancialMonthEndDate = FinancialMonthStartDate.AddMonths(1).AddDays(-1);
        }
    }
}