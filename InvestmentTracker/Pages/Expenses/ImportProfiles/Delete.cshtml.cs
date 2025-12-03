using System.Threading.Tasks;
using InvestmentTracker.Services.ImportProfiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvestmentTracker.Pages.Expenses.ImportProfiles
{
    public class DeleteModel : PageModel
    {
        private readonly ImportProfileService _service;

        public DeleteModel(ImportProfileService service)
        {
            _service = service;
        }

        public int Id { get; set; }
        public string ProfileName { get; set; } = "";
        public string ProfileDescription { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var profile = await _service.GetEntityAsync(id);
            if (profile == null)
            {
                return NotFound();
            }

            Id = profile.Id;
            ProfileName = profile.Name;
            ProfileDescription = profile.Description;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var success = await _service.DeleteProfileAsync(id);
            if (!success)
            {
                return NotFound();
            }

            TempData["ToastSuccess"] = "Import profile deleted successfully.";
            return RedirectToPage("./Index");
        }
    }
}
