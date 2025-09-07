using System.Globalization;

namespace InvestmentTracker.Models;

public static class CurrencyExtensions
{
    public static string ToCultureCode(this Currency currency)
    {
        return currency switch
        {
            Currency.CZK => "cs-CZ",
            Currency.EUR => "de-DE", // Using Germany for Euro representation
            Currency.USD => "en-US",
            _ => CultureInfo.CurrentCulture.Name
        };
    }
}
