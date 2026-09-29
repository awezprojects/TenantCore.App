using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TenantCore.Infrastructure.Persistence;

public class ClinicDbContextFactory : IDesignTimeDbContextFactory<ClinicDbContext>
{
    public ClinicDbContext CreateDbContext(string[] args)
    {
        // dotnet ef sets the working directory to the --startup-project directory (TenantCore.Api),
        // so appsettings.json / appsettings.{env}.json are resolved relative to it. This keeps the
        // design-time connection string in sync with the runtime one instead of hardcoding it here.
        var basePath = Directory.GetCurrentDirectory();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var connectionString = ReadConnectionString(basePath, $"appsettings.{environment}.json")
            ?? ReadConnectionString(basePath, "appsettings.json")
            ?? throw new InvalidOperationException(
                $"Could not resolve ConnectionStrings:ClinicConnection from appsettings.json/appsettings.{environment}.json in '{basePath}'.");

        var optionsBuilder = new DbContextOptionsBuilder<ClinicDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            b => b.MigrationsAssembly(typeof(ClinicDbContext).Assembly.FullName)
                  .MigrationsHistoryTable("__EFMigrationsHistory", "clinic"));
        return new ClinicDbContext(optionsBuilder.Options);
    }

    private static string? ReadConnectionString(string basePath, string fileName)
    {
        var path = Path.Combine(basePath, fileName);
        if (!File.Exists(path))
            return null;

        using var stream = File.OpenRead(path);
        using var doc = JsonDocument.Parse(stream);
        if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
            cs.TryGetProperty("ClinicConnection", out var conn))
        {
            return conn.GetString();
        }
        return null;
    }
}
