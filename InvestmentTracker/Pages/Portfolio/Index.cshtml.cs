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
        public Dictionary<Currency, decimal> SavingsTotalsByCurrency { get; set; } = new();
        public Dictionary<Currency, decimal> GrandTotalsByCurrency { get; set; } = new();
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
            var allValuesForChart = await _investmentService.GetInvestmentValuesFromDateAsync(fromDate);
            var savingsIds = investments.Where(i => i.Category == InvestmentCategory.Savings).Select(i => i.Id).ToHashSet();
            
            var (investmentsSeries, savingsSeries, totalSeries) = CalculatePortfolioSeries(allValuesForChart, fromDate, savingsIds);
            
            var chartDataObj = new {
                labels = totalSeries.Keys.ToList(),
                investments = totalSeries.Keys.Select(k => investmentsSeries[k]).ToList(),
                savings = totalSeries.Keys.Select(k => savingsSeries[k]).ToList(),
                total = totalSeries.Keys.Select(k => totalSeries[k]).ToList()
            };
            ChartTimeSeriesJson = JsonSerializer.Serialize(chartDataObj);

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
                    // Calculate Grand Total (Investments + Savings)
                    GrandTotalsByCurrency.TryGetValue(investment.Currency, out var currentGrandTotal);
                    GrandTotalsByCurrency[investment.Currency] = currentGrandTotal + latestValue.Value;

                    if (investment.Category == InvestmentCategory.Savings)
                    {
                        // Calculate Savings Total
                        SavingsTotalsByCurrency.TryGetValue(investment.Currency, out var currentSavingsTotal);
                        SavingsTotalsByCurrency[investment.Currency] = currentSavingsTotal + latestValue.Value;
                    }
                    else
                    {
                        // Calculate Investment Total (excluding Savings)
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
            }

            InvestmentsWithLatestValue = investmentsWithValues;

            // Prepare chart data (Investments Only)
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

            var investments = await _investmentService.GetAllInvestmentsAsync();
            var savingsIds = investments.Where(i => i.Category == InvestmentCategory.Savings).Select(i => i.Id).ToHashSet();

            var allValuesForChart = await _investmentService.GetInvestmentValuesFromDateAsync(fromDate);
            var (investmentsSeries, savingsSeries, totalSeries) = CalculatePortfolioSeries(allValuesForChart, fromDate, savingsIds);
            
            var chartDataObj = new {
                labels = totalSeries.Keys.ToList(),
                investments = totalSeries.Keys.Select(k => investmentsSeries[k]).ToList(),
                savings = totalSeries.Keys.Select(k => savingsSeries[k]).ToList(),
                total = totalSeries.Keys.Select(k => totalSeries[k]).ToList()
            };

            return new JsonResult(chartDataObj);
        }

        private (Dictionary<string, decimal> Investments, Dictionary<string, decimal> Savings, Dictionary<string, decimal> Total) CalculatePortfolioSeries(List<InvestmentValue> allValues, DateTime fromDate, HashSet<int> savingsIds)
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
            
            var investmentsSeries = new Dictionary<string, decimal>();
            var savingsSeries = new Dictionary<string, decimal>();
            var totalSeries = new Dictionary<string, decimal>();

            foreach (var date in sortedDates)
            {
                decimal investmentsTotal = 0;
                decimal savingsTotal = 0;

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
                        if (savingsIds.Contains(investmentId))
                        {
                            savingsTotal += mostRecentValue.Value;
                        }
                        else
                        {
                            investmentsTotal += mostRecentValue.Value;
                        }
                    }
                }

                string dateKey = date.ToString("yyyy-MM-dd");
                investmentsSeries[dateKey] = investmentsTotal;
                savingsSeries[dateKey] = savingsTotal;
                totalSeries[dateKey] = investmentsTotal + savingsTotal;
            }

            return (investmentsSeries, savingsSeries, totalSeries);
        }
    }
}