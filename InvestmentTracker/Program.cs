using InvestmentTracker.Data;
using InvestmentTracker.Models;
using InvestmentTracker.Infrastructure;
using InvestmentTracker.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddRazorPages(options => { })
    .AddMvcOptions(o =>
    {
        o.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider());
    });
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? "Data Source=investmenttracker.db";
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IInvestmentService, InvestmentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// Redirect root to Investments
app.MapGet("/", () => Results.Redirect("/Index"));

// Fallback for 404s
app.MapFallbackToPage("/NotFound");

// Apply EF Core migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Minimal API endpoints for future extensibility
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

app.Run();


app.Run();