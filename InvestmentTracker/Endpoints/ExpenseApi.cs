using InvestmentTracker.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentTracker.Endpoints;

public static class ExpenseApi
{
    public static void MapExpenseApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/expenses/trends", async (int monthsBack, IExpenseService service) =>
        {
            if (monthsBack <= 0 || monthsBack > 12)
            {
                return Results.BadRequest(new { error = "Months back must be between 1 and 12" });
            }

            var trends = await service.GetMonthlyExpenseTrendsAsync(monthsBack);
            return Results.Ok(trends);
        });
    }
}