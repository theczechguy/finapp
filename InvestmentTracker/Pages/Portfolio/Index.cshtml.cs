using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System;
using Microsoft.AspNetCore.Mvc;

namespace InvestmentTracker.Pages.Portfolio
{
    public class IndexModel : PageModel
    {
        private readonly IInvestmentService _investmentService;

        public IndexModel(IInvestmentService investmentService)
        {
            _investmentService = investmentService;
        }

        public List<InvestmentWithLatestValue> InvestmentsWithLatestValue { get; set; } = new();
        public Dictionary<Currency, decimal> TotalsByCurrency { get; set; } = new();
        public Dictionary<InvestmentCategory, Dictionary<Currency, decimal>> TotalsByCategory { get; set; } = new();
        public Dictionary<DateTime, decimal> TotalsByDate { get; set; } = new();
        public string ChartDataJson { get; set; } = "{}";
        public string ChartTimeSeriesJson { get; set; } = "{}";

        [BindProperty(SupportsGet = true)]
        public string TimeRange { get; set; } = "12m";

        public async Task OnGetAsync()
        {
            var investments = await _investmentService.GetAllInvestmentsAsync();

            var fromDate = DateTime.Today;
            switch (TimeRange)
            {
                case "6m":
                    fromDate = fromDate.AddMonths(-6);
                    break;
                case "2m":
                    fromDate = fromDate.AddMonths(-2);
                    break;
                case "12m":
                default:
                    fromDate = fromDate.AddMonths(-12);
                    break;
            }

            // Prepare time series data with forward-filling for missing values
            var valuesForChart = await _investmentService.GetInvestmentValuesFromDateAsync(fromDate);
            TotalsByDate = CalculatePortfolioValuesWithForwardFill(valuesForChart, fromDate);
            ChartTimeSeriesJson = JsonSerializer.Serialize(TotalsByDate.ToDictionary(kv => kv.Key.ToString("yyyy-MM-dd"), kv => kv.Value));

            var investmentsWithValues = new List<InvestmentWithLatestValue>();

            foreach (var investment in investments)
            {
                // Get the latest value for this investment
                var values = await _investmentService.GetInvestmentValuesAsync(investment.Id);
                var latestValue = values
                    .Select(v => new InvestmentValue
                    {
                        Id = (int)v.GetType().GetProperty("Id")?.GetValue(v)!,
                        AsOf = (DateTime)v.GetType().GetProperty("AsOf")?.GetValue(v)!,
                        Value = (decimal)v.GetType().GetProperty("Value")?.GetValue(v)!,
                        InvestmentId = investment.Id
                    })
                    .OrderByDescending(v => v.AsOf)
                    .FirstOrDefault();

                var investmentWithValue = new InvestmentWithLatestValue
                {
                    Id = investment.Id,
                    Name = investment.Name,
                    Category = investment.Category,
                    Type = investment.Type,
                    Currency = investment.Currency,
                    Provider = investment.Provider,
                    ChargeAmount = investment.ChargeAmount,
                    LatestValue = latestValue
                };

                investmentsWithValues.Add(investmentWithValue);

                if (latestValue != null)
                {
                    TotalsByCurrency.TryGetValue(investment.Currency, out var currentTotal);
                    TotalsByCurrency[investment.Currency] = currentTotal + latestValue.Value;
                    
                    if (!TotalsByCategory.ContainsKey(investment.Category))
                    {
                        TotalsByCategory[investment.Category] = new Dictionary<Currency, decimal>();
                    }
                    TotalsByCategory[investment.Category].TryGetValue(investment.Currency, out var currentCategoryTotal);
                    TotalsByCategory[investment.Category][investment.Currency] = currentCategoryTotal + latestValue.Value;
                }
            }

            InvestmentsWithLatestValue = investmentsWithValues;

            // Prepare chart data
            var chartData = new Dictionary<string, object>();
            foreach (var currency in TotalsByCurrency.Where(kv => kv.Value > 0m).Select(kv => kv.Key))
            {
                var categoryData = new Dictionary<string, decimal>();
                foreach (var category in TotalsByCategory)
                {
                    if (category.Value.ContainsKey(currency))
                    {
                        categoryData[category.Key.ToString()] = category.Value[currency];
                    }
                }
                if (categoryData.Any())
                {
                    chartData[currency.ToString()] = categoryData;
                }
            }
            ChartDataJson = JsonSerializer.Serialize(chartData);
        }

        public async Task<JsonResult> OnGetChartDataAsync(string timeRange)
        {
            var fromDate = DateTime.Today;
            switch (timeRange)
            {
                case "6m":
                    fromDate = fromDate.AddMonths(-6);
                    break;
                case "2m":
                    fromDate = fromDate.AddMonths(-2);
                    break;
                case "12m":
                default:
                    fromDate = fromDate.AddMonths(-12);
                    break;
            }

            var valuesForChart = await _investmentService.GetInvestmentValuesFromDateAsync(fromDate);
            var totalsByDate = CalculatePortfolioValuesWithForwardFill(valuesForChart, fromDate);
    
            var chartTimeSeriesJson = totalsByDate.ToDictionary(kv => kv.Key.ToString("yyyy-MM-dd"), kv => kv.Value);

            return new JsonResult(chartTimeSeriesJson);
        }

        private Dictionary<DateTime, decimal> CalculatePortfolioValuesWithForwardFill(List<InvestmentValue> allValues, DateTime fromDate)
        {
            // Group values by investment ID and sort by date
            var valuesByInvestment = allValues
                .GroupBy(v => v.InvestmentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(v => v.AsOf.Date).ToList()
                );

            // Get all unique dates in the range
            var allDates = new HashSet<DateTime>();
            foreach (var investmentValues in valuesByInvestment.Values)
            {
                foreach (var value in investmentValues)
                {
                    if (value.AsOf.Date >= fromDate)
                    {
                        allDates.Add(value.AsOf.Date);
                    }
                }
            }

            var sortedDates = allDates.OrderBy(d => d).ToList();
            var result = new Dictionary<DateTime, decimal>();

            foreach (var date in sortedDates)
            {
                decimal totalValue = 0;

                foreach (var investmentId in valuesByInvestment.Keys)
                {
                    var investmentValues = valuesByInvestment[investmentId];
                    
                    // Find the most recent value for this investment up to and including this date
                    var mostRecentValue = investmentValues
                        .Where(v => v.AsOf.Date <= date)
                        .OrderByDescending(v => v.AsOf.Date)
                        .FirstOrDefault();

                    if (mostRecentValue != null)
                    {
                        totalValue += mostRecentValue.Value;
                    }
                }

                if (totalValue > 0) // Only include dates where we have at least some data
                {
                    result[date] = totalValue;
                }
            }

            return result;
        }
    }
}