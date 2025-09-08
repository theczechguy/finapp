using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvestmentTracker.Pages.Expenses
{
    public class FamilyMembersModel : PageModel
    {
        private readonly IExpenseService _expenseService;

        public FamilyMembersModel(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        public List<FamilyMember> FamilyMembers { get; set; } = new();

        public async Task OnGetAsync()
        {
            FamilyMembers = (await _expenseService.GetFamilyMembersAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAddFamilyMemberAsync(string name, string relationship)
        {
            var familyMember = new FamilyMember
            {
                Name = name,
                Relationship = relationship,
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            await _expenseService.AddFamilyMemberAsync(familyMember);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateFamilyMemberAsync(int id, string name, string relationship)
        {
            var existingMember = await _expenseService.GetFamilyMemberAsync(id);
            if (existingMember == null)
            {
                return NotFound();
            }

            existingMember.Name = name;
            existingMember.Relationship = relationship;

            await _expenseService.UpdateFamilyMemberAsync(existingMember);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int memberId)
        {
            var member = await _expenseService.GetFamilyMemberAsync(memberId);
            if (member == null)
            {
                return NotFound();
            }

            member.IsActive = !member.IsActive;
            await _expenseService.UpdateFamilyMemberAsync(member);
            return RedirectToPage();
        }
    }
}
