using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace InvestmentTracker.Infrastructure;

public class InvariantDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        var value = valueProviderResult.FirstValue;
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.CompletedTask;
        }

        // Normalize common decimal separators
        value = value.Trim().Replace(" ", string.Empty).Replace(',', '.');

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "The field must be a number.");
        }

        return Task.CompletedTask;
    }
}
