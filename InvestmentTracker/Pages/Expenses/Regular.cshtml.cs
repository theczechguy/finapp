using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using InvestmentTracker.Data;
using InvestmentTracker.Models;
using InvestmentTracker.Services;

namespace InvestmentTracker.Pages.Expenses
{
    public class RegularModel : PageModel
    {
        private readonly IExpenseService _expenseService;
        private readonly AppDbContext _context;

        public RegularModel(IExpenseService expenseService, AppDbContext context)
        {
            _expenseService = expenseService;
            _context = context;
        }

        public List<RegularExpense> RegularExpenses { get; set; } = new();
        public List<ExpenseCategory> Categories { get; set; } = new();
        public decimal TotalMonthlyAmount { get; set; }
        public decimal TotalAnnualAmount { get; set; }
        public int AlternativeScheduleCount { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            // Load all regular expenses with their categories and schedules
            RegularExpenses = await _context.RegularExpenses
                .Include(e => e.Category)
                .Include(e => e.Schedules)
                .AsNoTracking()
                .OrderBy(e => e.Name)
                .ToListAsync();

            // Ensure display values reflect the defined schedule (even if starting in future)
            foreach (var e in RegularExpenses)
            {
                var latest = e.Schedules
                    .OrderByDescending(s => s.StartYear * 12 + s.StartMonth)
                    .FirstOrDefault();
                if (latest != null)
                {
                    e.DisplayFrequency = latest.Frequency;
                    e.DisplayAmount = latest.Amount;
                }
            }

            // Load all categories for filtering
            Categories = await _context.ExpenseCategories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Calculate summary statistics
            CalculateSummaryStatistics();
        }

        private void CalculateSummaryStatistics()
        {
            if (!RegularExpenses.Any())
            {
                TotalMonthlyAmount = 0;
                TotalAnnualAmount = 0;
                AlternativeScheduleCount = 0;
                return;
            }

            // Calculate monthly average (all expenses converted to monthly equivalent)
            TotalMonthlyAmount = RegularExpenses.Sum(e => e.Recurrence switch
            {
                Frequency.Monthly => e.Amount,
                Frequency.Quarterly => e.Amount / 3, // Quarterly divided by 3 months
                Frequency.SemiAnnually => e.Amount / 6, // Semi-annual divided by 6 months
                Frequency.Annually => e.Amount / 12, // Annual divided by 12 months
                _ => e.Amount
            });

            // Calculate total annual amount
            TotalAnnualAmount = RegularExpenses.Sum(e => e.Recurrence switch
            {
                Frequency.Monthly => e.Amount * 12,
                Frequency.Quarterly => e.Amount * 4,
                Frequency.SemiAnnually => e.Amount * 2,
                Frequency.Annually => e.Amount,
                _ => e.Amount * 12
            });

            // Count alternative schedules (non-monthly)
            AlternativeScheduleCount = RegularExpenses.Count(e => e.Recurrence != Frequency.Monthly);
        }

        public DateTime? GetNextDueDate(RegularExpense expense)
        {
            var currentDate = DateTime.Now;
            var currentSchedule = expense.Schedules
                .OrderByDescending(s => s.StartYear * 12 + s.StartMonth)
                .FirstOrDefault();
            
            if (currentSchedule == null)
                return null;

            switch (currentSchedule.Frequency)
            {
                case Frequency.Monthly:
                    // For monthly expenses, next due is always next month
                    return new DateTime(currentDate.Year, currentDate.Month, 1).AddMonths(1);

                case Frequency.Quarterly:
                    return GetNextOccurrence(currentDate, currentSchedule.StartMonth, 3);

                case Frequency.SemiAnnually:
                    return GetNextOccurrence(currentDate, currentSchedule.StartMonth, 6);

                case Frequency.Annually:
                    return GetNextOccurrence(currentDate, currentSchedule.StartMonth, 12);

                default:
                    return null;
            }
        }

