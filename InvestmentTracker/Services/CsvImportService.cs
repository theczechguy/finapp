using System.Globalization;
using InvestmentTracker.Data;
using InvestmentTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Services;

public class CsvImportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CsvImportService> _logger;

    public CsvImportService(AppDbContext context, ILogger<CsvImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ImportResult> ImportInvestmentPortfolioAsync(string csvContent)
    {
        var result = new ImportResult();

        try
        {
            _logger.LogDebug("Starting CSV import process");

            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 4)
            {
                _logger.LogWarning("CSV file validation failed: insufficient lines ({LineCount})", lines.Length);
                result.Errors.Add("CSV file must have at least 4 lines (header, empty, currencies, data)");
                return result;
            }

            _logger.LogDebug("Parsing dates from CSV header");
            // Parse dates from the first line (skip the "Datum" header)
            var dateLine = lines[0];
            var dates = ParseDates(dateLine);
            if (!dates.Any())
            {
                _logger.LogWarning("No valid dates found in CSV header");
                result.Errors.Add("No valid dates found in CSV");
                return result;
            }

            _logger.LogInformation("Found {DateCount} dates in CSV: {DateRange}", dates.Count,
                dates.Any() ? $"{dates.Min():d} to {dates.Max():d}" : "none");

            // Skip the empty line (line 1) and currency line (line 2)
            // Process investment lines starting from line 3
            _logger.LogDebug("Processing investment lines starting from line 3");
            for (int i = 3; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";")) continue;

                var investmentResult = await ProcessInvestmentLineAsync(line, dates);
                result.InvestmentsProcessed += investmentResult.InvestmentsProcessed;
                result.ValuesProcessed += investmentResult.ValuesProcessed;
                result.Errors.AddRange(investmentResult.Errors);
            }

            result.Success = result.Errors.Count == 0;
            _logger.LogInformation("CSV import completed. Success: {Success}, Investments: {Investments}, Values: {Values}, Errors: {ErrorCount}",
                result.Success, result.InvestmentsProcessed, result.ValuesProcessed, result.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing CSV data");
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    private List<DateTime> ParseDates(string dateLine)
    {
        _logger.LogDebug("Parsing dates from line: {DateLine}", dateLine);
        var dates = new List<DateTime>();
        var parts = dateLine.Split(';');

        // Skip the first part which should be empty or "Investice"
        for (int i = 1; i < parts.Length; i++)
        {
            var dateStr = parts[i].Trim();
            if (string.IsNullOrWhiteSpace(dateStr) || dateStr == "Datum" || dateStr == "Investice") continue;

            // Try to parse European date format (DD.MM.YYYY)
            if (DateTime.TryParseExact(dateStr, "d.M.yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            {
                dates.Add(date);
                _logger.LogDebug("Parsed date: {DateStr} -> {Date}", dateStr, date.ToString("d"));
            }
            else
            {
                _logger.LogWarning("Failed to parse date: {DateStr}", dateStr);
            }
        }

        _logger.LogDebug("Parsed {DateCount} dates from CSV header", dates.Count);
        return dates;
    }

    private async Task<ImportResult> ProcessInvestmentLineAsync(string line, List<DateTime> dates)
    {
        var result = new ImportResult();

        try
        {
            _logger.LogDebug("Processing investment line: {Line}", line);
            var parts = line.Split(';');

            if (parts.Length < 2)
            {
                _logger.LogWarning("Invalid line format - insufficient parts: {Line}", line);
                result.Errors.Add($"Invalid line format: {line}");
                return result;
            }

            var investmentName = parts[0].Trim();
            if (string.IsNullOrWhiteSpace(investmentName) || investmentName.Contains("CZK"))
            {
                _logger.LogDebug("Skipping summary/total line: {InvestmentName}", investmentName);
                // Skip total/summary lines
                return result;
            }

            _logger.LogDebug("Processing investment: {InvestmentName}", investmentName);

            // Map investment to category and type
            var (category, type, currency) = MapInvestmentDetails(investmentName);

            // Create or get investment
            var investment = await GetOrCreateInvestmentAsync(investmentName, category, type, currency);
            result.InvestmentsProcessed++;

            // Process values - each date has 3 columns (CZK, USD, EUR)
            for (int dateIndex = 0; dateIndex < dates.Count; dateIndex++)
            {
                int valueIndex = 1 + (dateIndex * 3); // Skip investment name, then 3 columns per date

                if (valueIndex >= parts.Length)
                    break;

                // Check each currency column for this date
                for (int currencyOffset = 0; currencyOffset < 3; currencyOffset++)
                {
                    int columnIndex = valueIndex + currencyOffset;
                    if (columnIndex >= parts.Length)
                        break;

                    var valueStr = parts[columnIndex].Trim();
                    if (string.IsNullOrWhiteSpace(valueStr))
                        continue;

                    var value = ParseValue(valueStr);
                    if (value.HasValue)
                    {
                        await AddInvestmentValueAsync(investment.Id, dates[dateIndex], value.Value);
                        result.ValuesProcessed++;
                        _logger.LogDebug("Added value {Value} for investment {InvestmentName} on {Date}",
                            value.Value, investmentName, dates[dateIndex].ToString("d"));
                        break; // Use the first non-empty value for this date
                    }
                }
            }

            _logger.LogInformation("Processed investment {InvestmentName}: {ValuesProcessed} values",
                investmentName, result.ValuesProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing investment line: {Line}", line);
            result.Errors.Add($"Error processing line: {ex.Message}");
        }

        return result;
    }

    private (InvestmentCategory category, InvestmentType type, Currency currency) MapInvestmentDetails(string investmentName)
    {
        _logger.LogDebug("Mapping investment details for: {InvestmentName}", investmentName);
        var name = investmentName.ToLowerInvariant();

        // Determine category
        InvestmentCategory category = InvestmentCategory.Stocks; // default
        if (name.Contains("crypto") || name.Contains("binance") || name.Contains("anycoin"))
            category = InvestmentCategory.Crypto;
        else if (name.Contains("realitni") || name.Contains("real estate"))
            category = InvestmentCategory.RealEstate;
        else if (name.Contains("bond"))
            category = InvestmentCategory.Bonds;

        // Determine type (most are recurring investments)
        InvestmentType type = InvestmentType.Recurring;

        // Determine currency (default to CZK, check for others)
        Currency currency = Currency.CZK;
        if (name.Contains("binance") || name.Contains("usd"))
            currency = Currency.USD;
        else if (name.Contains("investbay") || name.Contains("eur"))
            currency = Currency.EUR;

        _logger.LogDebug("Mapped {InvestmentName} -> Category: {Category}, Type: {Type}, Currency: {Currency}",
            investmentName, category, type, currency);
        return (category, type, currency);
    }

    private async Task<Investment> GetOrCreateInvestmentAsync(string name, InvestmentCategory category,
        InvestmentType type, Currency currency)
    {
        var existing = await _context.Investments
            .FirstOrDefaultAsync(i => i.Name == name);

        if (existing != null)
            return existing;

        var investment = new Investment
        {
            Name = name,
            Category = category,
            Type = type,
            Currency = currency,
            Provider = ExtractProvider(name)
        };

        _context.Investments.Add(investment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new investment: {Name}", name);
        return investment;
    }

    private string? ExtractProvider(string investmentName)
    {
        _logger.LogDebug("Extracting provider from investment name: {InvestmentName}", investmentName);
        if (investmentName.Contains(" - "))
        {
            var provider = investmentName.Split(" - ")[0].Trim();
            _logger.LogDebug("Extracted provider: {Provider}", provider);
            return provider;
        }
        _logger.LogDebug("No provider found in investment name");
        return null;
    }

    private decimal? ParseValue(string valueStr)
    {
        _logger.LogDebug("Parsing value from string: {ValueStr}", valueStr);
        if (string.IsNullOrWhiteSpace(valueStr))
        {
            _logger.LogDebug("Value string is empty or whitespace");
            return null;
        }

        // Remove currency symbols and clean up
        var cleanValue = valueStr
            .Replace("CZK", "")
            .Replace("USD", "")
            .Replace("EUR", "")
            .Replace("€", "")
            .Replace(",", ".")
            .Trim();

        if (decimal.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            _logger.LogDebug("Successfully parsed value: {ValueStr} -> {Value}", valueStr, value);
            return value;
        }

        _logger.LogWarning("Failed to parse value: {ValueStr} (cleaned: {CleanValue})", valueStr, cleanValue);
        return null;
    }

    private async Task AddInvestmentValueAsync(int investmentId, DateTime date, decimal value)
    {
        _logger.LogDebug("Adding investment value - InvestmentId: {InvestmentId}, Date: {Date}, Value: {Value}",
            investmentId, date.ToString("d"), value);

        // Check if value already exists for this date
        var existing = await _context.InvestmentValues
            .FirstOrDefaultAsync(v => v.InvestmentId == investmentId && v.AsOf.Date == date.Date);

        if (existing != null)
        {
            // Update existing value
            var oldValue = existing.Value;
            existing.Value = value;
            _logger.LogInformation("Updated existing investment value for investment {InvestmentId} on {Date}: {OldValue} -> {NewValue}",
                investmentId, date.ToString("d"), oldValue, value);
        }
        else
        {
            // Create new value
            var investmentValue = new InvestmentValue
            {
                InvestmentId = investmentId,
                AsOf = date,
                Value = value
            };
            _context.InvestmentValues.Add(investmentValue);
            _logger.LogInformation("Created new investment value for investment {InvestmentId} on {Date}: {Value}",
                investmentId, date.ToString("d"), value);
        }

        await _context.SaveChangesAsync();
        _logger.LogDebug("Investment value operation completed for investment {InvestmentId}", investmentId);
    }
}

public class ImportResult
{
    public bool Success { get; set; }
    public int InvestmentsProcessed { get; set; }
    public int ValuesProcessed { get; set; }
    public List<string> Errors { get; set; } = new();
}