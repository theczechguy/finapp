using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InvestmentTracker.Models;
using InvestmentTracker.ViewModels;

namespace InvestmentTracker.Services
{
    public interface IExpenseService
    {
        Task<MonthlyExpenseViewModel> GetMonthlyDataAsync(int year, int month);
        Task AddIncomeSourceAsync(IncomeSource incomeSource);
        Task UpdateIncomeSourceAsync(IncomeSource incomeSource);
        Task LogOrUpdateMonthlyIncomeAsync(int incomeSourceId, int year, int month, decimal actualAmount);
        Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync();
        Task AddExpenseCategoryAsync(ExpenseCategory category);
        Task<ExpenseCategory?> GetExpenseCategoryAsync(int id);
        Task UpdateExpenseCategoryAsync(ExpenseCategory category);
        Task DeleteExpenseCategoryAsync(int id);
        Task<IEnumerable<IncomeSource>> GetAllIncomeSourcesAsync();
        Task<IncomeSource?> GetIncomeSourceAsync(int id);
        Task DeleteIncomeSourceAsync(int id);
        Task AddRegularExpenseAsync(RegularExpense expense);
        Task UpdateRegularExpenseAsync(RegularExpense expense);
        Task AddIrregularExpenseAsync(IrregularExpense expense);
        Task DeleteIrregularExpenseAsync(int expenseId);
    }
}
