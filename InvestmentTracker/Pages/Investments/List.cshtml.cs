using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace InvestmentTracker.Pages.Investments;

public class ListModel(IInvestmentService investmentService) : PageModel
{
    public IList<InvestmentSummary> Investments { get;set; } = default!;
    public Dictionary<int, InvestmentValue?> LatestValues { get; set; } = new();

    public async Task OnGetAsync()
    {
        var investments = await investmentService.GetAllInvestmentsAsync();
        Investments = investments.ToList();

        // Populate latest values for each investment so the list page can show them
        foreach (var inv in Investments)
        {
            try
            {
                var values = await investmentService.GetInvestmentValuesAsync(inv.Id);
                var latestValue = values
                    .Select(v => new InvestmentValue
                    {
                        Id = (int)v.GetType().GetProperty("Id")?.GetValue(v)!,
                        AsOf = (DateTime)v.GetType().GetProperty("AsOf")?.GetValue(v)!,
                        Value = (decimal)v.GetType().GetProperty("Value")?.GetValue(v)!,
                        InvestmentId = inv.Id
                    })
                    .OrderByDescending(v => v.AsOf)
                    .FirstOrDefault();

                LatestValues[inv.Id] = latestValue;
            }
            catch
            {
                LatestValues[inv.Id] = null;
            }
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await investmentService.DeleteInvestmentAsync(id);
        return RedirectToPage();
    }
}
