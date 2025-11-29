using System;
using System.Linq;
using System.Threading.Tasks;
using InvestmentTracker.Data;
using InvestmentTracker.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentTracker.Services.ImportProfiles
{
    public class ImportProfileSeeder
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImportProfileSeeder> _logger;
        private readonly ILogger<ImportProfileFileReader> _fsLogger;

        public ImportProfileSeeder(
            AppDbContext context,
            IWebHostEnvironment environment,
            ILogger<ImportProfileSeeder> logger,
            ILogger<ImportProfileFileReader> fsLogger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _fsLogger = fsLogger;
        }

        public async Task SeedAsync()
        {
            if (await _context.ImportProfiles.AnyAsync())
            {
                return;
            }

            _logger.LogInformation("ImportProfiles table is empty. Seeding from file system...");

            var fsProvider = new ImportProfileFileReader(_environment, _fsLogger);
            var fileProfiles = fsProvider.ReadAllProfiles().ToList();

            if (!fileProfiles.Any())
            {
                _logger.LogWarning("No file-based profiles found to seed.");
                return;
            }

            foreach (var fileProfile in fileProfiles)
            {
                var entity = new ImportProfile
                {
                    Name = fileProfile.Metadata.DisplayName,
                    Description = fileProfile.Metadata.Description,
                    ProfileData = fileProfile,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ImportProfiles.Add(entity);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} import profiles from file system.", fileProfiles.Count);
        }
    }
}
