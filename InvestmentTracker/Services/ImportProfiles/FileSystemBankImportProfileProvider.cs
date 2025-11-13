using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvestmentTracker.Models.ImportProfiles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace InvestmentTracker.Services.ImportProfiles
{
    public class FileSystemBankImportProfileProvider : IBankImportProfileProvider
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileSystemBankImportProfileProvider> _logger;
        private readonly string _profileDirectory;
        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
        private IReadOnlyDictionary<string, BankImportProfile>? _profiles;
        private DateTime _lastLoadedUtc;

        public FileSystemBankImportProfileProvider(
            IWebHostEnvironment environment,
            ILogger<FileSystemBankImportProfileProvider> logger)
        {
            _environment = environment;
            _logger = logger;
            _profileDirectory = Path.Combine(_environment.ContentRootPath, "ImportProfiles");
        }

        public async Task<IReadOnlyList<BankImportProfileSummary>> GetSummariesAsync()
        {
            var profiles = await LoadProfilesAsync();
            return profiles.Values
                .Select(p => p.ToSummary())
                .OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<BankImportProfile?> GetProfileAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var profiles = await LoadProfilesAsync();
            profiles.TryGetValue(id, out var profile);
            return profile;
        }

        public async Task<IReadOnlyList<BankImportProfile>> GetAllProfilesAsync()
        {
            var profiles = await LoadProfilesAsync();
            return profiles.Values.ToList();
        }

        private async Task<IReadOnlyDictionary<string, BankImportProfile>> LoadProfilesAsync()
        {
            if (_profiles != null)
            {
                return _profiles;
            }

            await _loadSemaphore.WaitAsync();
            try
            {
                if (_profiles != null)
                {
                    return _profiles;
                }

                if (!Directory.Exists(_profileDirectory))
                {
                    _logger.LogWarning("Import profile directory '{ProfileDirectory}' not found. No bank profiles will be available.", _profileDirectory);
                    _profiles = new Dictionary<string, BankImportProfile>(StringComparer.OrdinalIgnoreCase);
                    return _profiles;
                }

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var profiles = new ConcurrentDictionary<string, BankImportProfile>(StringComparer.OrdinalIgnoreCase);
                var files = Directory.EnumerateFiles(_profileDirectory, "*.yaml", SearchOption.TopDirectoryOnly)
                    .Concat(Directory.EnumerateFiles(_profileDirectory, "*.yml", SearchOption.TopDirectoryOnly));

                foreach (var file in files)
                {
                    try
                    {
                        using var reader = File.OpenText(file);
                        var profile = deserializer.Deserialize<BankImportProfile>(reader);
                        if (profile == null || string.IsNullOrWhiteSpace(profile.Id))
                        {
                            _logger.LogWarning("Skipping import profile '{File}' because it has no id.", file);
                            continue;
                        }

                        NormalizeProfile(profile);

                        if (!profiles.TryAdd(profile.Id, profile))
                        {
                            _logger.LogWarning("Duplicate import profile id '{ProfileId}' detected in file '{File}'. Existing definition will be kept.", profile.Id, file);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load bank import profile from '{File}'.", file);
                    }
                }

                _profiles = profiles;
                _lastLoadedUtc = DateTime.UtcNow;
                _logger.LogInformation("Loaded {Count} bank import profile(s) from {Directory}.", _profiles.Count, _profileDirectory);
                return _profiles;
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        private static void NormalizeProfile(BankImportProfile profile)
        {
            profile.Metadata ??= new BankImportProfileMetadata();
            profile.Parser ??= new BankImportProfileParser();
            profile.Columns ??= new List<BankImportProfileColumn>();
            profile.Rules ??= new BankImportProfileRules();
            profile.ExpenseFieldMappings ??= new List<ExpenseFieldMapping>();

            profile.Metadata.DisplayName = profile.Metadata.DisplayName?.Trim() ?? profile.Id;
            profile.Metadata.Description = profile.Metadata.Description?.Trim() ?? string.Empty;

            foreach (var column in profile.Columns)
            {
                column.Header = column.Header?.Trim() ?? string.Empty;
                column.Target = column.Target?.Trim() ?? string.Empty;
                column.Transform = column.Transform?.Trim();
                column.Notes = column.Notes?.Trim();
            }

            profile.Notes = profile.Notes?.Select(n => n.Trim()).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();

            foreach (var mapping in profile.ExpenseFieldMappings)
            {
                mapping.Field = mapping.Field?.Trim() ?? string.Empty;
                mapping.SourceHeader = mapping.SourceHeader?.Trim();
                mapping.SourceHeaders = mapping.SourceHeaders?
                    .Select(header => header?.Trim())
                    .Where(header => !string.IsNullOrWhiteSpace(header))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? new List<string>();
                if (!string.IsNullOrWhiteSpace(mapping.SourceHeader))
                {
                    var normalizedLegacy = mapping.SourceHeader.Trim();
                    if (!mapping.SourceHeaders.Any(header => string.Equals(header, normalizedLegacy, StringComparison.OrdinalIgnoreCase)))
                    {
                        mapping.SourceHeaders.Insert(0, normalizedLegacy);
                    }
                    mapping.SourceHeader = null;
                }
                mapping.Target = mapping.Target?.Trim();
                mapping.Fallback = mapping.Fallback?.Trim();
                mapping.Notes = mapping.Notes?.Trim();
            }
        }
    }
}
