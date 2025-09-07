using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvestmentTracker.Data;
using InvestmentTracker.Models;
using InvestmentTracker.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentTracker.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ExpenseService> _logger;

        public ExpenseService(AppDbContext context, ILogger<ExpenseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MonthlyExpenseViewModel> GetMonthlyDataAsync(int year, int month)
        {
            _logger.LogInformation("Fetching expense data for {Year}-{Month}", year, month);
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Incomes
            var allIncomeSources = await _context.IncomeSources.ToListAsync();
            var monthlyIncomes = await _context.MonthlyIncomes
                .Where(i => i.Month.Year == year && i.Month.Month == month)
                .ToDictionaryAsync(i => i.IncomeSourceId);

            var incomeViewModels = allIncomeSources.Select(source => new IncomeViewModel
            {
                IncomeSourceId = source.Id,
                Name = source.Name,
                ExpectedAmount = source.ExpectedAmount,
                ActualAmount = monthlyIncomes.TryGetValue(source.Id, out var income) ? income.ActualAmount : 0,
                Currency = source.Currency
            }).ToList();
            
            var totalIncome = incomeViewModels.Sum(i => i.ActualAmount);

            // Regular Expenses
            var regularExpenses = await _context.RegularExpenses
                .Include(e => e.Category)
                .Where(e => e.StartDate <= endDate && (e.EndDate == null || e.EndDate >= startDate))
                .ToListAsync();

            var applicableRegularExpenses = new List<RegularExpense>();
            foreach (var expense in regularExpenses)
            {
                // This logic will need to be more robust to handle different frequencies
                if (expense.Recurrence == Frequency.Monthly)
                {
                    applicableRegularExpenses.Add(expense);
                }
                // TODO: Add logic for Quarterly, SemiAnnually, Annually
            }
            var totalRegularExpenses = applicableRegularExpenses.Sum(e => e.Amount);

            // Irregular Expenses
            var irregularExpenses = await _context.IrregularExpenses
                .Include(e => e.Category)
                .Where(e => e.Date >= startDate && e.Date <= endDate)
                .ToListAsync();
            var totalIrregularExpenses = irregularExpenses.Sum(e => e.Amount);

            // Totals
            var totalExpenses = totalRegularExpenses + totalIrregularExpenses;

            // Category Breakdown
            var expensesByCategory = new Dictionary<string, decimal>();
            foreach (var expense in applicableRegularExpenses)
            {
                if (expense.Category != null)
                {
                    expensesByCategory.TryGetValue(expense.Category.Name, out var currentTotal);
                    expensesByCategory[expense.Category.Name] = currentTotal + expense.Amount;
                }
            }
            foreach (var expense in irregularExpenses)
            {
                if (expense.Category != null)
                {
                    expensesByCategory.TryGetValue(expense.Category.Name, out var currentTotal);
                    expensesByCategory[expense.Category.Name] = currentTotal + expense.Amount;
                }
            }

            var viewModel = new MonthlyExpenseViewModel
            {
                Year = year,
                Month = month,
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                Incomes = incomeViewModels,
                RegularExpenses = applicableRegularExpenses,
                IrregularExpenses = irregularExpenses,
                ExpensesByCategory = expensesByCategory
            };

            return viewModel;
        }

        public Task AddIncomeSourceAsync(IncomeSource incomeSource)
        {
            _context.Add(incomeSource);
            return _context.SaveChangesAsync();
        }

        public Task UpdateIncomeSourceAsync(IncomeSource incomeSource)
        {
            _context.Update(incomeSource);
            return _context.SaveChangesAsync();
        }

        public async Task LogOrUpdateMonthlyIncomeAsync(int incomeSourceId, int year, int month, decimal actualAmount)
        {
            var monthDate = new DateTime(year, month, 1);
            var existing = await _context.MonthlyIncomes.FirstOrDefaultAsync(m => m.IncomeSourceId == incomeSourceId && m.Month == monthDate);

            if (existing != null)
            {
                existing.ActualAmount = actualAmount;
            }
            else
            {
                var newMonthlyIncome = new MonthlyIncome
                {
                    IncomeSourceId = incomeSourceId,
                    Month = monthDate,
                    ActualAmount = actualAmount
                };
                _context.Add(newMonthlyIncome);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync()
        {
            return await _context.ExpenseCategories.ToListAsync();
        }

        public Task AddExpenseCategoryAsync(ExpenseCategory category)
        {
            _context.Add(category);
            return _context.SaveChangesAsync();
        }

        public async Task<ExpenseCategory?> GetExpenseCategoryAsync(int id)
        {
            return await _context.ExpenseCategories.FindAsync(id);
        }

        public Task UpdateExpenseCategoryAsync(ExpenseCategory category)
        {
            _context.Update(category);
            return _context.SaveChangesAsync();
        }

        public async Task DeleteExpenseCategoryAsync(int id)
        {
            var category = await _context.ExpenseCategories.FindAsync(id);
            if (category != null)
            {
                _context.ExpenseCategories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<IncomeSource>> GetAllIncomeSourcesAsync()
        {
            return await _context.IncomeSources.ToListAsync();
        }

        public async Task<IncomeSource?> GetIncomeSourceAsync(int id)
        {
            return await _context.IncomeSources.FindAsync(id);
        }

        public async Task DeleteIncomeSourceAsync(int id)
        {
            var incomeSource = await _context.IncomeSources.FindAsync(id);
            if (incomeSource != null)
            {
                _context.IncomeSources.Remove(incomeSource);
                await _context.SaveChangesAsync();
            }
        }

        public Task AddRegularExpenseAsync(RegularExpense expense)
        {
            _context.Add(expense);
            return _context.SaveChangesAsync();
        }

        public Task UpdateRegularExpenseAsync(RegularExpense expense)
        {
            // This needs more complex logic to handle historical data correctly, as per the design doc.
            // For now, a simple update.
            _context.Update(expense);
            return _context.SaveChangesAsync();
        }

        public Task AddIrregularExpenseAsync(IrregularExpense expense)
        {
            _context.Add(expense);
            return _context.SaveChangesAsync();
        }

        public Task DeleteIrregularExpenseAsync(int expenseId)
        {
            var expense = new IrregularExpense { Id = expenseId };
            _context.Remove(expense);
            return _context.SaveChangesAsync();
        }
    }
}
