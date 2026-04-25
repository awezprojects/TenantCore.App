using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TenantCore.Infrastructure.Persistence;

public class ClinicDbContextFactory : IDesignTimeDbContextFactory<ClinicDbContext>
{
    public ClinicDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ClinicDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.\\SQLEXPRESS;Database=TenantClinicDb;Trusted_Connection=True;TrustServerCertificate=True;",
            b => b.MigrationsAssembly(typeof(ClinicDbContext).Assembly.FullName)
                  .MigrationsHistoryTable("__EFMigrationsHistory", "clinic"));
        return new ClinicDbContext(optionsBuilder.Options);
    }
}
