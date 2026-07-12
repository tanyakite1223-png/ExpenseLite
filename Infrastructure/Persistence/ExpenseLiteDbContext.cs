using ExpenseLite.Domain.ExpenseReports;
using Microsoft.EntityFrameworkCore;

namespace ExpenseLite.Infrastructure.Persistence;

public sealed class ExpenseLiteDbContext : DbContext
{
    public ExpenseLiteDbContext(DbContextOptions<ExpenseLiteDbContext> options)
        : base(options)
    {
    }

    public DbSet<ExpenseReport> ExpenseReports => Set<ExpenseReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExpenseLiteDbContext).Assembly);
    }
}
