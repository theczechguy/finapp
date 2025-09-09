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
            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 4)
            {
                result.Errors.Add("CSV file must have at least 4 lines (header, empty, currencies, data)");
                return result;
            }

            // Parse dates from the first line (skip the "Datum" header)
            var dateLine = lines[0];
            var dates = ParseDates(dateLine);
            if (!dates.Any())
            {
                result.Errors.Add("No valid dates found in CSV");
                return result;
            }

            // Skip the empty line (line 1) and currency line (line 2)
            // Process investment lines starting from line 3
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
            }
        }

        return dates;
    }

    private async Task<ImportResult> ProcessInvestmentLineAsync(string line, List<DateTime> dates)
    {
        var result = new ImportResult();
        var parts = line.Split(';');

        if (parts.Length < 2)
        {
            result.Errors.Add($"Invalid line format: {line}");
            return result;
        }

        var investmentName = parts[0].Trim();
        if (string.IsNullOrWhiteSpace(investmentName) || investmentName.Contains("CZK"))
        {
            // Skip total/summary lines
            return result;
        }

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
                    break; // Use the first non-empty value for this date
                }
            }
        }

        return result;
    }

    private (InvestmentCategory category, InvestmentType type, Currency currency) MapInvestmentDetails(string investmentName)
    {
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
        if (investmentName.Contains(" - "))
        {
            return investmentName.Split(" - ")[0].Trim();
        }
        return null;
    }

    private decimal? ParseValue(string valueStr)
    {
        if (string.IsNullOrWhiteSpace(valueStr)) return null;

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
            return value;
        }

        return null;
    }

    private async Task AddInvestmentValueAsync(int investmentId, DateTime date, decimal value)
    {
        // Check if value already exists for this date
        var existing = await _context.InvestmentValues
            .FirstOrDefaultAsync(v => v.InvestmentId == investmentId && v.AsOf.Date == date.Date);

        if (existing != null)
        {
            // Update existing value
            existing.Value = value;
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
        }

        await _context.SaveChangesAsync();
    }
}

public class ImportResult
{
    public bool Success { get; set; }
    public int InvestmentsProcessed { get; set; }
    public int ValuesProcessed { get; set; }
    public List<string> Errors { get; set; } = new();
}