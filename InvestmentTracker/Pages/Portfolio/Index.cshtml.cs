using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace InvestmentTracker.Pages.Portfolio
{
    public class IndexModel : PageModel
    {
        private readonly IInvestmentService _investmentService;

        public IndexModel(IInvestmentService investmentService)
        {
            _investmentService = investmentService;
        }

        public List<Investment> InvestmentsWithLatestValue { get; set; } = new();
        public Dictionary<Currency, decimal> TotalsByCurrency { get; set; } = new();
        public Dictionary<InvestmentCategory, Dictionary<Currency, decimal>> TotalsByCategory { get; set; } = new();
        public Dictionary<DateTime, decimal> TotalsByDate { get; set; } = new();
        public string ChartDataJson { get; set; } = "{}";
        public string ChartTimeSeriesJson { get; set; } = "{}";

        public async Task OnGetAsync()
        {
            var investments = await _investmentService.GetAllInvestmentsAsync();

            // Prepare time series data first (before filtering values)
            var allValues = investments.SelectMany(i => i.Values);
            TotalsByDate = allValues.GroupBy(v => v.AsOf.Date)
                .ToDictionary(g => g.Key, g => g.Sum(v => v.Value))
                .OrderBy(kv => kv.Key)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            ChartTimeSeriesJson = JsonSerializer.Serialize(TotalsByDate.ToDictionary(kv => kv.Key.ToString("yyyy-MM-dd"), kv => kv.Value));

            var investmentsWithValues = new List<Investment>();

            foreach (var investment in investments)
            {
                var latestValue = investment.Values.OrderByDescending(v => v.AsOf).FirstOrDefault();
                if (latestValue != null)
                {
                    // We only need the latest value for this page
                    investment.Values = new List<InvestmentValue> { latestValue };
                    investmentsWithValues.Add(investment);

                    TotalsByCurrency.TryGetValue(investment.Currency, out var currentTotal);
                    TotalsByCurrency[investment.Currency] = currentTotal + latestValue.Value;
                    
                    if (!TotalsByCategory.ContainsKey(investment.Category))
                    {
                        TotalsByCategory[investment.Category] = new Dictionary<Currency, decimal>();
                    }
                    TotalsByCategory[investment.Category].TryGetValue(investment.Currency, out var currentCategoryTotal);
                    TotalsByCategory[investment.Category][investment.Currency] = currentCategoryTotal + latestValue.Value;
                }
                else
                {
                    // Add investment even if it has no values to show it in the list
                    investment.Values = new List<InvestmentValue>();
                    investmentsWithValues.Add(investment);
                }
            }

            InvestmentsWithLatestValue = investmentsWithValues;

            // Prepare chart data
            var chartData = new Dictionary<string, object>();
            foreach (var currency in TotalsByCurrency.Keys)
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
    }
}
