using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using InvestmentTracker.Models;
using InvestmentTracker.Models.ImportProfiles;
using InvestmentTracker.Services.ImportProfiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
        public ImportProfile Entity { get; set; } = new();

        [BindProperty]
        public string ProfileJson { get; set; } = "{}";

        public bool IsNew => Entity.Id == 0;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id.HasValue)
            {
                var entity = await _service.GetEntityAsync(id.Value);
                if (entity == null) return NotFound();
                Entity = entity;
            }
            else
            {
                Entity = new ImportProfile();
                Entity.ProfileData = new BankImportProfile();
            }
            
            // Serialize ProfileData to JSON for editing
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            ProfileJson = JsonSerializer.Serialize(Entity.ProfileData, options);
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Entity.Name))
            {
                ModelState.AddModelError("Entity.Name", "Name is required.");
                return Page();
            }

            BankImportProfile profileData;
            try 
            {
                profileData = JsonSerializer.Deserialize<BankImportProfile>(ProfileJson) ?? new BankImportProfile();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("ProfileJson", $"Invalid JSON: {ex.Message}");
                return Page();
            }

            // Sync metadata
            profileData.Metadata.DisplayName = Entity.Name;
            profileData.Metadata.Description = Entity.Description;

            if (Entity.Id == 0)
            {
                Entity.ProfileData = profileData;
                await _service.CreateProfileAsync(Entity);
            }
            else
            {
                await _service.UpdateProfileAsync(Entity.Id, profileData, Entity.Name, Entity.Description);
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
    }
}
