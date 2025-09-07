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
                ActualAmount = monthlyIncomes.TryGetValue(source.Id, out var income) ? income.ActualAmount : source.ExpectedAmount,
                Currency = source.Currency
            }).ToList();
            
            var totalIncome = incomeViewModels.Sum(i => i.ActualAmount);

            // Regular Expenses - now with month-based logic
            var regularExpenses = await _context.RegularExpenses
                .Include(e => e.Category)
                .Include(e => e.Schedules)
                .Where(e => e.Schedules.Any(s =>
                    (s.StartYear * 12 + s.StartMonth) <= (year * 12 + month) &&
                    (s.EndYear == null || s.EndMonth == null ||
                     (s.EndYear * 12 + s.EndMonth) >= (year * 12 + month))))
                .ToListAsync();

            var applicableRegularExpenses = new List<RegularExpense>();
            var expenseAmounts = new Dictionary<int, decimal>(); // Track amounts per expense for the month
            
            foreach (var expense in regularExpenses)
            {
                // Find the applicable schedule for this month using month-based logic
                var applicableSchedule = expense.Schedules
                    .Where(s => s.IsActiveForMonth(year, month))
                    .OrderByDescending(s => s.StartYear * 12 + s.StartMonth)
                    .FirstOrDefault();

                if (applicableSchedule != null)
                {
                    // This logic will need to be more robust to handle different frequencies
                    if (applicableSchedule.Frequency == Frequency.Monthly)
                    {
                        // Set the display amount for this month
                        expense.DisplayAmount = applicableSchedule.Amount;
                        applicableRegularExpenses.Add(expense);
                        expenseAmounts[expense.Id] = applicableSchedule.Amount;
                    }
                    // TODO: Add logic for Quarterly, SemiAnnually, Annually
                }
            }
            var totalRegularExpenses = expenseAmounts.Values.Sum();

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
                if (expense.Category != null && expenseAmounts.TryGetValue(expense.Id, out var amount))
                {
                    expensesByCategory.TryGetValue(expense.Category.Name, out var currentTotal);
                    expensesByCategory[expense.Category.Name] = currentTotal + amount;
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

        public async Task AddRegularExpenseAsync(RegularExpense expense)
        {
            // The expense should already have its initial schedule created by the caller
            // If it doesn't have any schedules, create a default one (fallback)
            if (!expense.Schedules.Any())
            {
                var initialSchedule = new ExpenseSchedule
                {
                    StartYear = DateTime.Today.Year,
                    StartMonth = DateTime.Today.Month,
                    Amount = 0, // Default amount
                    Frequency = Frequency.Monthly
                };
                expense.Schedules.Add(initialSchedule);
            }

            _context.Add(expense);
            await _context.SaveChangesAsync();
        }

        public async Task<RegularExpense?> GetRegularExpenseAsync(int id)
        {
            return await _context.RegularExpenses
                .Include(e => e.Category)
                .Include(e => e.Schedules)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task UpdateRegularExpenseAsync(RegularExpense expense)
        {
            // For temporal data integrity, we need to handle updates carefully
            // Instead of updating existing schedules, we create new ones for future dates

            var existingExpense = await _context.RegularExpenses
                .Include(e => e.Schedules)
                .FirstOrDefaultAsync(e => e.Id == expense.Id);

            if (existingExpense == null)
            {
                throw new ArgumentException("Expense not found");
            }

            // Update basic properties
            existingExpense.Name = expense.Name;
            existingExpense.ExpenseCategoryId = expense.ExpenseCategoryId;
            existingExpense.Currency = expense.Currency;

            // Handle schedule updates with temporal logic
            var currentSchedule = existingExpense.Schedules
                .Where(s => s.StartDate <= DateTime.Today && (s.EndDate == null || s.EndDate >= DateTime.Today))
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            if (currentSchedule != null)
            {
                // If the current schedule values are different, we need to create a new schedule
                if (currentSchedule.Amount != expense.Amount ||
                    currentSchedule.Frequency != expense.Recurrence)
                {
                    // End the current schedule as of yesterday (if startDate is in the future)
                    // or as of the day before the new start date (if startDate is in the past)
                    var endDateForCurrent = expense.StartDate > DateTime.Today
                        ? DateTime.Today.AddDays(-1)
                        : expense.StartDate.AddDays(-1);

                    if (endDateForCurrent >= currentSchedule.StartDate)
                    {
                        // Set end month based on the end date
                        var endDateTime = endDateForCurrent;
                        currentSchedule.EndYear = endDateTime.Year;
                        currentSchedule.EndMonth = endDateTime.Month;
                    }

                    // Create a new schedule starting on the specified date
                    var newSchedule = new ExpenseSchedule
                    {
                        StartYear = expense.StartDate.Year,
                        StartMonth = expense.StartDate.Month,
                        Amount = expense.Amount,
                        Frequency = expense.Recurrence
                    };

                    if (expense.EndDate.HasValue)
                    {
                        newSchedule.EndYear = expense.EndDate.Value.Year;
                        newSchedule.EndMonth = expense.EndDate.Value.Month;
                    }

                    existingExpense.Schedules.Add(newSchedule);
                }
                else
                {
                    // If only basic properties changed, just update the current schedule's end date if needed
                    if (expense.EndDate.HasValue)
                    {
                        currentSchedule.EndYear = expense.EndDate.Value.Year;
                        currentSchedule.EndMonth = expense.EndDate.Value.Month;
                    }
                    else
                    {
                        currentSchedule.EndYear = null;
                        currentSchedule.EndMonth = null;
                    }
                }
            }
            else
            {
                // No current schedule, create a new one
                var newSchedule = new ExpenseSchedule
                {
                    StartYear = expense.StartDate.Year,
                    StartMonth = expense.StartDate.Month,
                    Amount = expense.Amount,
                    Frequency = expense.Recurrence
                };

                if (expense.EndDate.HasValue)
                {
                    newSchedule.EndYear = expense.EndDate.Value.Year;
                    newSchedule.EndMonth = expense.EndDate.Value.Month;
                }

                existingExpense.Schedules.Add(newSchedule);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateRegularExpenseScheduleAsync(int expenseId, decimal amount, Frequency frequency, DateTime startDate)
        {
            var existingExpense = await _context.RegularExpenses
                .Include(e => e.Schedules)
                .FirstOrDefaultAsync(e => e.Id == expenseId);

            if (existingExpense == null)
            {
                throw new ArgumentException("Expense not found");
            }

            // Find the most recent schedule that would be active at the time of the change
            var currentSchedule = existingExpense.Schedules
                .Where(s => s.StartDate <= DateTime.Today && (s.EndDate == null || s.EndDate >= DateTime.Today))
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            // If we're setting a future start date, we need to handle it differently
            if (startDate > DateTime.Today)
            {
                // For future changes, end the current schedule and create a new one starting on the future date
                if (currentSchedule != null)
                {
                    // End current schedule as of the day before the new start date
                    var endDateTime = startDate.AddDays(-1);
                    currentSchedule.EndYear = endDateTime.Year;
                    currentSchedule.EndMonth = endDateTime.Month;
                }

                // Create new schedule for the future date
                var newSchedule = new ExpenseSchedule
                {
                    StartYear = startDate.Year,
                    StartMonth = startDate.Month,
                    Amount = amount,
                    Frequency = frequency
                };

                existingExpense.Schedules.Add(newSchedule);
            }
            else
            {
                // For current/past changes, modify the existing schedule or create a new one
                if (currentSchedule != null)
                {
                    // If the current schedule has different values, end it and create a new one
                    if (currentSchedule.Amount != amount ||
                        currentSchedule.Frequency != frequency)
                    {
                        // End current schedule as of the day before the new start date
                        var endDateTime = startDate.AddDays(-1);
                        currentSchedule.EndYear = endDateTime.Year;
                        currentSchedule.EndMonth = endDateTime.Month;

                        // Create new schedule
                        var newSchedule = new ExpenseSchedule
                        {
                            StartYear = startDate.Year,
                            StartMonth = startDate.Month,
                            Amount = amount,
                            Frequency = frequency
                        };

                        existingExpense.Schedules.Add(newSchedule);
                    }
                    // If values are the same, just update the existing schedule
                }
                else
                {
                    // No current schedule, create a new one
                    var newSchedule = new ExpenseSchedule
                    {
                        StartYear = startDate.Year,
                        StartMonth = startDate.Month,
                        Amount = amount,
                        Frequency = frequency
                    };

                    existingExpense.Schedules.Add(newSchedule);
                }
            }

            await _context.SaveChangesAsync();
        }

        public Task AddIrregularExpenseAsync(IrregularExpense expense)
        {
            _context.Add(expense);
            return _context.SaveChangesAsync();
        }

        public async Task<IrregularExpense?> GetIrregularExpenseAsync(int id)
        {
            return await _context.IrregularExpenses
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public Task UpdateIrregularExpenseAsync(IrregularExpense expense)
        {
            _context.Update(expense);
            return _context.SaveChangesAsync();
        }

        public Task DeleteIrregularExpenseAsync(int expenseId)
        {
            var expense = new IrregularExpense { Id = expenseId };
            _context.Remove(expense);
            return _context.SaveChangesAsync();
        }

        public async Task SeedDefaultCategoriesAsync()
        {
            var existingCategories = await _context.ExpenseCategories.AnyAsync();
            if (existingCategories)
            {
                return; // Already seeded
            }

            var defaultCategories = new[]
            {
                "Groceries",
                "Utilities",
                "Transportation",
                "Entertainment",
                "Healthcare",
                "Dining Out",
                "Shopping",
                "Insurance",
                "Rent/Mortgage",
                "Education"
            };

            foreach (var categoryName in defaultCategories)
            {
                _context.ExpenseCategories.Add(new ExpenseCategory { Name = categoryName });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<RegularExpense>> GetAllRegularExpensesAsync()
        {
            return await _context.RegularExpenses
                .Include(e => e.Category)
                .Include(e => e.Schedules)
                .ToListAsync();
        }
    }
}
