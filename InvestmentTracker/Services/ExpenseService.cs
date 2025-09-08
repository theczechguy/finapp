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

            // One-time Incomes
            var oneTimeIncomes = await GetOneTimeIncomesForMonthAsync(year, month);
            var totalOneTimeIncome = oneTimeIncomes.Sum(oti => oti.Amount);
            totalIncome += totalOneTimeIncome;

            // Regular Expenses - now with month-based logic
            var regularExpenses = await _context.RegularExpenses
                .Include(e => e.Category)
                .Include(e => e.FamilyMember)
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
                .Include(e => e.FamilyMember)
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

            // Budgets: compute effective budgets and map to view model
            var effectiveBudgets = await GetEffectiveBudgetsAsync(year, month);
            var budgetsByCategoryId = effectiveBudgets
                .GroupBy(b => b.ExpenseCategoryId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.StartYear * 12 + b.StartMonth).First());

            // Build budgets list per category present in categories list
            var allCategories = await _context.ExpenseCategories.ToListAsync();
            var budgetsVm = new List<BudgetItemViewModel>();
            foreach (var cat in allCategories)
            {
                expensesByCategory.TryGetValue(cat.Name, out var spent);
                var budget = budgetsByCategoryId.TryGetValue(cat.Id, out var b) ? b.Amount : (decimal?)null;
                
                // Treat zero-amount budgets as no budget
                if (budget.HasValue && budget.Value == 0)
                {
                    budget = null;
                }
                
                budgetsVm.Add(new BudgetItemViewModel
                {
                    CategoryId = cat.Id,
                    CategoryName = cat.Name,
                    BudgetAmount = budget,
                    SpentAmount = spent
                });
            }

            var viewModel = new MonthlyExpenseViewModel
            {
                Year = year,
                Month = month,
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                Incomes = incomeViewModels,
                OneTimeIncomes = oneTimeIncomes.ToList(),
                RegularExpenses = applicableRegularExpenses,
                IrregularExpenses = irregularExpenses,
                ExpensesByCategory = expensesByCategory,
                Budgets = budgetsVm
            };

            return viewModel;
        }

        public async Task<List<CategoryBudget>> GetEffectiveBudgetsAsync(int year, int month)
        {
            var monthIndex = year * 12 + month;
            return await _context.CategoryBudgets
                .Include(cb => cb.ExpenseCategory)
                .Where(cb => (cb.StartYear * 12 + cb.StartMonth) <= monthIndex &&
                             (cb.EndYear == null || cb.EndMonth == null || (cb.EndYear * 12 + cb.EndMonth) >= monthIndex))
                .ToListAsync();
        }

        public async Task SetCategoryBudgetAsync(int categoryId, decimal amount, int year, int month, bool applyToFuture)
        {
            var monthIndex = year * 12 + month;
            var budgets = await _context.CategoryBudgets
                .Where(cb => cb.ExpenseCategoryId == categoryId)
                .ToListAsync();

            if (applyToFuture)
            {
                // End any existing budget that extends beyond this month
                foreach (var existing in budgets)
                {
                    var existingStart = existing.StartYear * 12 + existing.StartMonth;
                    var existingEnd = existing.EndYear.HasValue && existing.EndMonth.HasValue
                        ? existing.EndYear.Value * 12 + existing.EndMonth.Value
                        : int.MaxValue;

                    if (existingStart <= monthIndex && existingEnd >= monthIndex)
                    {
                        // End it at previous month
                        var prev = new DateTime(year, month, 1).AddMonths(-1);
                        existing.EndYear = prev.Year;
                        existing.EndMonth = prev.Month;
                    }
                }

                // Add a new budget starting this month with no end
                var newBudget = new CategoryBudget
                {
                    ExpenseCategoryId = categoryId,
                    StartYear = year,
                    StartMonth = month,
                    Amount = amount
                };
                _context.CategoryBudgets.Add(newBudget);
            }
            else
            {
                // This month only: create a single-month budget or adjust existing range
                // If a range covers this month, split it into up to two ranges (before and after)
                var covering = budgets.FirstOrDefault(cb => cb.IsActiveForMonth(year, month));
                if (covering != null)
                {
                    // Adjust covering: end the first part at previous month
                    var prev = new DateTime(year, month, 1).AddMonths(-1);
                    if (prev >= covering.StartDate)
                    {
                        covering.EndYear = prev.Year;
                        covering.EndMonth = prev.Month;
                    }

                    // Add a second part beginning next month with original amount if the original had open end
                    var next = new DateTime(year, month, 1).AddMonths(1);
                    var originalHadOpenEnd = !covering.EndYear.HasValue || !covering.EndMonth.HasValue;
                    if (originalHadOpenEnd)
                    {
                        var tail = new CategoryBudget
                        {
                            ExpenseCategoryId = categoryId,
                            StartYear = next.Year,
                            StartMonth = next.Month,
                            Amount = covering.Amount
                        };
                        _context.CategoryBudgets.Add(tail);
                    }
                }

                // Add the single-month override
                var thisMonthBudget = new CategoryBudget
                {
                    ExpenseCategoryId = categoryId,
                    StartYear = year,
                    StartMonth = month,
                    EndYear = year,
                    EndMonth = month,
                    Amount = amount
                };
                _context.CategoryBudgets.Add(thisMonthBudget);
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteCategoryBudgetAsync(int categoryId, int year, int month)
        {
            var monthIndex = year * 12 + month;
            var budgets = await _context.CategoryBudgets
                .Where(cb => cb.ExpenseCategoryId == categoryId)
                .ToListAsync();

            // Find budgets that are active for this month
            var activeBudgets = budgets.Where(cb => cb.IsActiveForMonth(year, month)).ToList();

            foreach (var budget in activeBudgets)
            {
                var budgetStart = budget.StartYear * 12 + budget.StartMonth;
                var budgetEnd = budget.EndYear.HasValue && budget.EndMonth.HasValue
                    ? budget.EndYear.Value * 12 + budget.EndMonth.Value
                    : int.MaxValue;

                if (budgetStart == monthIndex && budgetEnd == monthIndex)
                {
                    // Single month budget - delete it entirely
                    _context.CategoryBudgets.Remove(budget);
                }
                else if (budgetStart <= monthIndex && budgetEnd >= monthIndex)
                {
                    if (budgetStart == monthIndex)
                    {
                        // Budget starts this month - move start to next month
                        var nextMonth = new DateTime(year, month, 1).AddMonths(1);
                        budget.StartYear = nextMonth.Year;
                        budget.StartMonth = nextMonth.Month;
                    }
                    else if (budgetEnd == monthIndex)
                    {
                        // Budget ends this month - move end to previous month
                        var prevMonth = new DateTime(year, month, 1).AddMonths(-1);
                        budget.EndYear = prevMonth.Year;
                        budget.EndMonth = prevMonth.Month;
                    }
                    else
                    {
                        // Budget spans across this month - split it
                        // End the first part at previous month
                        var prevMonth = new DateTime(year, month, 1).AddMonths(-1);
                        budget.EndYear = prevMonth.Year;
                        budget.EndMonth = prevMonth.Month;

                        // Create a new budget starting next month with the same amount
                        var nextMonth = new DateTime(year, month, 1).AddMonths(1);
                        var newBudget = new CategoryBudget
                        {
                            ExpenseCategoryId = categoryId,
                            StartYear = nextMonth.Year,
                            StartMonth = nextMonth.Month,
                            EndYear = budget.EndYear,
                            EndMonth = budget.EndMonth,
                            Amount = budget.Amount
                        };
                        _context.CategoryBudgets.Add(newBudget);
                    }
                }
            }

            await _context.SaveChangesAsync();
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

        public Task AddOneTimeIncomeAsync(OneTimeIncome income)
        {
            _context.Add(income);
            return _context.SaveChangesAsync();
        }

        public async Task<OneTimeIncome?> GetOneTimeIncomeAsync(int id)
        {
            return await _context.OneTimeIncomes
                .Include(oti => oti.IncomeSource)
                .FirstOrDefaultAsync(oti => oti.Id == id);
        }

        public Task UpdateOneTimeIncomeAsync(OneTimeIncome income)
        {
            _context.Update(income);
            return _context.SaveChangesAsync();
        }

        public Task DeleteOneTimeIncomeAsync(int incomeId)
        {
            var income = new OneTimeIncome { Id = incomeId };
            _context.Remove(income);
            return _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<OneTimeIncome>> GetOneTimeIncomesForMonthAsync(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            
            return await _context.OneTimeIncomes
                .Include(oti => oti.IncomeSource)
                .Where(oti => oti.Date >= startDate && oti.Date <= endDate)
                .ToListAsync();
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
                .Include(e => e.FamilyMember)
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
                .Include(e => e.FamilyMember)
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
                .Include(e => e.FamilyMember)
                .Include(e => e.Schedules)
                .ToListAsync();
        }

        public async Task<IEnumerable<FamilyMember>> GetFamilyMembersAsync()
        {
            return await _context.FamilyMember
                .Where(fm => fm.IsActive)
                .OrderBy(fm => fm.Name)
                .ToListAsync();
        }

        public async Task AddFamilyMemberAsync(FamilyMember familyMember)
        {
            _context.Add(familyMember);
            await _context.SaveChangesAsync();
        }

        public async Task<FamilyMember?> GetFamilyMemberAsync(int id)
        {
            return await _context.FamilyMember.FindAsync(id);
        }

        public async Task UpdateFamilyMemberAsync(FamilyMember familyMember)
        {
            _context.Update(familyMember);
            await _context.SaveChangesAsync();
        }
    }
}