        private DateTime GetNextOccurrence(DateTime currentDate, int startingMonth, int intervalMonths)
        {
            var currentYear = currentDate.Year;
            var currentMonth = currentDate.Month;

            // Find the next occurrence starting from the current year
            for (int year = currentYear; year <= currentYear + 2; year++)
            {
                for (int month = startingMonth; month <= 12; month += intervalMonths)
                {
                    var occurrenceDate = new DateTime(year, month, 1);
                    
                    // If this occurrence is in the future (or current month), return it
                    if (occurrenceDate.Year > currentYear || 
                        (occurrenceDate.Year == currentYear && occurrenceDate.Month >= currentMonth))
                    {
                        return occurrenceDate;
                    }
                }
                
                // For subsequent years, reset starting month to the base starting month
                // but adjust if we've passed it in the current year
                if (year == currentYear && currentMonth > startingMonth)
                {
                    // Find next valid month in this year
                    for (int month = startingMonth; month <= 12; month += intervalMonths)
                    {
                        if (month > currentMonth)
                        {
                            return new DateTime(year, month, 1);
                        }
                    }
                }
            }

            // Fallback - should not reach here under normal circumstances
            return new DateTime(currentYear + 1, startingMonth, 1);
        }

        public async Task<IActionResult> OnPostCreateAsync(
            string name, 
            int categoryId, 
            decimal amount, 
            string frequency, 
            int? startingMonth, 
            string? description)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(name) || categoryId <= 0 || amount <= 0 || string.IsNullOrWhiteSpace(frequency))
                {
                    return new JsonResult(new { success = false, message = "Please fill in all required fields." });
                }

                // Parse frequency
                if (!Enum.TryParse<Frequency>(frequency, out var parsedFrequency))
                {
                    return new JsonResult(new { success = false, message = "Invalid frequency selected." });
                }

                // Validate starting month for non-monthly frequencies
                if (parsedFrequency != Frequency.Monthly && (!startingMonth.HasValue || startingMonth < 1 || startingMonth > 12))
                {
                    return new JsonResult(new { success = false, message = "Starting month is required for non-monthly frequencies." });
                }

                // Create the regular expense
                var expense = new RegularExpense
                {
                    Name = name.Trim(),
                    Description = description?.Trim(),
                    ExpenseCategoryId = categoryId
                };

                // Create the expense schedule
                var schedule = new ExpenseSchedule
                {
                    StartYear = DateTime.Now.Year,
                    StartMonth = startingMonth ?? DateTime.Now.Month,
                    Amount = amount,
                    Frequency = parsedFrequency,
                    RegularExpense = expense
                };

                expense.Schedules.Add(schedule);

