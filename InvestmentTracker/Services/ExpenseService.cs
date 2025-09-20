using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public async Task<FinancialScheduleConfig?> GetFinancialScheduleConfigAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving financial schedule configuration");
                var config = await _context.FinancialScheduleConfigs.AsNoTracking().FirstOrDefaultAsync();
                _logger.LogDebug("Retrieved financial schedule config: {@Config}", config);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve financial schedule configuration");
                throw;
            }
        }

        public async Task<MonthlyExpenseViewModel> GetMonthlyDataAsync(int year, int month)
        {
            using var scope = _logger.BeginScope("Getting monthly expense data for {Year}/{Month}", year, month);
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogDebug("Starting monthly data retrieval for {Year}/{Month}", year, month);
                
                // Load financial schedule config
                var configStopwatch = Stopwatch.StartNew();
                var config = await _context.FinancialScheduleConfigs.AsNoTracking().FirstOrDefaultAsync();
                _logger.LogDebug("Financial schedule config loaded in {ElapsedMs}ms", configStopwatch.ElapsedMilliseconds);
                
                string scheduleType = config?.ScheduleType ?? "Calendar";
                int startDay = config?.StartDay ?? 1;
                _logger.LogDebug("Using schedule type: {ScheduleType}, start day: {StartDay}", scheduleType, startDay);

                DateTime startDate, endDate;
                if (scheduleType == "Custom")
                {
                    var currentTargetMonth = new DateTime(year, month, 1);
                    var nextTargetMonth = currentTargetMonth.AddMonths(1);

                    var overrides = await _context.FinancialMonthOverrides
                        .AsNoTracking()
                        .Where(o => o.TargetMonth == currentTargetMonth || o.TargetMonth == nextTargetMonth)
                        .ToDictionaryAsync(o => o.TargetMonth);

                    var currentMonthOverride = overrides.GetValueOrDefault(currentTargetMonth);
                    var nextMonthOverride = overrides.GetValueOrDefault(nextTargetMonth);

                    // Determine the start date for the current financial month
                    startDate = currentMonthOverride?.OverrideStartDate ?? new DateTime(year, month, startDay);

                    // Determine the start date for the next financial month to calculate the current month's end date
                    DateTime nextMonthStartDate;
                    if (nextMonthOverride != null)
                    {
                        nextMonthStartDate = nextMonthOverride.OverrideStartDate;
                    }
                    else
                    {
                        var nextMonthDate = new DateTime(year, month, startDay).AddMonths(1);
                        nextMonthStartDate = new DateTime(nextMonthDate.Year, nextMonthDate.Month, startDay);
                    }
                    
                    endDate = nextMonthStartDate.AddDays(-1);
                }
                else
                {
                    // Calendar month
                    startDate = new DateTime(year, month, 1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                }
                
                _logger.LogDebug("Date range calculated: {StartDate} to {EndDate}", startDate, endDate);

                // Execute queries sequentially to avoid DbContext concurrency issues
                var queryStopwatch = Stopwatch.StartNew();
                var incomeViewModels = await GetMonthlyIncomeDataAsync(year, month);
                var oneTimeIncomes = await GetOneTimeIncomesForMonthAsync(year, month);
                var (applicableRegularExpenses, expenseAmounts) = await GetApplicableRegularExpensesAsync(year, month);
                var irregularExpenses = await GetIrregularExpensesForMonthAsync(startDate, endDate);
                var effectiveBudgets = await GetEffectiveBudgetsAsync(year, month);
                var allCategories = await _context.ExpenseCategories.AsNoTracking().ToListAsync();
                _logger.LogDebug("Database queries completed in {ElapsedMs}ms", queryStopwatch.ElapsedMilliseconds);

                // Calculate totals
                var calculationStopwatch = Stopwatch.StartNew();
                var totalIncome = incomeViewModels.Sum(i => i.ActualAmount) + oneTimeIncomes.Sum(oti => oti.Amount);
                var totalRegularExpenses = expenseAmounts.Values.Sum();
                var totalIrregularExpenses = irregularExpenses.Sum(e => e.Amount);
                var totalExpenses = totalRegularExpenses + totalIrregularExpenses;
                
                _logger.LogDebug("Calculated totals - Income: {TotalIncome}, Regular Expenses: {TotalRegular}, Irregular Expenses: {TotalIrregular}, Total Expenses: {TotalExpenses}", 
                    totalIncome, totalRegularExpenses, totalIrregularExpenses, totalExpenses);

                // Build category breakdown efficiently
                var expensesByCategory = BuildCategoryBreakdown(applicableRegularExpenses, expenseAmounts, irregularExpenses);
                _logger.LogDebug("Built category breakdown for {CategoryCount} categories", expensesByCategory.Count);

                // Build budgets view model
                var budgetsVm = BuildBudgetsViewModel(effectiveBudgets, expensesByCategory, allCategories);
                _logger.LogDebug("Built budgets view model with {BudgetCount} budget items", budgetsVm.Count);
                
                calculationStopwatch.Stop();
                _logger.LogDebug("Calculations completed in {ElapsedMs}ms", calculationStopwatch.ElapsedMilliseconds);

                var viewModel = new MonthlyExpenseViewModel
                {
                    SelectedDate = new DateTime(year, month, 1),
                    TotalIncome = totalIncome,
                    TotalExpenses = totalExpenses,
                    Incomes = incomeViewModels.ToList(),
                    OneTimeIncomes = oneTimeIncomes.ToList(),
                    RegularExpenses = applicableRegularExpenses,
                    IrregularExpenses = irregularExpenses.ToList(),
                    ExpensesByCategory = expensesByCategory,
                    Budgets = budgetsVm,
                    ScheduleConfig = config,
                    FinancialMonthStartDate = startDate,
                    FinancialMonthEndDate = endDate
                };

                stopwatch.Stop();
                _logger.LogInformation("Monthly data retrieved successfully for {Year}/{Month} in {ElapsedMs}ms. Income: {TotalIncome}, Expenses: {TotalExpenses}", 
                    year, month, stopwatch.ElapsedMilliseconds, totalIncome, totalExpenses);
                    
                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve monthly data for {Year}/{Month} after {ElapsedMs}ms", 
                    year, month, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task SetFinancialMonthOverrideAsync(DateTime targetMonth, DateTime overrideStartDate)
        {
            try
            {
                _logger.LogInformation("Setting financial month override for {TargetMonth} to {OverrideStartDate}", 
                    targetMonth.ToString("yyyy-MM-dd"), overrideStartDate.ToString("yyyy-MM-dd"));
                
                var existingOverride = await _context.FinancialMonthOverrides
                    .FirstOrDefaultAsync(fmo => fmo.TargetMonth == targetMonth);

                if (existingOverride != null)
                {
                    _logger.LogDebug("Updating existing override for {TargetMonth}", targetMonth.ToString("yyyy-MM-dd"));
                    existingOverride.OverrideStartDate = overrideStartDate;
                    _context.FinancialMonthOverrides.Update(existingOverride);
                }
                else
                {
                    _logger.LogDebug("Creating new override for {TargetMonth}", targetMonth.ToString("yyyy-MM-dd"));
                    var newOverride = new FinancialMonthOverride
                    {
                        TargetMonth = targetMonth,
                        OverrideStartDate = overrideStartDate
                    };
                    _context.FinancialMonthOverrides.Add(newOverride);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully set financial month override for {TargetMonth}", targetMonth.ToString("yyyy-MM-dd"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set financial month override for {TargetMonth}", targetMonth.ToString("yyyy-MM-dd"));
                throw;
            }
        }

        public async Task<DateTime?> GetExistingFinancialMonthOverrideAsync(DateTime targetMonth)
        {
            try
            {
                _logger.LogDebug("Checking for existing financial month override for {TargetMonth}", targetMonth.ToString("yyyy-MM-dd"));
                var overrideDate = await _context.FinancialMonthOverrides
                    .Where(o => o.TargetMonth == targetMonth)
                    .Select(o => (DateTime?)o.OverrideStartDate)
                    .FirstOrDefaultAsync();
                
                if (overrideDate.HasValue)
                {
                    _logger.LogDebug("Found override for {TargetMonth}: {OverrideDate}", 
                        targetMonth.ToString("yyyy-MM-dd"), overrideDate.Value.ToString("yyyy-MM-dd"));
                }
                else
                {
                    _logger.LogDebug("No override found for {TargetMonth}", targetMonth.ToString("yyyy-MM-dd"));
                }
                
                return overrideDate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check for financial month override for {TargetMonth}", targetMonth.ToString("yyyy-MM-dd"));
                throw;
            }
        }

        public async Task<DateTime> GetDefaultSelectedDateAsync()
        {
            try
            {
                _logger.LogDebug("Calculating default selected date");
                
                // Load financial schedule config
                var config = await GetFinancialScheduleConfigAsync();
                string scheduleType = config?.ScheduleType ?? "Calendar";
                int startDay = config?.StartDay ?? 1;
                
                var today = DateTime.Today;
                DateTime result;
                
                if (scheduleType == "Custom")
                {
                    _logger.LogDebug("Using custom schedule with start day {StartDay}", startDay);
                    // For custom schedules, find the start date of the period containing today
                    int currentYear = today.Year;
                    int currentMonth = today.Month; // 1-based
                    int currentDay = today.Day;
                    
                    if (currentDay >= startDay)
                    {
                        // Current date is in the period starting this month
                        result = new DateTime(currentYear, currentMonth, startDay);
                        _logger.LogDebug("Current date is in period starting this month: {Result}", result.ToString("yyyy-MM-dd"));
                    }
                    else
                    {
                        // Current date is in the period starting last month
                        if (currentMonth == 1)
                        {
                            // January, so go to December of previous year
                            result = new DateTime(currentYear - 1, 12, startDay);
                            _logger.LogDebug("Current date is in period starting last month (December previous year): {Result}", result.ToString("yyyy-MM-dd"));
                        }
                        else
                        {
                            result = new DateTime(currentYear, currentMonth - 1, startDay);
                            _logger.LogDebug("Current date is in period starting last month: {Result}", result.ToString("yyyy-MM-dd"));
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("Using calendar schedule");
                    // For calendar months, use the first day of current month
                    result = new DateTime(today.Year, today.Month, 1);
                    _logger.LogDebug("Using first day of current month: {Result}", result.ToString("yyyy-MM-dd"));
                }
                
                _logger.LogDebug("Default selected date calculated: {Result}", result.ToString("yyyy-MM-dd"));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate default selected date");
                throw;
            }
        }

        private async Task<DateTime> CalculateDefaultStartDateForMonthAsync(int year, int month)
        {
            // Load financial schedule config
            var config = await GetFinancialScheduleConfigAsync();
            string scheduleType = config?.ScheduleType ?? "Calendar";
            int startDay = config?.StartDay ?? 1;
            
            if (scheduleType == "Custom")
            {
                return new DateTime(year, month, startDay);
            }
            else
            {
                return new DateTime(year, month, 1);
            }
        }

        private async Task<List<IncomeViewModel>> GetMonthlyIncomeDataAsync(int year, int month)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogDebug("Loading monthly income data for {Year}/{Month}", year, month);
                
                var allIncomeSources = await _context.IncomeSources.AsNoTracking().ToListAsync();
                var monthlyIncomes = await _context.MonthlyIncomes
                    .AsNoTracking()
                    .Where(i => i.Month.Year == year && i.Month.Month == month)
                    .ToDictionaryAsync(i => i.IncomeSourceId);

                var result = allIncomeSources.Select(source => new IncomeViewModel
                {
                    IncomeSourceId = source.Id,
                    Name = source.Name,
                    ExpectedAmount = source.ExpectedAmount,
                    ActualAmount = monthlyIncomes.TryGetValue(source.Id, out var income) ? income.ActualAmount : source.ExpectedAmount,
                    Currency = source.Currency
                }).ToList();
                
                _logger.LogDebug("Loaded {IncomeSourceCount} income sources and {MonthlyIncomeCount} monthly income records in {ElapsedMs}ms", 
                    allIncomeSources.Count, monthlyIncomes.Count, stopwatch.ElapsedMilliseconds);
                    
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load monthly income data for {Year}/{Month} after {ElapsedMs}ms", 
                    year, month, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private async Task<(List<RegularExpense> expenses, Dictionary<int, decimal> amounts)> GetApplicableRegularExpensesAsync(int year, int month)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogDebug("Loading applicable regular expenses for {Year}/{Month}", year, month);
                
                // Optimized query with proper frequency handling
                var monthIndex = year * 12 + month;
                var queryStopwatch = Stopwatch.StartNew();
                var regularExpenses = await _context.RegularExpenses
                    .AsNoTracking()
                    .Include(e => e.Category)
                    .Include(e => e.FamilyMember)
                    .Include(e => e.Schedules.Where(s => 
                        (s.StartYear * 12 + s.StartMonth) <= monthIndex &&
                        (s.EndYear == null || s.EndMonth == null || (s.EndYear * 12 + s.EndMonth) >= monthIndex)))
                    .Where(e => e.Schedules.Any(s =>
                        (s.StartYear * 12 + s.StartMonth) <= monthIndex &&
                        (s.EndYear == null || s.EndMonth == null || (s.EndYear * 12 + s.EndMonth) >= monthIndex)))
                    .ToListAsync();
                
                _logger.LogDebug("Loaded {ExpenseCount} regular expenses from database in {ElapsedMs}ms", 
                    regularExpenses.Count, queryStopwatch.ElapsedMilliseconds);

                var applicableRegularExpenses = new List<RegularExpense>();
                var expenseAmounts = new Dictionary<int, decimal>();
                var processingStopwatch = Stopwatch.StartNew();
                
                foreach (var expense in regularExpenses)
                {
                    // Find the most recent applicable schedule for this month
                    var applicableSchedule = expense.Schedules
                        .Where(s => s.IsActiveForMonth(year, month))
                        .OrderByDescending(s => s.StartYear * 12 + s.StartMonth)
                        .FirstOrDefault();

                    if (applicableSchedule != null && applicableSchedule.ShouldApplyInMonth(year, month))
                    {
                        // Set the display amount for this month
                        expense.DisplayAmount = applicableSchedule.Amount;
                        expense.DisplayFrequency = applicableSchedule.Frequency;
                        applicableRegularExpenses.Add(expense);
                        expenseAmounts[expense.Id] = applicableSchedule.Amount;
                    }
                }
                
                _logger.LogDebug("Processed {ApplicableCount} applicable expenses out of {TotalCount} in {ElapsedMs}ms", 
                    applicableRegularExpenses.Count, regularExpenses.Count, processingStopwatch.ElapsedMilliseconds);

                _logger.LogDebug("Regular expenses processing completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return (applicableRegularExpenses, expenseAmounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load applicable regular expenses for {Year}/{Month} after {ElapsedMs}ms", 
                    year, month, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private async Task<List<IrregularExpense>> GetIrregularExpensesForMonthAsync(DateTime startDate, DateTime endDate)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogDebug("Loading irregular expenses for date range {StartDate} to {EndDate}", startDate, endDate);
                
                var result = await _context.IrregularExpenses
                    .AsNoTracking()
                    .Include(e => e.Category)
                    .Include(e => e.FamilyMember)
                    .Where(e => e.Date >= startDate && e.Date <= endDate)
                    .ToListAsync();
                    
                _logger.LogDebug("Loaded {ExpenseCount} irregular expenses in {ElapsedMs}ms", 
                    result.Count, stopwatch.ElapsedMilliseconds);
                    
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load irregular expenses for date range {StartDate} to {EndDate} after {ElapsedMs}ms", 
                    startDate, endDate, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private static Dictionary<string, decimal> BuildCategoryBreakdown(
            List<RegularExpense> applicableRegularExpenses, 
            Dictionary<int, decimal> expenseAmounts, 
            List<IrregularExpense> irregularExpenses)
        {
            var expensesByCategory = new Dictionary<string, decimal>();
            
            foreach (var expense in applicableRegularExpenses.Where(e => e.Category != null))
            {
                if (expenseAmounts.TryGetValue(expense.Id, out var amount))
                {
                    expensesByCategory.TryGetValue(expense.Category!.Name, out var currentTotal);
                    expensesByCategory[expense.Category.Name] = currentTotal + amount;
                }
            }
            
            foreach (var expense in irregularExpenses.Where(e => e.Category != null))
            {
                expensesByCategory.TryGetValue(expense.Category!.Name, out var currentTotal);
                expensesByCategory[expense.Category.Name] = currentTotal + expense.Amount;
            }

            return expensesByCategory;
        }

        private static List<BudgetItemViewModel> BuildBudgetsViewModel(
            List<CategoryBudget> effectiveBudgets, 
            Dictionary<string, decimal> expensesByCategory, 
            List<ExpenseCategory> allCategories)
        {
            var budgetsByCategoryId = effectiveBudgets
                .GroupBy(b => b.ExpenseCategoryId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.StartYear * 12 + b.StartMonth).First());

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

            return budgetsVm;
        }

        public async Task DeleteCategoryBudgetAsync(int categoryId, int year, int month)
        {
            try
            {
                _logger.LogInformation("Deleting category budget for category {CategoryId} starting from {Year}/{Month}", 
                    categoryId, year, month);
                    
                var monthIndex = year * 12 + month;
                var budgets = await _context.CategoryBudgets
                    .Where(cb => cb.ExpenseCategoryId == categoryId)
                    .ToListAsync();
                    
                _logger.LogDebug("Found {BudgetCount} budget entries for category {CategoryId}", 
                    budgets.Count, categoryId);

                // Find budgets that are active for this month or start in future months
                var relevantBudgets = budgets.Where(cb =>
                    (cb.StartYear * 12 + cb.StartMonth) >= monthIndex ||
                    cb.IsActiveForMonth(year, month)).ToList();
                    
                _logger.LogDebug("Found {RelevantCount} relevant budget entries to process", relevantBudgets.Count);

                foreach (var budget in relevantBudgets)
                {
                    var budgetStart = budget.StartYear * 12 + budget.StartMonth;
                    var budgetEnd = budget.EndYear.HasValue && budget.EndMonth.HasValue
                        ? budget.EndYear.Value * 12 + budget.EndMonth.Value
                        : int.MaxValue;

                    if (budgetStart >= monthIndex)
                    {
                        // Budget starts at or after this month - delete it entirely
                        _context.CategoryBudgets.Remove(budget);
                        _logger.LogDebug("Removed budget {BudgetId} that starts at/after {Year}/{Month}", 
                            budget.Id, year, month);
                    }
                    else if (budgetStart < monthIndex && budgetEnd >= monthIndex)
                    {
                        // Budget started before this month but extends to/through this month
                        // End it at the previous month
                        var prevMonth = new DateTime(year, month, 1).AddMonths(-1);
                        budget.EndYear = prevMonth.Year;
                        budget.EndMonth = prevMonth.Month;
                        _logger.LogDebug("Ended budget {BudgetId} at {EndYear}/{EndMonth}", 
                            budget.Id, budget.EndYear, budget.EndMonth);
                    }
                    // If budget ended before this month, we don't need to modify it
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted/modified budgets for category {CategoryId} from {Year}/{Month}", 
                    categoryId, year, month);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete category budget for category {CategoryId} from {Year}/{Month}", 
                    categoryId, year, month);
                throw;
            }
        }

        public async Task<List<BudgetHistoryItem>> GetBudgetHistoryAsync(int categoryId)
        {
            try
            {
                _logger.LogDebug("Retrieving budget history for category {CategoryId}", categoryId);
                var budgets = await _context.CategoryBudgets
                    .Where(cb => cb.ExpenseCategoryId == categoryId)
                    .Include(cb => cb.ExpenseCategory)
                    .OrderByDescending(cb => cb.StartYear)
                    .ThenByDescending(cb => cb.StartMonth)
                    .ToListAsync();

                var history = new List<BudgetHistoryItem>();
                foreach (var budget in budgets)
                {
                    history.Add(new BudgetHistoryItem
                    {
                        BudgetId = budget.Id,
                        CategoryId = budget.ExpenseCategoryId,
                        CategoryName = budget.ExpenseCategory?.Name ?? "Unknown",
                        Amount = budget.Amount,
                        StartYear = budget.StartYear,
                        StartMonth = budget.StartMonth,
                        EndYear = budget.EndYear,
                        EndMonth = budget.EndMonth,
                        StartDate = budget.StartDate,
                        EndDate = budget.EndDate,
                        IsActive = budget.IsActiveForMonth(DateTime.Today.Year, DateTime.Today.Month)
                    });
                }

                _logger.LogDebug("Retrieved {HistoryCount} budget history items for category {CategoryId}", history.Count, categoryId);
                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve budget history for category {CategoryId}", categoryId);
                throw;
            }
        }

        public async Task AddIncomeSourceAsync(IncomeSource incomeSource)
        {
            try
            {
                _logger.LogInformation("Adding new income source: {IncomeSourceName}", incomeSource.Name);
                _context.Add(incomeSource);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully added income source {IncomeSourceName} with ID {IncomeSourceId}", 
                    incomeSource.Name, incomeSource.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add income source {IncomeSourceName}", incomeSource.Name);
                throw;
            }
        }

        public async Task UpdateIncomeSourceAsync(IncomeSource incomeSource)
        {
            try
            {
                _logger.LogInformation("Updating income source {IncomeSourceId}: {IncomeSourceName}", 
                    incomeSource.Id, incomeSource.Name);
                _context.Update(incomeSource);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated income source {IncomeSourceId}: {IncomeSourceName}", 
                    incomeSource.Id, incomeSource.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update income source {IncomeSourceId}: {IncomeSourceName}", 
                    incomeSource.Id, incomeSource.Name);
                throw;
            }
        }

        public async Task LogOrUpdateMonthlyIncomeAsync(int incomeSourceId, int year, int month, decimal actualAmount)
        {
            try
            {
                _logger.LogInformation("Logging/updating monthly income for source {IncomeSourceId}, {Year}/{Month}: {Amount}", 
                    incomeSourceId, year, month, actualAmount);
                    
                var monthDate = new DateTime(year, month, 1);
                var existing = _context.MonthlyIncomes.FirstOrDefault(m => m.IncomeSourceId == incomeSourceId && m.Month == monthDate);

                if (existing != null)
                {
                    _logger.LogDebug("Updating existing monthly income record");
                    existing.ActualAmount = actualAmount;
                }
                else
                {
                    _logger.LogDebug("Creating new monthly income record");
                    var newMonthlyIncome = new MonthlyIncome
                    {
                        IncomeSourceId = incomeSourceId,
                        Month = monthDate,
                        ActualAmount = actualAmount
                    };
                    _context.Add(newMonthlyIncome);
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully logged/updated monthly income for source {IncomeSourceId}, {Year}/{Month}", 
                    incomeSourceId, year, month);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log/update monthly income for source {IncomeSourceId}, {Year}/{Month}", 
                    incomeSourceId, year, month);
                throw;
            }
        }

        public async Task AddOneTimeIncomeAsync(OneTimeIncome income)
        {
            try
            {
                _logger.LogInformation("Adding one-time income: {IncomeName}, Amount: {Amount}, Date: {Date}", 
                    income.Name, income.Amount, income.Date.ToString("yyyy-MM-dd"));
                _context.Add(income);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully added one-time income {IncomeName} with ID {IncomeId}", 
                    income.Name, income.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add one-time income {IncomeName}", income.Name);
                throw;
            }
        }

        public async Task<OneTimeIncome?> GetOneTimeIncomeAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving one-time income {IncomeId}", id);
                var income = await _context.OneTimeIncomes
                    .Include(oti => oti.IncomeSource)
                    .FirstOrDefaultAsync(oti => oti.Id == id);
                
                if (income == null)
                {
                    _logger.LogWarning("One-time income {IncomeId} not found", id);
                }
                else
                {
                    _logger.LogDebug("Retrieved one-time income {IncomeId}: {IncomeName}", id, income.Name);
                }
                
                return income;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve one-time income {IncomeId}", id);
                throw;
            }
        }

        public async Task UpdateOneTimeIncomeAsync(OneTimeIncome income)
        {
            try
            {
                _logger.LogInformation("Updating one-time income {IncomeId}: {IncomeName}", income.Id, income.Name);
                _context.Update(income);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated one-time income {IncomeId}: {IncomeName}", income.Id, income.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update one-time income {IncomeId}: {IncomeName}", income.Id, income.Name);
                throw;
            }
        }

        public async Task DeleteOneTimeIncomeAsync(int incomeId)
        {
            try
            {
                _logger.LogInformation("Deleting one-time income {IncomeId}", incomeId);
                var income = new OneTimeIncome { Id = incomeId };
                _context.Remove(income);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted one-time income {IncomeId}", incomeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete one-time income {IncomeId}", incomeId);
                throw;
            }
        }

        public async Task<IEnumerable<OneTimeIncome>> GetOneTimeIncomesForMonthAsync(int year, int month)
        {
            try
            {
                _logger.LogDebug("Retrieving one-time incomes for {Year}/{Month}", year, month);
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                
                var incomes = await _context.OneTimeIncomes
                    .AsNoTracking()
                    .Include(oti => oti.IncomeSource)
                    .Where(oti => oti.Date >= startDate && oti.Date <= endDate)
                    .ToListAsync();
                    
                _logger.LogDebug("Retrieved {IncomeCount} one-time incomes for {Year}/{Month}", incomes.Count, year, month);
                return incomes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve one-time incomes for {Year}/{Month}", year, month);
                throw;
            }
        }

        public async Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving all expense categories");
                var categories = await _context.ExpenseCategories.ToListAsync();
                _logger.LogDebug("Retrieved {CategoryCount} expense categories", categories.Count);
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve expense categories");
                throw;
            }
        }

        public async Task AddExpenseCategoryAsync(ExpenseCategory category)
        {
            try
            {
                _logger.LogInformation("Adding new expense category: {CategoryName}", category.Name);
                _context.Add(category);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully added expense category {CategoryName} with ID {CategoryId}", 
                    category.Name, category.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add expense category {CategoryName}", category.Name);
                throw;
            }
        }

        public async Task<ExpenseCategory?> GetExpenseCategoryAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving expense category {CategoryId}", id);
                var category = await _context.ExpenseCategories.FindAsync(id);
                
                if (category == null)
                {
                    _logger.LogWarning("Expense category {CategoryId} not found", id);
                }
                else
                {
                    _logger.LogDebug("Retrieved expense category {CategoryId}: {CategoryName}", id, category.Name);
                }
                
                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve expense category {CategoryId}", id);
                throw;
            }
        }

        public async Task UpdateExpenseCategoryAsync(ExpenseCategory category)
        {
            try
            {
                _logger.LogInformation("Updating expense category {CategoryId}: {CategoryName}", category.Id, category.Name);
                _context.Update(category);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated expense category {CategoryId}: {CategoryName}", category.Id, category.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update expense category {CategoryId}: {CategoryName}", category.Id, category.Name);
                throw;
            }
        }

        public async Task DeleteExpenseCategoryAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting expense category {CategoryId}", id);
                var category = await _context.ExpenseCategories.FindAsync(id);
                if (category != null)
                {
                    _context.ExpenseCategories.Remove(category);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully deleted expense category {CategoryId}: {CategoryName}", id, category.Name);
                }
                else
                {
                    _logger.LogWarning("Expense category {CategoryId} not found for deletion", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete expense category {CategoryId}", id);
                throw;
            }
        }

        public async Task SeedDefaultCategoriesAsync()
        {
            try
            {
                _logger.LogDebug("Checking if default expense categories need to be seeded");
                
                var existingCategories = await _context.ExpenseCategories.AnyAsync();
                if (existingCategories)
                {
                    _logger.LogDebug("Expense categories already exist, skipping seeding");
                    return; // Already seeded
                }

                _logger.LogInformation("Seeding default expense categories");
                
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
                _logger.LogInformation("Successfully seeded {CategoryCount} default expense categories", defaultCategories.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed default expense categories");
                throw;
            }
        }

        public async Task<IEnumerable<IncomeSource>> GetAllIncomeSourcesAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving all income sources");
                var incomeSources = await _context.IncomeSources.ToListAsync();
                _logger.LogDebug("Retrieved {IncomeSourceCount} income sources", incomeSources.Count);
                return incomeSources;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve income sources");
                throw;
            }
        }

        public async Task<IncomeSource?> GetIncomeSourceAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving income source {IncomeSourceId}", id);
                var incomeSource = await _context.IncomeSources.FindAsync(id);
                
                if (incomeSource == null)
                {
                    _logger.LogWarning("Income source {IncomeSourceId} not found", id);
                }
                else
                {
                    _logger.LogDebug("Retrieved income source {IncomeSourceId}: {IncomeSourceName}", id, incomeSource.Name);
                }
                
                return incomeSource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve income source {IncomeSourceId}", id);
                throw;
            }
        }

        public async Task DeleteIncomeSourceAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting income source {IncomeSourceId}", id);
                var incomeSource = await _context.IncomeSources.FindAsync(id);
                if (incomeSource != null)
                {
                    _context.IncomeSources.Remove(incomeSource);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully deleted income source {IncomeSourceId}: {IncomeSourceName}", id, incomeSource.Name);
                }
                else
                {
                    _logger.LogWarning("Income source {IncomeSourceId} not found for deletion", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete income source {IncomeSourceId}", id);
                throw;
            }
        }

        public async Task AddRegularExpenseAsync(RegularExpense expense)
        {
            try
            {
                _logger.LogInformation("Adding new regular expense: {ExpenseName}, Category: {CategoryId}, Amount: {Amount}", 
                    expense.Name, expense.ExpenseCategoryId, expense.Amount);
                    
                // The expense should already have its initial schedule created by the caller
                // If it doesn't have any schedules, create a default one (fallback)
                if (!expense.Schedules.Any())
                {
                    _logger.LogWarning("Regular expense {ExpenseName} has no schedules, creating default schedule", expense.Name);
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
                
                _logger.LogInformation("Successfully added regular expense {ExpenseName} with ID {ExpenseId}", 
                    expense.Name, expense.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add regular expense {ExpenseName}", expense.Name);
                throw;
            }
        }

        public async Task<RegularExpense?> GetRegularExpenseAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving regular expense {ExpenseId}", id);
                var expense = await _context.RegularExpenses
                    .Include(e => e.Category)
                    .Include(e => e.Schedules)
                    .Include(e => e.FamilyMember)
                    .FirstOrDefaultAsync(e => e.Id == id);
                
                if (expense == null)
                {
                    _logger.LogWarning("Regular expense {ExpenseId} not found", id);
                }
                else
                {
                    _logger.LogDebug("Retrieved regular expense {ExpenseId}: {ExpenseName}", id, expense.Name);
                }
                
                return expense;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve regular expense {ExpenseId}", id);
                throw;
            }
        }

        public async Task UpdateRegularExpenseAsync(RegularExpense expense)
        {
            try
            {
                _logger.LogInformation("Updating regular expense {ExpenseId}: {ExpenseName}", expense.Id, expense.Name);
                
                // For temporal data integrity, we need to handle updates carefully
                // Instead of updating existing schedules, we create new ones for future dates

                var existingExpense = await _context.RegularExpenses
                    .Include(e => e.Schedules)
                    .FirstOrDefaultAsync(e => e.Id == expense.Id);

                if (existingExpense == null)
                {
                    _logger.LogError("Regular expense with ID {ExpenseId} not found for update", expense.Id);
                    throw new ArgumentException("Expense not found");
                }

                _logger.LogDebug("Found existing expense {ExpenseName} with {ScheduleCount} schedules", 
                    existingExpense.Name, existingExpense.Schedules.Count);

                // Update basic properties
                existingExpense.Name = expense.Name;
                existingExpense.ExpenseCategoryId = expense.ExpenseCategoryId;
                existingExpense.Currency = expense.Currency;
                existingExpense.ExpenseType = expense.ExpenseType;
                existingExpense.FamilyMemberId = expense.FamilyMemberId;

                // Handle schedule updates with temporal logic
                var currentSchedule = existingExpense.Schedules
                    .Where(s => s.StartDate <= DateTime.Today && (s.EndDate == null || s.EndDate >= DateTime.Today))
                    .OrderByDescending(s => s.StartYear)
                    .ThenByDescending(s => s.StartMonth)
                    .ThenByDescending(s => s.StartDay)
                    .FirstOrDefault();

                if (currentSchedule != null)
                {
                    _logger.LogDebug("Current schedule found: Amount {Amount}, Frequency {Frequency}", 
                        currentSchedule.Amount, currentSchedule.Frequency);
                        
                    // If the current schedule values are different, we need to create a new schedule
                    if (currentSchedule.Amount != expense.Amount ||
                        currentSchedule.Frequency != expense.Recurrence)
                    {
                        _logger.LogDebug("Schedule values changed, creating new schedule entry");
                        
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
                        _logger.LogDebug("Added new schedule: Amount {Amount}, Frequency {Frequency}, Start {StartYear}/{StartMonth}", 
                            newSchedule.Amount, newSchedule.Frequency, newSchedule.StartYear, newSchedule.StartMonth);
                    }
                    else
                    {
                        _logger.LogDebug("Only updating end date for existing schedule");
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
                    _logger.LogDebug("No current schedule found, creating new schedule");
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
                _logger.LogInformation("Successfully updated regular expense {ExpenseId}: {ExpenseName}", expense.Id, expense.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update regular expense {ExpenseId}: {ExpenseName}", expense.Id, expense.Name);
                throw;
            }
        }

        public async Task AddIrregularExpenseAsync(IrregularExpense expense)
        {
            try
            {
                _logger.LogInformation("Adding irregular expense: {ExpenseName}, Amount: {Amount}, Date: {Date}", 
                    expense.Name, expense.Amount, expense.Date);
                    
                _context.Add(expense);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully added irregular expense {ExpenseName} with ID {ExpenseId}", 
                    expense.Name, expense.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add irregular expense {ExpenseName}", expense.Name);
                throw;
            }
        }

        public async Task<IrregularExpense?> GetIrregularExpenseAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving irregular expense {ExpenseId}", id);
                
                var expense = await _context.IrregularExpenses
                    .Include(e => e.Category)
                    .Include(e => e.FamilyMember)
                    .FirstOrDefaultAsync(e => e.Id == id);
                    
                if (expense == null)
                {
                    _logger.LogWarning("Irregular expense {ExpenseId} not found", id);
                }
                else
                {
                    _logger.LogDebug("Retrieved irregular expense {ExpenseId}: {ExpenseName}", id, expense.Name);
                }
                
                return expense;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve irregular expense {ExpenseId}", id);
                throw;
            }
        }

        public async Task UpdateIrregularExpenseAsync(IrregularExpense expense)
        {
            try
            {
                _logger.LogInformation("Updating irregular expense {ExpenseId}: {ExpenseName}", expense.Id, expense.Name);
                
                _context.Update(expense);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully updated irregular expense {ExpenseId}: {ExpenseName}", expense.Id, expense.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update irregular expense {ExpenseId}: {ExpenseName}", expense.Id, expense.Name);
                throw;
            }
        }

        public async Task DeleteIrregularExpenseAsync(int expenseId)
        {
            try
            {
                _logger.LogInformation("Deleting irregular expense {ExpenseId}", expenseId);
                
                var expense = new IrregularExpense { Id = expenseId };
                _context.Remove(expense);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully deleted irregular expense {ExpenseId}", expenseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete irregular expense {ExpenseId}", expenseId);
                throw;
            }
        }

        public async Task UpdateRegularExpenseScheduleAsync(int expenseId, decimal amount, Frequency frequency, DateTime startDate)
        {
            try
            {
                _logger.LogInformation("Updating regular expense schedule for expense {ExpenseId}: Amount {Amount}, Frequency {Frequency}, StartDate {StartDate}", 
                    expenseId, amount, frequency, startDate.ToString("yyyy-MM-dd"));
                    
                var existingExpense = await _context.RegularExpenses
                    .Include(e => e.Schedules)
                    .FirstOrDefaultAsync(e => e.Id == expenseId);

                if (existingExpense == null)
                {
                    _logger.LogError("Regular expense with ID {ExpenseId} not found for schedule update", expenseId);
                    throw new ArgumentException("Expense not found");
                }

                // Find the most recent schedule that would be active at the time of the change
                var currentSchedule = existingExpense.Schedules
                    .Where(s => s.StartDate <= DateTime.Today && (s.EndDate == null || s.EndDate >= DateTime.Today))
                    .OrderByDescending(s => s.StartYear)
                    .ThenByDescending(s => s.StartMonth)
                    .ThenByDescending(s => s.StartDay)
                    .FirstOrDefault();

                // If we're setting a future start date, we need to handle it differently
                if (startDate > DateTime.Today)
                {
                    _logger.LogDebug("Setting future start date for expense {ExpenseId}", expenseId);
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
                    _logger.LogDebug("Setting current/past start date for expense {ExpenseId}", expenseId);
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
                _logger.LogInformation("Successfully updated regular expense schedule for expense {ExpenseId}", expenseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update regular expense schedule for expense {ExpenseId}", expenseId);
                throw;
            }
        }

        public async Task<IEnumerable<RegularExpense>> GetAllRegularExpensesAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving all regular expenses");
                var expenses = await _context.RegularExpenses
                    .Include(e => e.Category)
                    .Include(e => e.FamilyMember)
                    .Include(e => e.Schedules)
                    .ToListAsync();
                _logger.LogDebug("Retrieved {ExpenseCount} regular expenses", expenses.Count);
                return expenses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve regular expenses");
                throw;
            }
        }

        public async Task<IEnumerable<FamilyMember>> GetFamilyMembersAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving active family members");
                var familyMembers = await _context.FamilyMember
                    .Where(fm => fm.IsActive)
                    .OrderBy(fm => fm.Name)
                    .ToListAsync();
                _logger.LogDebug("Retrieved {FamilyMemberCount} active family members", familyMembers.Count);
                return familyMembers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve family members");
                throw;
            }
        }

        public async Task AddFamilyMemberAsync(FamilyMember familyMember)
        {
            try
            {
                _logger.LogInformation("Adding new family member: {FamilyMemberName}", familyMember.Name);
                _context.Add(familyMember);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully added family member {FamilyMemberName} with ID {FamilyMemberId}", 
                    familyMember.Name, familyMember.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add family member {FamilyMemberName}", familyMember.Name);
                throw;
            }
        }

        public async Task<FamilyMember?> GetFamilyMemberAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving family member {FamilyMemberId}", id);
                var familyMember = await _context.FamilyMember.FindAsync(id);
                
                if (familyMember == null)
                {
                    _logger.LogWarning("Family member {FamilyMemberId} not found", id);
                }
                else
                {
                    _logger.LogDebug("Retrieved family member {FamilyMemberId}: {FamilyMemberName}", id, familyMember.Name);
                }
                
                return familyMember;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve family member {FamilyMemberId}", id);
                throw;
            }
        }

        public async Task UpdateFamilyMemberAsync(FamilyMember familyMember)
        {
            try
            {
                _logger.LogInformation("Updating family member {FamilyMemberId}: {FamilyMemberName}", 
                    familyMember.Id, familyMember.Name);
                _context.Update(familyMember);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated family member {FamilyMemberId}: {FamilyMemberName}", 
                    familyMember.Id, familyMember.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update family member {FamilyMemberId}: {FamilyMemberName}", 
                    familyMember.Id, familyMember.Name);
                throw;
            }
        }

        public async Task<List<CategoryBudget>> GetEffectiveBudgetsAsync(int year, int month)
        {
            try
            {
                _logger.LogDebug("Retrieving effective budgets for {Year}/{Month}", year, month);
                var monthIndex = year * 12 + month;
                var budgets = await _context.CategoryBudgets
                    .AsNoTracking()
                    .Include(cb => cb.ExpenseCategory)
                    .Where(cb => (cb.StartYear * 12 + cb.StartMonth) <= monthIndex &&
                                 (cb.EndYear == null || cb.EndMonth == null || (cb.EndYear * 12 + cb.EndMonth) >= monthIndex))
                    .ToListAsync();
                _logger.LogDebug("Retrieved {BudgetCount} effective budgets for {Year}/{Month}", budgets.Count, year, month);
                return budgets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve effective budgets for {Year}/{Month}", year, month);
                throw;
            }
        }

        public async Task SetCategoryBudgetAsync(int categoryId, decimal amount, int year, int month, bool applyToFuture)
        {
            try
            {
                _logger.LogInformation("Setting budget for category {CategoryId}: {Amount} for {Year}/{Month}, ApplyToFuture: {ApplyToFuture}", 
                    categoryId, amount, year, month, applyToFuture);
                    
                var monthIndex = year * 12 + month;
                var budgets = await _context.CategoryBudgets
                    .Where(cb => cb.ExpenseCategoryId == categoryId)
                    .ToListAsync();
                    
                _logger.LogDebug("Found {BudgetCount} existing budget entries for category {CategoryId}", 
                    budgets.Count, categoryId);

                if (applyToFuture)
                {
                    _logger.LogDebug("Applying budget to future months");
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
                            _logger.LogDebug("Ended existing budget {BudgetId} at {EndYear}/{EndMonth}", 
                                existing.Id, existing.EndYear, existing.EndMonth);
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
                    _logger.LogDebug("Added new open-ended budget starting {Year}/{Month}", year, month);
                }
                else
                {
                    _logger.LogDebug("Applying budget to current month only");
                    // This month only: create a single-month budget or adjust existing range
                    // If a range covers this month, split it into up to two ranges (before and after)
                    var covering = budgets.FirstOrDefault(cb => cb.IsActiveForMonth(year, month));
                    if (covering != null)
                    {
                        _logger.LogDebug("Found covering budget {BudgetId}, splitting range", covering.Id);
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
                            _logger.LogDebug("Added tail budget starting {Year}/{Month} with amount {Amount}", 
                                tail.StartYear, tail.StartMonth, tail.Amount);
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
                    _logger.LogDebug("Added single-month budget for {Year}/{Month}", year, month);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully set budget for category {CategoryId}: {Amount} for {Year}/{Month}", 
                    categoryId, amount, year, month);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set budget for category {CategoryId}: {Amount} for {Year}/{Month}", 
                    categoryId, amount, year, month);
                throw;
            }
        }

        public async Task<IEnumerable<IrregularExpenseCategoryAnalysis>> GetIrregularExpenseAnalysisAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogDebug("Analyzing irregular expenses for period {StartDate} to {EndDate}", 
                    startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                    
                var analysis = await _context.IrregularExpenses
                    .AsNoTracking()
                    .Where(e => e.Date >= startDate && e.Date <= endDate)
                    .GroupBy(e => e.Category)
                    .Select(g => new IrregularExpenseCategoryAnalysis
                    {
                        CategoryName = g.Key != null ? g.Key.Name : "Unknown",
                        TotalAmount = g.Sum(e => e.Amount)
                    })
                    .OrderByDescending(a => a.TotalAmount)
                    .ToListAsync();
                    
                _logger.LogDebug("Completed irregular expense analysis for {CategoryCount} categories", analysis.Count);
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze irregular expenses for period {StartDate} to {EndDate}", 
                    startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                throw;
            }
        }

        public async Task<IEnumerable<RegularExpenseCategoryAnalysis>> GetRegularExpenseAnalysisAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogDebug("Analyzing regular expenses for period {StartDate} to {EndDate}", 
                    startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                    
                var regularExpenses = await _context.RegularExpenses
                    .AsNoTracking()
                    .Include(e => e.Category)
                    .Include(e => e.Schedules)
                    .ToListAsync();

                var categoryTotals = new Dictionary<string, decimal>();

                foreach (var expense in regularExpenses)
                {
                    foreach (var schedule in expense.Schedules)
                    {
                        // Calculate the total amount for this expense in the period
                        var occurrences = CalculateOccurrencesInPeriod(schedule, startDate, endDate);
                        var totalForExpense = occurrences * schedule.Amount;
                        
                        var categoryName = expense.Category?.Name ?? "Unknown";
                        if (categoryTotals.ContainsKey(categoryName))
                        {
                            categoryTotals[categoryName] += totalForExpense;
                        }
                        else
                        {
                            categoryTotals[categoryName] = totalForExpense;
                        }
                    }
                }

                var analysis = categoryTotals
                    .Select(kvp => new RegularExpenseCategoryAnalysis
                    {
                        CategoryName = kvp.Key,
                        TotalAmount = kvp.Value
                    })
                    .OrderByDescending(a => a.TotalAmount)
                    .ToList();
                    
                _logger.LogDebug("Completed regular expense analysis for {CategoryCount} categories", analysis.Count);
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze regular expenses for period {StartDate} to {EndDate}", 
                    startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                throw;
            }
        }

        public async Task<IEnumerable<MonthlyExpenseTrends>> GetMonthlyExpenseTrendsAsync(int monthsBack)
        {
            try
            {
                _logger.LogDebug("Retrieving monthly expense trends for last {MonthsBack} months", monthsBack);
                
                var config = await _context.FinancialScheduleConfigs.AsNoTracking().FirstOrDefaultAsync();
                string scheduleType = config?.ScheduleType ?? "Calendar";
                int startDay = config?.StartDay ?? 1;

                var today = DateTime.Today;
                var trends = new List<MonthlyExpenseTrends>();

                // Calculate the periods going backwards
                for (int i = monthsBack - 1; i >= 0; i--)
                {
                    DateTime periodStart, periodEnd;
                    string periodLabel;

                    if (scheduleType == "Custom")
                    {
                        // Use the shared calculation method for consistency
                        var targetDate = today.AddMonths(-i);
                        var (start, end) = await CalculateFinancialMonthDatesAsync(targetDate.Year, targetDate.Month);
                        periodStart = start;
                        periodEnd = end;
                        periodLabel = $"{periodStart:MMM yyyy}";
                    }
                    else
                    {
                        // Calendar months
                        var targetDate = today.AddMonths(-i);
                        periodStart = new DateTime(targetDate.Year, targetDate.Month, 1);
                        periodEnd = periodStart.AddMonths(1).AddDays(-1);
                        periodLabel = $"{periodStart:MMM yyyy}";
                    }

                    // Get irregular expenses for this period
                    var irregularExpenses = await _context.IrregularExpenses
                        .AsNoTracking()
                        .Where(e => e.Date >= periodStart && e.Date <= periodEnd)
                        .SumAsync(e => (decimal?)e.Amount) ?? 0;

                    // Calculate regular expenses for this period
                    // This is complex because regular expenses are scheduled and may span multiple periods
                    var regularExpensesTotal = await CalculateRegularExpensesForPeriodAsync(periodStart, periodEnd);

                    trends.Add(new MonthlyExpenseTrends
                    {
                        PeriodLabel = periodLabel,
                        StartDate = periodStart,
                        EndDate = periodEnd,
                        RegularExpenses = regularExpensesTotal,
                        IrregularExpenses = irregularExpenses
                    });
                }

                var result = trends.OrderBy(t => t.StartDate);
                _logger.LogDebug("Retrieved monthly expense trends for {TrendCount} periods", result.Count());
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve monthly expense trends for last {MonthsBack} months", monthsBack);
                throw;
            }
        }

        private async Task<decimal> CalculateRegularExpensesForPeriodAsync(DateTime periodStart, DateTime periodEnd)
        {
            var allRegularExpenses = await _context.RegularExpenses
                .AsNoTracking()
                .Include(e => e.Schedules)
                .ToListAsync();

            decimal total = 0;

            foreach (var expense in allRegularExpenses)
            {
                foreach (var schedule in expense.Schedules)
                {
                    // Calculate how many times this expense occurs in the period
                    var occurrences = CalculateOccurrencesInPeriod(schedule, periodStart, periodEnd);
                    total += occurrences * schedule.Amount;
                }
            }

            return total;
        }

        private int CalculateOccurrencesInPeriod(ExpenseSchedule schedule, DateTime periodStart, DateTime periodEnd)
        {
            // This is a simplified calculation - in a real implementation you'd need more complex logic
            // to handle different frequencies (monthly, quarterly, etc.)
            
            if (schedule.Frequency == Frequency.Monthly)
            {
                // For monthly expenses, count how many months in the period
                var monthsInPeriod = ((periodEnd.Year - periodStart.Year) * 12) + periodEnd.Month - periodStart.Month + 1;
                return Math.Max(1, monthsInPeriod); // At least 1 occurrence
            }
            else if (schedule.Frequency == Frequency.Quarterly)
            {
                // Simplified quarterly calculation
                var quarters = Math.Ceiling((double)((periodEnd.Year - periodStart.Year) * 12 + periodEnd.Month - periodStart.Month + 1) / 3);
                return Math.Max(1, (int)quarters);
            }
            else if (schedule.Frequency == Frequency.SemiAnnually)
            {
                // Simplified semi-annual calculation
                var halfYears = Math.Ceiling((double)((periodEnd.Year - periodStart.Year) * 12 + periodEnd.Month - periodStart.Month + 1) / 6);
                return Math.Max(1, (int)halfYears);
            }
            else if (schedule.Frequency == Frequency.Annually)
            {
                // Simplified annual calculation
                var years = periodEnd.Year - periodStart.Year + 1;
                return Math.Max(1, years);
            }

            return 1; // Default to 1 occurrence
        }

        public async Task<(DateTime startDate, DateTime endDate)> CalculateFinancialMonthDatesAsync(int year, int month)
        {
            try
            {
                _logger.LogDebug("Calculating financial month dates for {Year}/{Month}", year, month);
                
                // Load financial schedule config
                var config = await _context.FinancialScheduleConfigs.AsNoTracking().FirstOrDefaultAsync();
                string scheduleType = config?.ScheduleType ?? "Calendar";
                int startDay = config?.StartDay ?? 1;

                DateTime startDate, endDate;
                if (scheduleType == "Custom")
                {
                    _logger.LogDebug("Using custom schedule with start day {StartDay}", startDay);
                    
                    var currentTargetMonth = new DateTime(year, month, 1);
                    var nextTargetMonth = currentTargetMonth.AddMonths(1);

                    var overrides = await _context.FinancialMonthOverrides
                        .AsNoTracking()
                        .Where(o => o.TargetMonth == currentTargetMonth || o.TargetMonth == nextTargetMonth)
                        .ToDictionaryAsync(o => o.TargetMonth);

                    var currentMonthOverride = overrides.GetValueOrDefault(currentTargetMonth);
                    var nextMonthOverride = overrides.GetValueOrDefault(nextTargetMonth);

                    // Determine the start date for the current financial month
                    startDate = currentMonthOverride?.OverrideStartDate ?? new DateTime(year, month, startDay);

                    // Determine the start date for the next financial month to calculate the current month's end date
                    DateTime nextMonthStartDate;
                    if (nextMonthOverride != null)
                    {
                        nextMonthStartDate = nextMonthOverride.OverrideStartDate;
                    }
                    else
                    {
                        var nextMonthDate = new DateTime(year, month, startDay).AddMonths(1);
                        nextMonthStartDate = new DateTime(nextMonthDate.Year, nextMonthDate.Month, startDay);
                    }
                    
                    endDate = nextMonthStartDate.AddDays(-1);
                    _logger.LogDebug("Calculated custom financial month: {StartDate} to {EndDate}", 
                        startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                }
                else
                {
                    _logger.LogDebug("Using calendar schedule");
                    // Calendar month
                    startDate = new DateTime(year, month, 1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                    _logger.LogDebug("Calculated calendar month: {StartDate} to {EndDate}", 
                        startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                }

                return (startDate, endDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate financial month dates for {Year}/{Month}", year, month);
                throw;
            }
        }
    }
}
