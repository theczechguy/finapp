using System;
using System.Threading.Tasks;
using InvestmentTracker.Services;
using InvestmentTracker.ViewModels;
using InvestmentTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvestmentTracker.Pages.Expenses
{
    public class IndexModel : PageModel
    {
        private readonly IExpenseService _expenseService;

        public IndexModel(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        public MonthlyExpenseViewModel ViewModel { get; set; } = new();
        public string PrefilledOverrideDate { get; private set; } = "";

        public async Task OnGetAsync()
        {
            DateTime selectedDate;
            
            if (SelectedDate.HasValue)
            {
                selectedDate = SelectedDate.Value;
            }
            else
            {
                // When no date is specified, determine the default based on schedule configuration
                selectedDate = await GetDefaultSelectedDateAsync();
            }
            
            var selectedYear = selectedDate.Year;
            var selectedMonth = selectedDate.Month;

            // Seed default categories if needed
            await _expenseService.SeedDefaultCategoriesAsync();

            ViewModel = await _expenseService.GetMonthlyDataAsync(selectedYear, selectedMonth);
            
            // Set the prefilled override date for the modal
            PrefilledOverrideDate = (await GetPrefilledOverrideDateAsync(selectedDate)).ToString("yyyy-MM-dd");
        }

        private async Task<DateTime> GetDefaultSelectedDateAsync()
        {
            // Load financial schedule config to determine default view
            var config = await _expenseService.GetFinancialScheduleConfigAsync();
            string scheduleType = config?.ScheduleType ?? "Calendar";
            int startDay = config?.StartDay ?? 1;
            
            var today = DateTime.Today;
            
            if (scheduleType == "Custom")
            {
                // For custom schedules, find the start date of the period containing today
                int currentYear = today.Year;
                int currentMonth = today.Month; // 1-based
                int currentDay = today.Day;
                
                if (currentDay >= startDay)
                {
                    // Current date is in the period starting this month
                    return new DateTime(currentYear, currentMonth, startDay);
                }
                else
                {
                    // Current date is in the period starting last month
                    if (currentMonth == 1)
                    {
                        // January, so go to December of previous year
                        return new DateTime(currentYear - 1, 12, startDay);
                    }
                    else
                    {
                        return new DateTime(currentYear, currentMonth - 1, startDay);
                    }
                }
            }
            else
            {
                // For calendar months, use the first day of current month
                return new DateTime(today.Year, today.Month, 1);
            }
        }

        public async Task<IActionResult> OnPostUpdateIncomeAsync(int incomeSourceId, decimal actualAmount, int year, int month)
        {
            await _expenseService.LogOrUpdateMonthlyIncomeAsync(incomeSourceId, year, month, actualAmount);
            TempData["ToastSuccess"] = "Income updated.";
            return RedirectToPage(new { SelectedDate = new DateTime(year, month, 1) });
        }

        public async Task<IActionResult> OnPostAddRegularExpenseAsync(string name, decimal amount, int categoryId, string frequency, int startYear, int startMonth, string currency, string expenseType, int? familyMemberId)
        {
            var expense = new RegularExpense
            {
                Name = name,
                ExpenseCategoryId = categoryId,
                Currency = Enum.Parse<Currency>(currency),
                ExpenseType = Enum.Parse<ExpenseType>(expenseType),
                FamilyMemberId = familyMemberId
            };

            // Create initial schedule with month and day-based fields
            int startDay = 1;
            if (Request.Form.ContainsKey("startDay"))
            {
                var startDayValue = Request.Form["startDay"];
                if (!string.IsNullOrEmpty(startDayValue) && int.TryParse(startDayValue, out var parsedDay))
                {
                    startDay = parsedDay;
                }
            }
            expense.Schedules.Add(new ExpenseSchedule
            {
                StartYear = startYear,
                StartMonth = startMonth,
                StartDay = startDay,
                Amount = amount,
                Frequency = Enum.Parse<Frequency>(frequency)
            });

            await _expenseService.AddRegularExpenseAsync(expense);
            TempData["ToastSuccess"] = "Regular expense added.";
            return RedirectToPage(new { SelectedDate });
        }

        public async Task<IActionResult> OnPostAddIrregularExpenseAsync(string name, decimal amount, int categoryId, DateTime date, string currency, string expenseType, int? familyMemberId)
        {
            var expense = new IrregularExpense
            {
                Name = name,
                Amount = amount,
                ExpenseCategoryId = categoryId,
                Date = date,
                Currency = Enum.Parse<Currency>(currency),
                ExpenseType = Enum.Parse<ExpenseType>(expenseType),
                FamilyMemberId = familyMemberId
            };

            await _expenseService.AddIrregularExpenseAsync(expense);
            TempData["ToastSuccess"] = "Irregular expense added.";
            return RedirectToPage(new { SelectedDate });
        }

        public async Task<IActionResult> OnPostDeleteIrregularExpenseAsync(int expenseId)
        {
            await _expenseService.DeleteIrregularExpenseAsync(expenseId);
            TempData["ToastSuccess"] = "Irregular expense deleted.";
            return RedirectToPage(new { SelectedDate });
        }

        public async Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync()
        {
            return await _expenseService.GetExpenseCategoriesAsync();
        }

        public async Task<IActionResult> OnPostUpdateRegularExpenseAsync(int id, string name, decimal amount, int categoryId, string frequency, int startYear, int startMonth, string currency, string expenseType, int? familyMemberId)
        {
            var existingExpense = await _expenseService.GetRegularExpenseAsync(id);
            if (existingExpense == null)
            {
                return NotFound();
            }

            // Update basic properties
            existingExpense.Name = name;
            existingExpense.ExpenseCategoryId = categoryId;
            existingExpense.Currency = Enum.Parse<Currency>(currency);
            existingExpense.ExpenseType = Enum.Parse<ExpenseType>(expenseType);
            existingExpense.FamilyMemberId = familyMemberId;

            // Handle schedule updates with temporal logic
            var startDate = new DateTime(startYear, startMonth, 1);
            await _expenseService.UpdateRegularExpenseScheduleAsync(id, amount, Enum.Parse<Frequency>(frequency), startDate);
            TempData["ToastSuccess"] = "Regular expense updated.";
            return RedirectToPage(new { SelectedDate });
        }

        public async Task<IActionResult> OnPostUpdateIrregularExpenseAsync(int id, string name, decimal amount, int categoryId, DateTime date, string currency, string expenseType, int? familyMemberId)
        {
            var existingExpense = await _expenseService.GetIrregularExpenseAsync(id);
            if (existingExpense == null)
            {
                return NotFound();
            }

            existingExpense.Name = name;
            existingExpense.Amount = amount;
            existingExpense.ExpenseCategoryId = categoryId;
            existingExpense.Date = date;
            existingExpense.Currency = Enum.Parse<Currency>(currency);
            existingExpense.ExpenseType = Enum.Parse<ExpenseType>(expenseType);
            existingExpense.FamilyMemberId = familyMemberId;

            await _expenseService.UpdateIrregularExpenseAsync(existingExpense);
            TempData["ToastSuccess"] = "Irregular expense updated.";
            return RedirectToPage(new { SelectedDate });
        }

        public async Task<IActionResult> OnPostAddOneTimeIncomeAsync(string name, decimal amount, DateTime date, string currency, int? incomeSourceId)
        {
            var income = new OneTimeIncome
            {
                Name = name,
                Amount = amount,
                Date = date,
                Currency = Enum.Parse<Currency>(currency),
                IncomeSourceId = incomeSourceId
            };

            await _expenseService.AddOneTimeIncomeAsync(income);
            TempData["ToastSuccess"] = "One-time income added.";
            return RedirectToPage(new { SelectedDate });
        }

        public async Task<IActionResult> OnPostUpdateOneTimeIncomeAsync(int id, string name, decimal amount, DateTime date, string currency, int? incomeSourceId)
        {
            var existingIncome = await _expenseService.GetOneTimeIncomeAsync(id);
            if (existingIncome == null)
            {
                return NotFound();
            }

            existingIncome.Name = name;
            existingIncome.Amount = amount;
            existingIncome.Date = date;
            existingIncome.Currency = Enum.Parse<Currency>(currency);
            existingIncome.IncomeSourceId = incomeSourceId;

            await _expenseService.UpdateOneTimeIncomeAsync(existingIncome);
            TempData["ToastSuccess"] = "One-time income updated.";
            return RedirectToPage(new { SelectedDate });
        }

        public async Task<IActionResult> OnPostDeleteOneTimeIncomeAsync(int incomeId)
        {
            await _expenseService.DeleteOneTimeIncomeAsync(incomeId);
            TempData["ToastSuccess"] = "One-time income deleted.";
            return RedirectToPage(new { SelectedDate });
        }

        public async Task<IActionResult> OnPostAdjustStartDateAsync(DateTime overrideDate)
        {
            if (SelectedDate == null)
            {
                TempData["ToastError"] = "Cannot adjust start date without a selected month.";
                return RedirectToPage();
            }

            var targetMonth = new DateTime(SelectedDate.Value.Year, SelectedDate.Value.Month, 1);
            await _expenseService.SetFinancialMonthOverrideAsync(targetMonth, overrideDate);

            TempData["ToastSuccess"] = "Financial month start date adjusted.";
            return RedirectToPage(new { SelectedDate });
        }

        public async Task<IEnumerable<IncomeSource>> GetIncomeSourcesAsync()
        {
            return await _expenseService.GetAllIncomeSourcesAsync();
        }

        public async Task<IEnumerable<RegularExpense>> GetAllRegularExpensesAsync()
        {
            return await _expenseService.GetAllRegularExpensesAsync();
        }

        public async Task<IEnumerable<FamilyMember>> GetFamilyMembersAsync()
        {
            return await _expenseService.GetFamilyMembersAsync();
        }

        private async Task<DateTime> GetPrefilledOverrideDateAsync(DateTime selectedDate)
        {
            // First check if there's an existing override for this month
            var targetMonth = new DateTime(selectedDate.Year, selectedDate.Month, 1);
            
            // Check for existing override using the service
            var existingOverrideDate = await _expenseService.GetExistingFinancialMonthOverrideAsync(targetMonth);
            
            if (existingOverrideDate.HasValue)
            {
                // If there's an existing override, use that date
                return existingOverrideDate.Value;
            }
            
            // If no override exists, use the standard configured start date
            var scheduleConfig = await _expenseService.GetFinancialScheduleConfigAsync();
            string scheduleType = scheduleConfig?.ScheduleType ?? "Calendar";
            int startDay = scheduleConfig?.StartDay ?? 1;
            
            if (scheduleType == "Custom")
            {
                return new DateTime(selectedDate.Year, selectedDate.Month, startDay);
            }
            else
            {
                return new DateTime(selectedDate.Year, selectedDate.Month, 1);
            }
        }
    }
}
