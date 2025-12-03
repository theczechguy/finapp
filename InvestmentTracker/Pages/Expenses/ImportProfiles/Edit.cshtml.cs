using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using InvestmentTracker.Models;
using InvestmentTracker.Models.ImportProfiles;
using InvestmentTracker.Services.ImportProfiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InvestmentTracker.Pages.Expenses.ImportProfiles
{
    public class EditModel : PageModel
    {
        private readonly ImportProfileService _service;

        public EditModel(ImportProfileService service)
        {
            _service = service;
        }

        [BindProperty]
        public ImportProfileViewModel Profile { get; set; } = new();

        public bool IsNew => Profile.Id == 0;

        public SelectList EncodingOptions { get; } = new SelectList(new[] 
        { 
            "utf-8", "windows-1250", "iso-8859-1", "iso-8859-2" 
        });

        public SelectList DelimiterOptions { get; } = new SelectList(new[]
        {
            new { Value = ";", Text = "Semicolon (;)" },
            new { Value = ",", Text = "Comma (,)" },
            new { Value = "|", Text = "Pipe (|)" },
            new { Value = "\t", Text = "Tab" }
        }, "Value", "Text");

        public SelectList DecimalSeparatorOptions { get; } = new SelectList(new[]
        {
            new { Value = ",", Text = "Comma (,)" },
            new { Value = ".", Text = "Dot (.)" }
        }, "Value", "Text");

        public SelectList AmountTransformOptions { get; } = new SelectList(new[]
        {
            new { Value = "", Text = "None (Standard)" },
            new { Value = "signedAmount", Text = "Signed Amount (Negative = Outflow)" },
            new { Value = "directionSignedAmount", Text = "Direction Column Based" }
        }, "Value", "Text");

        public class ImportProfileViewModel
        {
            public int Id { get; set; }

            [Required]
            [Display(Name = "Profile Name")]
            public string Name { get; set; } = "";

            [Display(Name = "Description")]
            public string Description { get; set; } = "";

            // Parser Settings
            [Required]
            public string Delimiter { get; set; } = ";";
            
            [Required]
            [Display(Name = "Decimal Separator")]
            public string DecimalSeparator { get; set; } = ",";
            
            [Required]
            [Display(Name = "Date Format")]
            public string DateFormat { get; set; } = "dd.MM.yyyy";
            
            [Required]
            public string Encoding { get; set; } = "utf-8";
            
            [Display(Name = "Skip Rows")]
            public int SkipRows { get; set; } = 0;

            // Mappings
            [Display(Name = "Date Headers", Description = "Comma-separated list of column names")]
            public string? DateHeaders { get; set; }

            [Display(Name = "Amount Headers", Description = "Comma-separated list of column names")]
            public string? AmountHeaders { get; set; }

            [Display(Name = "Amount Transform")]
            public string? AmountTransform { get; set; }

            [Display(Name = "Direction Header")]
            public string? DirectionHeader { get; set; }

            [Display(Name = "Currency Headers", Description = "Comma-separated list of column names")]
            public string? CurrencyHeaders { get; set; }

            [Display(Name = "Default Currency")]
            public string? CurrencyDefault { get; set; }

            [Display(Name = "Counterparty / Name Headers", Description = "Comma-separated list of column names")]
            public string? NameHeaders { get; set; }

            [Display(Name = "Name Fallback")]
            public string? NameFallback { get; set; } = "Imported Transaction";

            [Display(Name = "Memo / Note Headers", Description = "Comma-separated list of column names")]
            public string? MemoHeaders { get; set; }
            
            [Display(Name = "Counterparty Account Headers", Description = "Comma-separated list of column names")]
            public string? CounterpartyAccountHeaders { get; set; }

            [Display(Name = "Default Expense Type")]
            public string? ExpenseTypeDefault { get; set; } = "Family";
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id.HasValue)
            {
                var entity = await _service.GetEntityAsync(id.Value);
                if (entity == null) return NotFound();
                
                MapEntityToViewModel(entity);
            }
            else
            {
                Profile = new ImportProfileViewModel();
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var profileData = new BankImportProfile
            {
                Id = Profile.Name.ToLowerInvariant().Replace(" ", "-"), // Generate ID from name
                Version = 2,
                Metadata = new BankImportProfileMetadata
                {
                    DisplayName = Profile.Name,
                    Description = Profile.Description
                },
                Parser = new BankImportProfileParser
                {
                    Delimiter = Profile.Delimiter,
                    DecimalSeparator = Profile.DecimalSeparator,
                    DateFormat = Profile.DateFormat,
                    Encoding = Profile.Encoding,
                    DefaultSkipRows = Profile.SkipRows
                }
            };

            // Reconstruct Mappings
            var mappings = new List<ExpenseFieldMapping>();

            // Helper to split headers
            List<string> SplitHeaders(string? input) => 
                input?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? new List<string>();

            // Date
            if (!string.IsNullOrWhiteSpace(Profile.DateHeaders))
            {
                mappings.Add(new ExpenseFieldMapping 
                { 
                    Field = "date", 
                    SourceHeaders = SplitHeaders(Profile.DateHeaders)
                });
            }

            // Amount
            if (!string.IsNullOrWhiteSpace(Profile.AmountHeaders))
            {
                var amountMapping = new ExpenseFieldMapping 
                { 
                    Field = "amount", 
                    SourceHeaders = SplitHeaders(Profile.AmountHeaders),
                    Transform = !string.IsNullOrWhiteSpace(Profile.AmountTransform) ? Profile.AmountTransform : null
                };

                if (Profile.AmountTransform == "directionSignedAmount" && !string.IsNullOrWhiteSpace(Profile.DirectionHeader))
                {
                    amountMapping.TransformSettings["directionHeader"] = Profile.DirectionHeader;
                }

                mappings.Add(amountMapping);
            }

            // Currency
            var currencyMapping = new ExpenseFieldMapping { Field = "currency" };
            if (!string.IsNullOrWhiteSpace(Profile.CurrencyHeaders))
            {
                currencyMapping.SourceHeaders = SplitHeaders(Profile.CurrencyHeaders);
            }
            if (!string.IsNullOrWhiteSpace(Profile.CurrencyDefault))
            {
                currencyMapping.Fallback = Profile.CurrencyDefault;
            }
            if (currencyMapping.SourceHeaders.Any() || !string.IsNullOrWhiteSpace(currencyMapping.Fallback))
            {
                mappings.Add(currencyMapping);
            }

            // Name
            var nameMapping = new ExpenseFieldMapping { Field = "name" };
            if (!string.IsNullOrWhiteSpace(Profile.NameHeaders))
            {
                nameMapping.SourceHeaders = SplitHeaders(Profile.NameHeaders);
            }
            nameMapping.Fallback = !string.IsNullOrWhiteSpace(Profile.NameFallback) ? Profile.NameFallback : "Imported Transaction";
            mappings.Add(nameMapping);

            // Memo
            if (!string.IsNullOrWhiteSpace(Profile.MemoHeaders))
            {
                mappings.Add(new ExpenseFieldMapping 
                { 
                    Field = "memo", 
                    SourceHeaders = SplitHeaders(Profile.MemoHeaders)
                });
            }
            
            // Counterparty Account
            if (!string.IsNullOrWhiteSpace(Profile.CounterpartyAccountHeaders))
            {
                mappings.Add(new ExpenseFieldMapping 
                { 
                    Field = "counterpartyAccount", 
                    SourceHeaders = SplitHeaders(Profile.CounterpartyAccountHeaders)
                });
            }

            // Expense Type
            mappings.Add(new ExpenseFieldMapping 
            { 
                Field = "expenseType", 
                Fallback = Profile.ExpenseTypeDefault 
            });

            profileData.ExpenseFieldMappings = mappings;

            if (Profile.Id == 0)
            {
                var newEntity = new ImportProfile
                {
                    Name = Profile.Name,
                    Description = Profile.Description,
                    ProfileData = profileData
                };
                await _service.CreateProfileAsync(newEntity);
            }
            else
            {
                await _service.UpdateProfileAsync(Profile.Id, profileData, Profile.Name, Profile.Description);
            }

            TempData["ToastSuccess"] = "Profile saved successfully.";
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _service.DeleteProfileAsync(id);
            TempData["ToastSuccess"] = "Profile deleted successfully.";
            return RedirectToPage("./Index");
        }

        private void MapEntityToViewModel(ImportProfile entity)
        {
            var data = entity.ProfileData;
            
            Profile = new ImportProfileViewModel
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                
                Delimiter = data.Parser.Delimiter,
                DecimalSeparator = data.Parser.DecimalSeparator,
                DateFormat = data.Parser.DateFormat,
                Encoding = data.Parser.Encoding,
                SkipRows = data.Parser.DefaultSkipRows
            };

            // Helper to join headers
            string JoinHeaders(List<string> headers) => string.Join(", ", headers ?? new List<string>());

            // Map Fields
            var dateMap = data.ExpenseFieldMappings.FirstOrDefault(m => m.Field == "date");
            if (dateMap != null) Profile.DateHeaders = JoinHeaders(dateMap.SourceHeaders);

            var amountMap = data.ExpenseFieldMappings.FirstOrDefault(m => m.Field == "amount");
            if (amountMap != null)
            {
                Profile.AmountHeaders = JoinHeaders(amountMap.SourceHeaders);
                Profile.AmountTransform = amountMap.Transform ?? "";
                if (amountMap.TransformSettings.TryGetValue("directionHeader", out var dirHeader))
                {
                    Profile.DirectionHeader = dirHeader;
                }
            }

            var currencyMap = data.ExpenseFieldMappings.FirstOrDefault(m => m.Field == "currency");
            if (currencyMap != null)
            {
                Profile.CurrencyHeaders = JoinHeaders(currencyMap.SourceHeaders);
                Profile.CurrencyDefault = currencyMap.Fallback ?? "";
            }

            var nameMap = data.ExpenseFieldMappings.FirstOrDefault(m => m.Field == "name");
            if (nameMap != null)
            {
                Profile.NameHeaders = JoinHeaders(nameMap.SourceHeaders);
                Profile.NameFallback = nameMap.Fallback ?? "";
            }

            var memoMap = data.ExpenseFieldMappings.FirstOrDefault(m => m.Field == "memo");
            if (memoMap != null) Profile.MemoHeaders = JoinHeaders(memoMap.SourceHeaders);
            
            var accountMap = data.ExpenseFieldMappings.FirstOrDefault(m => m.Field == "counterpartyAccount");
            if (accountMap != null) Profile.CounterpartyAccountHeaders = JoinHeaders(accountMap.SourceHeaders);

            var typeMap = data.ExpenseFieldMappings.FirstOrDefault(m => m.Field == "expenseType");
            if (typeMap != null) Profile.ExpenseTypeDefault = typeMap.Fallback ?? "Family";
        }
    }
}
