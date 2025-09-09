using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvestmentTracker.Pages.Expenses;

public class ExpenseCategoriesModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public ExpenseCategoriesModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public IList<ExpenseCategory> ExpenseCategories { get; set; } = new List<ExpenseCategory>();

    [BindProperty]
    public ExpenseCategory NewCategory { get; set; } = new();

    public async Task OnGetAsync()
    {
        ExpenseCategories = (await _expenseService.GetExpenseCategoriesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ExpenseCategories = (await _expenseService.GetExpenseCategoriesAsync()).ToList();
            return Page();
        }

        await _expenseService.AddExpenseCategoryAsync(NewCategory);
        TempData["ToastSuccess"] = "Category added.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        // Note: In a real application, you might want to check if the category is being used
        // before allowing deletion, or handle cascading deletes appropriately
        var category = await _expenseService.GetExpenseCategoryAsync(id);
        if (category != null)
        {
            // For now, we'll just remove it - in production you'd want better error handling
            await _expenseService.DeleteExpenseCategoryAsync(id);
        }
        TempData["ToastSuccess"] = "Category deleted.";
        return RedirectToPage();
    }
}
