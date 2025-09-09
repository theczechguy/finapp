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

// Configure database - PostgreSQL only
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("PostgreSQL connection string is required.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Configure PostgreSQL to handle DateTime properly
        npgsqlOptions.EnableRetryOnFailure();
    });
    
    // Configure DateTime handling for PostgreSQL
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});

builder.Services.AddScoped<IInvestmentService, InvestmentService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<CsvImportService>();

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