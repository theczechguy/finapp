using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using InvestmentTracker.Services;
using InvestmentTracker.ViewModels;
using InvestmentTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InvestmentTracker.Services.ImportProfiles;
using InvestmentTracker.Models.ImportProfiles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentTracker.Pages.Expenses
{
    public class IndexModel : PageModel
    {
        private readonly IExpenseService _expenseService;
        private readonly IBankImportProfileProvider _bankImportProfileProvider;

        public IndexModel(IExpenseService expenseService, IBankImportProfileProvider bankImportProfileProvider)
        {
            _expenseService = expenseService;
            _bankImportProfileProvider = bankImportProfileProvider;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        public MonthlyExpenseViewModel ViewModel { get; set; } = new();
        public string PrefilledOverrideDate { get; private set; } = "";
        public IReadOnlyList<BankImportProfileSummary> BankProfiles { get; private set; } = Array.Empty<BankImportProfileSummary>();
        public IReadOnlyList<BankImportProfile> BankProfilesDetailed { get; private set; } = Array.Empty<BankImportProfile>();
        public IReadOnlyList<FamilyMember> FamilyMembers { get; private set; } = Array.Empty<FamilyMember>();

        public class ImportExpensesRequest
        {
            public string? ProfileId { get; set; }
            public int SkipRows { get; set; }
            public int OriginalRowCount { get; set; }
            public List<ImportExpenseRow> Rows { get; set; } = new();
        }

        public class ImportExpenseRow
        {
            public int? SourceRowNumber { get; set; }
            public string? Name { get; set; }
            public decimal? Amount { get; set; }
            public string? Currency { get; set; }
            public string? Date { get; set; }
            public string? ExpenseType { get; set; }
            public int? FamilyMemberId { get; set; }
            public string? Memo { get; set; }
        }

        public class ImportExpensesResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int ImportedCount { get; set; }
            public List<int> FailedRows { get; set; } = new();
            public List<string> Errors { get; set; } = new();
        }

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
                selectedDate = await _expenseService.GetDefaultSelectedDateAsync();
            }
            
            var selectedYear = selectedDate.Year;
            var selectedMonth = selectedDate.Month;

            // Seed default categories if needed
            await _expenseService.SeedDefaultCategoriesAsync();

            ViewModel = await _expenseService.GetMonthlyDataAsync(selectedYear, selectedMonth);
            
            // Set the prefilled override date for the modal
            PrefilledOverrideDate = (await GetPrefilledOverrideDateAsync(selectedDate)).ToString("yyyy-MM-dd");

            var allProfiles = await _bankImportProfileProvider.GetAllProfilesAsync();
            BankProfilesDetailed = allProfiles;
            BankProfiles = allProfiles
                .Select(p => p.ToSummary())
                .OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var familyMembers = await _expenseService.GetFamilyMembersAsync();
            FamilyMembers = familyMembers
                .Where(member => member.IsActive)
                .OrderBy(member => member.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
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

        public async Task<IActionResult> OnPostImportExpensesAsync([FromBody] ImportExpensesRequest? request)
        {
            var antiforgery = HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            await antiforgery.ValidateRequestAsync(HttpContext);

            if (request == null)
            {
                var failure = new ImportExpensesResponse
                {
                    Success = false,
                    Message = "Import payload is missing."
                };

                return new JsonResult(failure)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            if (request.Rows == null || request.Rows.Count == 0)
            {
                var failure = new ImportExpensesResponse
                {
                    Success = false,
                    Message = "No expenses were provided for import."
                };

                return new JsonResult(failure)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var errors = new List<string>();
            var failedRows = new List<int>();
            var imported = 0;

            var activeMembers = (await _expenseService.GetFamilyMembersAsync())
                .ToDictionary(member => member.Id, member => member);

            foreach (var (row, index) in request.Rows.Select((row, index) => (row, index)))
            {
                if (row == null)
                {
                    continue;
                }

                var rowNumber = row.SourceRowNumber ?? index + 1;

                if (!row.Amount.HasValue)
                {
                    errors.Add($"Row {rowNumber}: Amount is required.");
                    failedRows.Add(rowNumber);
                    continue;
                }

                var roundedAmount = decimal.Round(row.Amount.Value, 2, MidpointRounding.AwayFromZero);

                var currencyCode = (row.Currency ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(currencyCode) || !Enum.TryParse(currencyCode, true, out Currency currency))
                {
                    errors.Add($"Row {rowNumber}: Unknown currency '{row.Currency ?? "(empty)"}'.");
                    failedRows.Add(rowNumber);
                    continue;
                }

                var dateText = (row.Date ?? string.Empty).Trim();
                if (!DateTime.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    errors.Add($"Row {rowNumber}: Date '{row.Date ?? "(empty)"}' is invalid.");
                    failedRows.Add(rowNumber);
                    continue;
                }

                parsedDate = parsedDate.Date;

                var name = string.IsNullOrWhiteSpace(row.Name) ? "Imported transaction" : row.Name.Trim();

                var expenseType = ExpenseType.Family;
                if (!string.IsNullOrWhiteSpace(row.ExpenseType) && Enum.TryParse(row.ExpenseType, true, out ExpenseType parsedExpenseType))
                {
                    expenseType = parsedExpenseType;
                }

                int? familyMemberId = null;
                if (expenseType == ExpenseType.Individual)
                {
                    if (!row.FamilyMemberId.HasValue)
                    {
                        errors.Add($"Row {rowNumber}: Individual expenses require a family member.");
                        failedRows.Add(rowNumber);
                        continue;
                    }

                    if (!activeMembers.TryGetValue(row.FamilyMemberId.Value, out var member))
                    {
                        errors.Add($"Row {rowNumber}: Family member {row.FamilyMemberId} is not active.");
                        failedRows.Add(rowNumber);
                        continue;
                    }

                    familyMemberId = member.Id;
                }

                var expense = new IrregularExpense
                {
                    Name = name,
                    Amount = roundedAmount,
                    Currency = currency,
                    Date = parsedDate,
                    ExpenseType = expenseType,
                    FamilyMemberId = familyMemberId
                };

                try
                {
                    await _expenseService.AddIrregularExpenseAsync(expense);
                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {rowNumber}: Failed to save expense. {ex.Message}");
                    failedRows.Add(rowNumber);
                }
            }

            if (imported == 0)
            {
                var failure = new ImportExpensesResponse
                {
                    Success = false,
                    Message = "Import failed. No expenses were saved.",
                    ImportedCount = 0,
                    FailedRows = failedRows,
                    Errors = errors
                };

                return new JsonResult(failure)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var message = imported == 1
                ? "Imported 1 expense."
                : $"Imported {imported} expenses.";

            if (failedRows.Count > 0)
            {
                message += $" Skipped {failedRows.Count} row{(failedRows.Count == 1 ? string.Empty : "s")} due to validation errors.";
            }

            var response = new ImportExpensesResponse
            {
                Success = failedRows.Count == 0,
                Message = message,
                ImportedCount = imported,
                FailedRows = failedRows,
                Errors = errors
            };

            return new JsonResult(response);
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
