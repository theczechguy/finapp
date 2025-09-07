using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvestmentTracker.Pages.Expenses;

public class EditExpenseCategoryModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public EditExpenseCategoryModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [BindProperty]
    public ExpenseCategory Category { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var category = await _expenseService.GetExpenseCategoryAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        Category = category;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _expenseService.UpdateExpenseCategoryAsync(Category);
        return RedirectToPage("./ExpenseCategories");
    }
}