                _context.RegularExpenses.Add(expense);
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Regular expense created successfully." });
            }
            catch
            {
                return new JsonResult(new { success = false, message = "An error occurred while creating the expense." });
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync(
            int id,
            string name, 
            int categoryId, 
            decimal amount, 
            string frequency, 
            int? startingMonth, 
            string? description)
        {
            try
            {
                // Validate inputs
                if (id <= 0 || string.IsNullOrWhiteSpace(name) || categoryId <= 0 || amount <= 0 || string.IsNullOrWhiteSpace(frequency))
                {
                    return new JsonResult(new { success = false, message = "Please fill in all required fields." });
                }

                // Parse frequency
                if (!Enum.TryParse<Frequency>(frequency, out var parsedFrequency))
                {
                    return new JsonResult(new { success = false, message = "Invalid frequency selected." });
                }

                // Validate starting month for non-monthly frequencies
                if (parsedFrequency != Frequency.Monthly && (!startingMonth.HasValue || startingMonth < 1 || startingMonth > 12))
                {
                    return new JsonResult(new { success = false, message = "Starting month is required for non-monthly frequencies." });
                }

                // Find the expense
                var expense = await _context.RegularExpenses
                    .Include(e => e.Schedules)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (expense == null)
                {
                    return new JsonResult(new { success = false, message = "Expense not found." });
                }

                // Update the expense
                expense.Name = name.Trim();
                expense.Description = description?.Trim();
                expense.ExpenseCategoryId = categoryId;

                // Update or create the schedule
                var currentSchedule = expense.Schedules.FirstOrDefault();
                if (currentSchedule != null)
                {
                    // Update existing schedule
                    currentSchedule.Amount = amount;
                    currentSchedule.Frequency = parsedFrequency;
                    currentSchedule.StartMonth = startingMonth ?? DateTime.Now.Month;
                }
                else
                {
                    // Create new schedule if none exists
                    var schedule = new ExpenseSchedule
                    {
                        StartYear = DateTime.Now.Year,
                        StartMonth = startingMonth ?? DateTime.Now.Month,
                        Amount = amount,
                        Frequency = parsedFrequency,
                        RegularExpense = expense
                    };
                    expense.Schedules.Add(schedule);
                }

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Regular expense updated successfully." });
            }
            catch
            {
                return new JsonResult(new { success = false, message = "An error occurred while updating the expense." });
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var expense = await _context.RegularExpenses
                    .Include(e => e.Schedules)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (expense == null)
                {
                    return new JsonResult(new { success = false, message = "Expense not found." });
                }

                _context.RegularExpenses.Remove(expense);
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Regular expense deleted successfully." });
            }
            catch
            {
                return new JsonResult(new { success = false, message = "An error occurred while deleting the expense." });
            }
        }

        public async Task<IActionResult> OnGetExpenseDetailsAsync(int id)
        {
            try
            {
                var expense = await _context.RegularExpenses
                    .Include(e => e.Category)
                    .Include(e => e.Schedules)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (expense == null)
                {
                    return new JsonResult(new { success = false, message = "Expense not found." });
                }

                var currentSchedule = expense.Schedules.FirstOrDefault();

                return new JsonResult(new 
                { 
                    success = true,
                    expense = new
                    {
                        id = expense.Id,
                        name = expense.Name,
                        description = expense.Description,
                        amount = currentSchedule?.Amount ?? 0,
                        categoryId = expense.ExpenseCategoryId,
                        frequency = currentSchedule?.Frequency.ToString() ?? "Monthly",
                        startingMonth = currentSchedule?.StartMonth ?? DateTime.Now.Month,
                        hasSchedule = currentSchedule != null
                    }
                });
            }
            catch
            {
                return new JsonResult(new { success = false, message = "An error occurred while loading the expense details." });
            }
        }

        public string GetDetailedSchedule(RegularExpense expense)
        {
            if (expense.Schedules == null || !expense.Schedules.Any())
                return "No schedule defined";

            var currentSchedule = expense.Schedules
                .OrderByDescending(s => s.StartYear * 12 + s.StartMonth)
                .First();

            var scheduleInfo = new List<string>();
            
            // Add period info
            var startDate = $"{currentSchedule.StartYear}-{currentSchedule.StartMonth:D2}";
            var endDate = currentSchedule.EndYear.HasValue && currentSchedule.EndMonth.HasValue 
                ? $"{currentSchedule.EndYear}-{currentSchedule.EndMonth:D2}" 
                : "ongoing";
            
            scheduleInfo.Add($"<strong>Period:</strong> {startDate} to {endDate}");
            
            // Add frequency-specific information with upcoming months
            var frequencyInfo = GetFrequencyDescription(currentSchedule);
            if (!string.IsNullOrEmpty(frequencyInfo))
            {
                scheduleInfo.Add($"<strong>Schedule:</strong> {frequencyInfo}");
            }
            
            return string.Join("<br/>", scheduleInfo);
        }

        public string GetFrequencyDescription(ExpenseSchedule schedule)
        {
            var today = DateTime.Today;
            var upcomingMonths = new List<string>();
            
            // Check next 12 months to show upcoming occurrences
            for (int i = 0; i < 12; i++)
            {
                var checkDate = today.AddMonths(i);
                if (schedule.ShouldApplyInMonth(checkDate.Year, checkDate.Month))
                {
                    upcomingMonths.Add(checkDate.ToString("MMM yyyy"));
                    if (upcomingMonths.Count >= 4) break; // Show max 4 upcoming occurrences
                }
            }
            
            if (schedule.Frequency == Frequency.Monthly)
            {
                return "Every month";
            }
            else if (upcomingMonths.Any())
            {
                return $"Next occurrences: {string.Join(", ", upcomingMonths)}";
            }
            
            return schedule.Frequency.ToString();
        }

        private List<string> GetScheduleMonths(int startMonth, int intervalMonths, int currentYear, int nextYear)
        {
            var months = new List<string>();
            
            // Get months for current year
            for (int month = startMonth; month <= 12; month += intervalMonths)
            {
                months.Add(new DateTime(currentYear, month, 1).ToString("MMM"));
            }
            
            // Get months for next year (up to 12 months ahead)
            for (int month = startMonth; month <= 12 && months.Count < 4; month += intervalMonths)
            {
                if (month <= 12)
                {
                    months.Add(new DateTime(nextYear, month, 1).ToString("MMM"));
                }
            }
            
            return months.Take(4).ToList(); // Limit to 4 months to keep display clean
        }
    }
}
