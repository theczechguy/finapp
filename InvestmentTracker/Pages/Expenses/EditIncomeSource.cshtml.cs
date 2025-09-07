using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvestmentTracker.Pages.Expenses;

public class EditIncomeSourceModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public EditIncomeSourceModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [BindProperty]
    public IncomeSource IncomeSource { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var incomeSource = await _expenseService.GetIncomeSourceAsync(id);
        if (incomeSource == null)
        {
            return NotFound();
        }
        IncomeSource = incomeSource;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _expenseService.UpdateIncomeSourceAsync(IncomeSource);

        return RedirectToPage("./IncomeSources");
    }
}
