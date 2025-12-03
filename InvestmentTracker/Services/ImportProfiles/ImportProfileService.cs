using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvestmentTracker.Data;
using InvestmentTracker.Models;
using InvestmentTracker.Models.ImportProfiles;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Services.ImportProfiles
{
    public class ImportProfileService : IBankImportProfileProvider
    {
        private readonly AppDbContext _context;

        public ImportProfileService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<BankImportProfileSummary>> GetSummariesAsync()
        {
            var profiles = await _context.ImportProfiles
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return profiles.Select(p => 
            {
                var summary = p.ProfileData.ToSummary();
                summary.Id = p.Id.ToString(); // Map int ID to string
                summary.DisplayName = p.Name; // Override with DB Name
                summary.Description = p.Description;
                return summary;
            }).ToList();
        }

        public async Task<BankImportProfile?> GetProfileAsync(string id)
        {
            if (!int.TryParse(id, out var intId)) return null;

            var profile = await _context.ImportProfiles
                .FirstOrDefaultAsync(p => p.Id == intId && p.IsActive);

            if (profile == null) return null;

            var result = profile.ProfileData;
            result.Id = profile.Id.ToString();
            result.Metadata.DisplayName = profile.Name;
            result.Metadata.Description = profile.Description;
            
            return result;
        }

        public async Task<IReadOnlyList<BankImportProfile>> GetAllProfilesAsync()
        {
            var profiles = await _context.ImportProfiles
                .Where(p => p.IsActive)
                .ToListAsync();

            return profiles.Select(p => 
            {
                var result = p.ProfileData;
                result.Id = p.Id.ToString();
                result.Metadata.DisplayName = p.Name;
                result.Metadata.Description = p.Description;
                return result;
            }).ToList();
        }

        // Management Methods

        public async Task<IReadOnlyList<ImportProfile>> GetAllEntitiesAsync()
        {
            return await _context.ImportProfiles
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<ImportProfile?> GetEntityAsync(int id)
        {
            return await _context.ImportProfiles
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        }

        public async Task<ImportProfile> CreateProfileAsync(ImportProfile profile)
        {
            _context.ImportProfiles.Add(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task<ImportProfile?> UpdateProfileAsync(int id, BankImportProfile profileData, string name, string description)
        {
            var profile = await _context.ImportProfiles.FindAsync(id);
            if (profile == null || !profile.IsActive) return null;

            profile.Name = name;
            profile.Description = description;
            profile.ProfileData = profileData;
            profile.UpdatedAt = DateTime.UtcNow;

            // Force EF Core to recognize the JSON column change
            _context.Entry(profile).Property(p => p.ProfileData).IsModified = true;

            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task<bool> DeleteProfileAsync(int id)
        {
            var profile = await _context.ImportProfiles.FindAsync(id);
            if (profile == null) return false;

            profile.IsActive = false;
            profile.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
