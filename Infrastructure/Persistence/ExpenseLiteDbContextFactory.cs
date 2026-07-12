using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExpenseLite.Infrastructure.Persistence;

public sealed class ExpenseLiteDbContextFactory : IDesignTimeDbContextFactory<ExpenseLiteDbContext>
{
    public ExpenseLiteDbContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddUserSecrets<ExpenseLiteDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("ExpenseLite")
            ?? "Host=127.0.0.1;Port=5432;Database=expenselite_dev;Username=expenselite_app";

        var options = new DbContextOptionsBuilder<ExpenseLiteDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ExpenseLiteDbContext(options);
    }
}
