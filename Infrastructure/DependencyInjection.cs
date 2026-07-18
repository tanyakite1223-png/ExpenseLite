using ExpenseLite.Application.CashAdvances;
using ExpenseLite.Application.ExpenseReports;
using ExpenseLite.Application.Projects;
using ExpenseLite.Infrastructure.CashAdvances;
using ExpenseLite.Infrastructure.ExpenseReports;
using ExpenseLite.Infrastructure.Persistence;
using ExpenseLite.Infrastructure.Projects;
using Microsoft.EntityFrameworkCore;

namespace ExpenseLite.Infrastructure;

public static class DependencyInjection
{
    private const string MissingConnectionStringMessage =
        "找不到 ConnectionStrings:ExpenseLite。請用 user secrets 或環境變數設定 PostgreSQL 連線字串。";

    public static IServiceCollection AddExpenseLiteInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ExpenseLite");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(MissingConnectionStringMessage);
        }

        services.AddDbContext<ExpenseLiteDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IExpenseReportRepository, EfExpenseReportRepository>();
        services.AddScoped<ICashAdvanceRepository, EfCashAdvanceRepository>();
        services.AddScoped<IProjectRepository, EfProjectRepository>();

        return services;
    }
}
