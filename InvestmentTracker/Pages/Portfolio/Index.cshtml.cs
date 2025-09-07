using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task OnGetAsync()
        {
            var investments = await _investmentService.GetAllInvestmentsAsync();
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
        }
    }
}
