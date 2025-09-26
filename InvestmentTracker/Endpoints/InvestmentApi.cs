using InvestmentTracker.Models;
using InvestmentTracker.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentTracker.Endpoints;

public static class InvestmentApi
{
    public static void MapInvestmentApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/investments", async (IInvestmentService service) =>
            Results.Ok(await service.GetAllInvestmentsAsync()));

        api.MapGet("/investments/{id:int}", async (int id, IInvestmentService service) =>
            await service.GetInvestmentAsync(id)
                is { } inv
                ? Results.Ok(inv)
                : Results.NotFound());

        api.MapGet("/investments/{id:int}/contributions", async (int id, IInvestmentService service) =>
        {
            var list = await service.GetOneTimeContributionsAsync(id);
            return Results.Ok(list);
        });

        api.MapPost("/investments/{id:int}/contributions", async (int id, OneTimeContribution input, IInvestmentService service) =>
        {
            if (id != input.InvestmentId) return Results.BadRequest("Mismatched InvestmentId");
            await service.AddOneTimeContributionAsync(id, input);
            return Results.Created($"/api/investments/{id}/contributions/{input.Id}", new { input.Id });
        });

        api.MapDelete("/investments/{id:int}/contributions/{contributionId:int}", async (int id, int contributionId, IInvestmentService service) =>
        {
            await service.DeleteOneTimeContributionAsync(id, contributionId);
            return Results.NoContent();
        });

        api.MapPost("/investments", async (Investment input, IInvestmentService service) =>
        {
            var investment = await service.AddInvestmentAsync(input);
            return Results.Created($"/api/investments/{investment.Id}", new { investment.Id });
        });

        api.MapPut("/investments/{id:int}", async (int id, Investment update, IInvestmentService service) =>
        {
            var success = await service.UpdateInvestmentAsync(id, update);
            return success ? Results.NoContent() : Results.NotFound();
        });

        api.MapDelete("/investments/{id:int}", async (int id, IInvestmentService service) =>
        {
            var success = await service.DeleteInvestmentAsync(id);
            return success ? Results.NoContent() : Results.NotFound();
        });

        api.MapGet("/investments/{id:int}/values", async (int id, IInvestmentService service) =>
        {
            var values = await service.GetInvestmentValuesAsync(id);
            return Results.Ok(values);
        });

        api.MapPost("/investments/{id:int}/values", async (int id, InvestmentValue input, IInvestmentService service) =>
        {
            if (id != input.InvestmentId) return Results.BadRequest("Mismatched InvestmentId");
            await service.AddInvestmentValueAsync(id, input);
            return Results.Created($"/api/investments/{id}/values/{input.Id}", new { input.Id });
        });

        // Contribution Schedules APIs
        api.MapGet("/investments/{id:int}/schedules", async (int id, IInvestmentService service) =>
        {
            var list = await service.GetContributionSchedulesAsync(id);
            return Results.Ok(list);
        });

        api.MapPost("/investments/{id:int}/schedules", async (int id, ContributionSchedule input, IInvestmentService service) =>
        {
            if (id != input.InvestmentId) return Results.BadRequest("Mismatched InvestmentId");
            
            var (schedule, error) = await service.AddContributionScheduleAsync(id, input);

            if (error is not null) return Results.BadRequest(error);
            
            return Results.Created($"/api/investments/{id}/schedules/{schedule!.Id}", new { schedule.Id });
        });

        api.MapDelete("/investments/{id:int}/schedules/{scheduleId:int}", async (int id, int scheduleId, IInvestmentService service) =>
        {
            var success = await service.DeleteContributionScheduleAsync(id, scheduleId);
            return success ? Results.NoContent() : Results.NotFound();
        });

        // CSV Import API
        api.MapPost("/investments/import-csv", async (HttpRequest request, CsvImportService importService) =>
        {
            try
            {
                // Read the uploaded file
                var form = await request.ReadFormAsync();
                var file = form.Files.GetFile("csvFile");

                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest(new { error = "No CSV file provided" });
                }

                // Read file content
                using var reader = new StreamReader(file.OpenReadStream());
                var csvContent = await reader.ReadToEndAsync();

                // Import the data
                var result = await importService.ImportInvestmentPortfolioAsync(csvContent);

                if (result.Success)
                {
                    return Results.Ok(new
                    {
                        message = "Import completed successfully",
                        investmentsProcessed = result.InvestmentsProcessed,
                        valuesProcessed = result.ValuesProcessed
                    });
                }
                else
                {
                    return Results.BadRequest(new
                    {
                        error = "Import completed with errors",
                        errors = result.Errors,
                        investmentsProcessed = result.InvestmentsProcessed,
                        valuesProcessed = result.ValuesProcessed
                    });
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Import failed: {ex.Message}");
            }
        });

        api.MapGet("/providers", async (string? query, IInvestmentService service) =>
        {
            var providers = await service.GetProvidersAsync(query);
            return Results.Ok(providers);
        });

        api.MapGet("/investments/{id:int}/series", async (int id, string? from, string? to, IInvestmentService service) =>
        {
            DateTime toDate = DateTime.Today;
            DateTime fromDate = DateTime.Today.AddMonths(-12);

            if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var f)) fromDate = f.Date;
            if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var t)) toDate = t.Date;

            var series = await service.GetInvestmentSeriesAsync(id, fromDate, toDate);
            return Results.Ok(series.Select(p => new { asOf = p.AsOf.ToString("yyyy-MM-dd"), value = p.Value, invested = p.Invested }));
        });
    }
}
