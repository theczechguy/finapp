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
        Task<FinancialScheduleConfig?> GetFinancialScheduleConfigAsync();
        Task AddIncomeSourceAsync(IncomeSource incomeSource);
        Task UpdateIncomeSourceAsync(IncomeSource incomeSource);
        Task LogOrUpdateMonthlyIncomeAsync(int incomeSourceId, int year, int month, decimal actualAmount);
        Task AddOneTimeIncomeAsync(OneTimeIncome income);
        Task<OneTimeIncome?> GetOneTimeIncomeAsync(int id);
        Task UpdateOneTimeIncomeAsync(OneTimeIncome income);
        Task DeleteOneTimeIncomeAsync(int incomeId);
        Task<IEnumerable<OneTimeIncome>> GetOneTimeIncomesForMonthAsync(int year, int month);
        Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync();
        Task AddExpenseCategoryAsync(ExpenseCategory category);
        Task<ExpenseCategory?> GetExpenseCategoryAsync(int id);
        Task UpdateExpenseCategoryAsync(ExpenseCategory category);
        Task DeleteExpenseCategoryAsync(int id);
        Task SeedDefaultCategoriesAsync();
        Task<IEnumerable<IncomeSource>> GetAllIncomeSourcesAsync();
        Task<IncomeSource?> GetIncomeSourceAsync(int id);
        Task DeleteIncomeSourceAsync(int id);
        Task AddRegularExpenseAsync(RegularExpense expense);
        Task<RegularExpense?> GetRegularExpenseAsync(int id);
        Task UpdateRegularExpenseAsync(RegularExpense expense);
        Task AddIrregularExpenseAsync(IrregularExpense expense);
        Task<IrregularExpense?> GetIrregularExpenseAsync(int id);
        Task UpdateIrregularExpenseAsync(IrregularExpense expense);
        Task DeleteIrregularExpenseAsync(int expenseId);
        Task UpdateRegularExpenseScheduleAsync(int expenseId, decimal amount, Frequency frequency, DateTime startDate);
        Task<IEnumerable<RegularExpense>> GetAllRegularExpensesAsync();
        Task<IEnumerable<FamilyMember>> GetFamilyMembersAsync();
        Task AddFamilyMemberAsync(FamilyMember familyMember);
        Task<FamilyMember?> GetFamilyMemberAsync(int id);
        Task UpdateFamilyMemberAsync(FamilyMember familyMember);

    // Budgets
    Task<List<CategoryBudget>> GetEffectiveBudgetsAsync(int year, int month);
    Task SetCategoryBudgetAsync(int categoryId, decimal amount, int year, int month, bool applyToFuture);
    Task DeleteCategoryBudgetAsync(int categoryId, int year, int month);
    Task<List<BudgetHistoryItem>> GetBudgetHistoryAsync(int categoryId);
    }
}
