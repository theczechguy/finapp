using InvestmentTracker.Data;
using InvestmentTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Pages.Expenses
{
    public class UserAccountsModel : PageModel
    {
        private readonly AppDbContext _context;

        public UserAccountsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<UserAccount> UserAccounts { get; set; } = new();

        public async Task OnGetAsync()
        {
            UserAccounts = await _context.UserAccounts
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync(UserAccount account)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Basic duplicate check
            if (await _context.UserAccounts.AnyAsync(a => a.AccountNumber == account.AccountNumber))
            {
                TempData["ToastError"] = "Account number already exists.";
                return RedirectToPage();
            }

            _context.UserAccounts.Add(account);
            await _context.SaveChangesAsync();

            TempData["ToastSuccess"] = "Account added successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync(UserAccount account)
        {
            var existing = await _context.UserAccounts.FindAsync(account.Id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = account.Name;
            existing.AccountNumber = account.AccountNumber;
            existing.BankName = account.BankName;
            existing.IsActive = account.IsActive;

            await _context.SaveChangesAsync();

            TempData["ToastSuccess"] = "Account updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var account = await _context.UserAccounts.FindAsync(id);
            if (account != null)
            {
                _context.UserAccounts.Remove(account);
                await _context.SaveChangesAsync();
                TempData["ToastSuccess"] = "Account deleted successfully.";
            }

            return RedirectToPage();
        }
    }
}
