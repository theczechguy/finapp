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
        Task AddRegularExpenseAsync(RegularExpense expense);
        Task UpdateRegularExpenseAsync(RegularExpense expense);
        Task AddIrregularExpenseAsync(IrregularExpense expense);
        Task DeleteIrregularExpenseAsync(int expenseId);
    }
}
