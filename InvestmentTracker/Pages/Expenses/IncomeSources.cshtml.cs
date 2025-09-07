using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvestmentTracker.Pages.Expenses;

public class IncomeSourcesModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public IncomeSourcesModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public IList<IncomeSource> IncomeSources { get; set; } = new List<IncomeSource>();

    [BindProperty]
    public IncomeSource NewIncomeSource { get; set; } = new();

    public async Task OnGetAsync()
    {
        IncomeSources = (await _expenseService.GetAllIncomeSourcesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            IncomeSources = (await _expenseService.GetAllIncomeSourcesAsync()).ToList();
            return Page();
        }

        await _expenseService.AddIncomeSourceAsync(NewIncomeSource);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await _expenseService.DeleteIncomeSourceAsync(id);
        return RedirectToPage();
    }
}
