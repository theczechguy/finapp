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
    }
}
