using InvestmentTracker.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Services;

public static class StartupBackfillHelper
{
    public static async Task BackfillProvidersAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        var doBackfill = config.GetValue<bool?>("BackfillProvidersOnStartup") ?? true;
        if (!doBackfill)
        {
            logger.LogInformation("Provider backfill on startup is disabled by configuration.");
            return;
        }

        logger.LogInformation("Starting provider backfill from Investments table.");

        var sql = @"
INSERT INTO ""InvestmentProviders"" (""Name"")
SELECT DISTINCT trim(""Provider"")
FROM ""Investments""
WHERE ""Provider"" IS NOT NULL AND trim(""Provider"") <> ''
ON CONFLICT (""Name"") DO NOTHING;";

        var rows = await db.Database.ExecuteSqlRawAsync(sql);
        logger.LogInformation("Provider backfill completed (rows affected: {Rows}).", rows);
    }
}
