using InvestmentTracker.Data;
using InvestmentTracker.Models;
using InvestmentTracker.Infrastructure;
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
app.MapGet("/", () => Results.Redirect("/Investments"));

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

api.MapGet("/investments", async (AppDbContext db) =>
    await db.Investments
    .Select(i => new { i.Id, i.Name, i.Provider, i.Type, i.Currency, i.ChargeAmount })
        .ToListAsync());

api.MapGet("/investments/{id:int}", async (int id, AppDbContext db) =>
    await db.Investments.Include(i => i.Values).Include(i => i.Schedules).Include(i => i.OneTimeContributions).FirstOrDefaultAsync(i => i.Id == id)
        is { } inv
        ? Results.Ok(inv)
        : Results.NotFound());

api.MapGet("/investments/{id:int}/contributions", async (int id, AppDbContext db) =>
{
    var list = await db.OneTimeContributions
        .Where(c => c.InvestmentId == id)
        .OrderBy(c => c.Date)
        .Select(c => new { c.Id, c.Date, c.Amount })
        .ToListAsync();
    return Results.Ok(list);
});

api.MapPost("/investments/{id:int}/contributions", async (int id, OneTimeContribution input, AppDbContext db) =>
{
    if (id != input.InvestmentId) return Results.BadRequest("Mismatched InvestmentId");
    db.OneTimeContributions.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/api/investments/{id}/contributions/{input.Id}", new { input.Id });
});

api.MapDelete("/investments/{id:int}/contributions/{contributionId:int}", async (int id, int contributionId, AppDbContext db) =>
{
    var c = await db.OneTimeContributions.FirstOrDefaultAsync(x => x.Id == contributionId && x.InvestmentId == id);
    if (c is null) return Results.NotFound();
    db.OneTimeContributions.Remove(c);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

api.MapPost("/investments", async (Investment input, AppDbContext db) =>
{
    db.Investments.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/api/investments/{input.Id}", new { input.Id });
});

api.MapPut("/investments/{id:int}", async (int id, Investment update, AppDbContext db) =>
{
    var existing = await db.Investments.FindAsync(id);
    if (existing is null) return Results.NotFound();
    existing.Name = update.Name;
    existing.Provider = update.Provider;
    existing.Type = update.Type;
    existing.Currency = update.Currency;
    existing.ChargeAmount = update.ChargeAmount;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

api.MapDelete("/investments/{id:int}", async (int id, AppDbContext db) =>
{
    var existing = await db.Investments.FindAsync(id);
    if (existing is null) return Results.NotFound();
    db.Remove(existing);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

api.MapGet("/investments/{id:int}/values", async (int id, AppDbContext db) =>
{
    var values = await db.InvestmentValues
        .Where(v => v.InvestmentId == id)
        .OrderBy(v => v.AsOf)
        .Select(v => new { v.Id, v.AsOf, v.Value })
        .ToListAsync();
    return Results.Ok(values);
});

api.MapPost("/investments/{id:int}/values", async (int id, InvestmentValue input, AppDbContext db) =>
{
    if (id != input.InvestmentId) return Results.BadRequest("Mismatched InvestmentId");
    db.InvestmentValues.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/api/investments/{id}/values/{input.Id}", new { input.Id });
});

// Contribution Schedules APIs
api.MapGet("/investments/{id:int}/schedules", async (int id, AppDbContext db) =>
{
    var list = await db.ContributionSchedules
        .Where(s => s.InvestmentId == id)
        .OrderBy(s => s.StartDate)
        .Select(s => new { s.Id, s.StartDate, s.EndDate, s.Amount, s.Frequency, s.DayOfMonth })
        .ToListAsync();
    return Results.Ok(list);
});

api.MapPost("/investments/{id:int}/schedules", async (int id, ContributionSchedule input, AppDbContext db) =>
{
    if (id != input.InvestmentId) return Results.BadRequest("Mismatched InvestmentId");
    // basic overlap check
    var newStart = input.StartDate.Date;
    var newEnd = (input.EndDate?.Date) ?? DateTime.MaxValue.Date;
    var overlaps = await db.ContributionSchedules.AnyAsync(s => s.InvestmentId == id &&
        newStart <= (s.EndDate == null ? DateTime.MaxValue.Date : s.EndDate.Value.Date) && s.StartDate.Date <= newEnd);
    if (overlaps) return Results.BadRequest("Overlaps existing schedule.");
    input.Frequency = ContributionFrequency.Monthly;
    if (input.DayOfMonth is null || input.DayOfMonth < 1 || input.DayOfMonth > 31)
        input.DayOfMonth = input.StartDate.Day;
    db.ContributionSchedules.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/api/investments/{id}/schedules/{input.Id}", new { input.Id });
});

api.MapDelete("/investments/{id:int}/schedules/{scheduleId:int}", async (int id, int scheduleId, AppDbContext db) =>
{
    var sched = await db.ContributionSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId && s.InvestmentId == id);
    if (sched is null) return Results.NotFound();
    db.ContributionSchedules.Remove(sched);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();