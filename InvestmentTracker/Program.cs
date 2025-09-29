using InvestmentTracker.Data;
using InvestmentTracker.Infrastructure;
using InvestmentTracker.Services;
using Microsoft.EntityFrameworkCore;
using InvestmentTracker.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Load .env file in Development environment
if (builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.Load();
}

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

// Configure Data Protection for persistent keys in production
var keyDirectory = builder.Configuration["DataProtection:KeyDirectory"] ?? Environment.GetEnvironmentVariable("DATAPROTECTION__KEY_DIRECTORY") ?? "/keys";
if (!builder.Environment.IsDevelopment())
{
    // Ensure the directory exists and is writable; fail startup if not.
    try
    {
        Directory.CreateDirectory(keyDirectory);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Unable to create DataProtection key directory '{keyDirectory}': {ex.Message}", ex);
    }

    // Verify we can write to the directory (fail-closed)
    var testFile = Path.Combine(keyDirectory, $".dp-write-test-{Guid.NewGuid():N}");
    try
    {
        File.WriteAllText(testFile, "test");
        File.Delete(testFile);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"DataProtection key directory '{keyDirectory}' is not writable by the process. Ensure the directory is mounted and writable by the application user. Error: {ex.Message}", ex);
    }

    // Allow application name to be configured via appsettings (DataProtection:ApplicationName)
    var dataProtectionAppName = builder.Configuration["DataProtection:ApplicationName"] ?? Environment.GetEnvironmentVariable("DATAPROTECTION__APPLICATION_NAME") ?? "FinApp";

    builder.Services.AddDataProtection()
        .SetApplicationName(dataProtectionAppName)
        .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
}

var app = builder.Build();

// Log DataProtection status at startup for easier diagnostics
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
var dpAppNameConfigured = builder.Configuration["DataProtection:ApplicationName"] ?? Environment.GetEnvironmentVariable("DATAPROTECTION__APPLICATION_NAME") ?? "FinApp";
startupLogger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);
startupLogger.LogInformation("DataProtection configured application name: {DPAppName}", dpAppNameConfigured);
startupLogger.LogInformation("DataProtection key directory: {KeyDir}", keyDirectory);
if (!app.Environment.IsDevelopment())
{
    startupLogger.LogWarning("DataProtection key ring is persisted to '{KeyDir}' and is stored unencrypted. Ensure the volume is permissioned and backed up.", keyDirectory);
}

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

    // Optional automatic backfill of provider names from existing investments.
    // Controlled by configuration: BackfillProvidersOnStartup (bool, default true).
    try
    {
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await StartupBackfillHelper.BackfillProvidersAsync(db, config, logger);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Provider backfill failed during startup. Proceeding without blocking application startup.");
    }
}

// Map API endpoints
app.MapInvestmentApi();
app.MapExpenseApi();

app.Run();