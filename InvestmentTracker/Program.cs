using InvestmentTracker.Data;
using InvestmentTracker.Infrastructure;
using InvestmentTracker.Services;
using Microsoft.EntityFrameworkCore;
using InvestmentTracker.Endpoints;

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

// Map API endpoints
app.MapInvestmentApi();

app.Run();