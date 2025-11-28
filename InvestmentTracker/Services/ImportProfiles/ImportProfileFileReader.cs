using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InvestmentTracker.Models.ImportProfiles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace InvestmentTracker.Services.ImportProfiles
{
    /// <summary>
    /// Helper class to read legacy YAML import profiles for initial database seeding.
    /// </summary>
    public class ImportProfileFileReader
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImportProfileFileReader> _logger;
        private readonly string _profileDirectory;

        public ImportProfileFileReader(
            IWebHostEnvironment environment,
            ILogger<ImportProfileFileReader> logger)
        {
            _environment = environment;
            _logger = logger;
            _profileDirectory = Path.Combine(_environment.ContentRootPath, "ImportProfiles");
        }

        public IEnumerable<BankImportProfile> ReadAllProfiles()
        {
            if (!Directory.Exists(_profileDirectory))
            {
                _logger.LogWarning("Import profile directory '{ProfileDirectory}' not found.", _profileDirectory);
                yield break;
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var files = Directory.EnumerateFiles(_profileDirectory, "*.yaml", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateFiles(_profileDirectory, "*.yml", SearchOption.TopDirectoryOnly));

            foreach (var file in files)
            {
                BankImportProfile? profile = null;
                try
                {
                    using var reader = File.OpenText(file);
                    profile = deserializer.Deserialize<BankImportProfile>(reader);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load bank import profile from '{File}'.", file);
                    continue;
                }

                if (profile == null || string.IsNullOrWhiteSpace(profile.Id))
                {
                    _logger.LogWarning("Skipping import profile '{File}' because it has no id.", file);
                    continue;
                }

                NormalizeProfile(profile);
                yield return profile;
            }
        }

        private static void NormalizeProfile(BankImportProfile profile)
        {
            profile.Metadata ??= new BankImportProfileMetadata();
            profile.Parser ??= new BankImportProfileParser();
            profile.Columns ??= new List<BankImportProfileColumn>();
            profile.ExpenseFieldMappings ??= new List<ExpenseFieldMapping>();

            profile.Metadata.DisplayName = profile.Metadata.DisplayName?.Trim() ?? profile.Id;
            profile.Metadata.Description = profile.Metadata.Description?.Trim() ?? string.Empty;

            foreach (var column in profile.Columns)
            {
                column.Header = column.Header?.Trim() ?? string.Empty;
                column.Target = column.Target?.Trim() ?? string.Empty;
                column.Transform = column.Transform?.Trim();
            }

            foreach (var mapping in profile.ExpenseFieldMappings)
            {
                mapping.Field = mapping.Field?.Trim() ?? string.Empty;
                mapping.SourceHeader = mapping.SourceHeader?.Trim();
                var normalizedHeaders = new List<string>();
                if (mapping.SourceHeaders != null && mapping.SourceHeaders.Count > 0)
                {
                    var seenHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var header in mapping.SourceHeaders)
                    {
                        if (string.IsNullOrWhiteSpace(header))
                        {
                            continue;
                        }

                        var trimmed = header.Trim();
                        if (seenHeaders.Add(trimmed))
                        {
                            normalizedHeaders.Add(trimmed);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(mapping.SourceHeader))
                {
                    var normalizedLegacy = mapping.SourceHeader.Trim();
                    if (!normalizedHeaders.Any(header => string.Equals(header, normalizedLegacy, StringComparison.OrdinalIgnoreCase)))
                    {
                        normalizedHeaders.Insert(0, normalizedLegacy);
                    }
                    mapping.SourceHeader = null;
                }

                mapping.SourceHeaders = normalizedHeaders;
                mapping.Target = mapping.Target?.Trim();
                mapping.Fallback = mapping.Fallback?.Trim();
            }
        }
    }
}
