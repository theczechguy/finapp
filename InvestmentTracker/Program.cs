using InvestmentTracker.Data;
using InvestmentTracker.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? "Data Source=investmenttracker.db";
    options.UseSqlite(connectionString);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// Ensure database exists and apply migrations if any (Create if none)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Minimal API endpoints for future extensibility
var api = app.MapGroup("/api");

api.MapGet("/investments", async (AppDbContext db) =>
    await db.Investments
        .Select(i => new { i.Id, i.Name, i.Type, i.RecurringAmount })
        .ToListAsync());

api.MapGet("/investments/{id:int}", async (int id, AppDbContext db) =>
    await db.Investments.Include(i => i.Values).FirstOrDefaultAsync(i => i.Id == id)
        is { } inv
        ? Results.Ok(inv)
        : Results.NotFound());

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
    existing.Type = update.Type;
    existing.RecurringAmount = update.RecurringAmount;
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

app.Run();