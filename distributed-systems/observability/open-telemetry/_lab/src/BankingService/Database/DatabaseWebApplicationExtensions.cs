using Microsoft.EntityFrameworkCore;

namespace BankingService.Database;

public static class DatabaseWebApplicationExtensions
{
    public static async Task<WebApplication> MigrateDatabase(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        await dbContext.Database.MigrateAsync(cts.Token);
        return app;
    }
}
