using ExpenseLite.Domain.ExpenseReports;
using ExpenseLite.Domain.CashAdvances;
using ExpenseLite.Domain.Projects;
using ExpenseLite.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseLite.Infrastructure.Persistence;

public sealed class ExpenseLiteDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ExpenseLiteDbContext(DbContextOptions<ExpenseLiteDbContext> options)
        : base(options)
    {
    }

    public DbSet<ExpenseReport> ExpenseReports => Set<ExpenseReport>();

    public DbSet<CashAdvance> CashAdvances => Set<CashAdvance>();

    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 先讓 Identity 建好它的預設模型，再套用本專案的設定。
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExpenseLiteDbContext).Assembly);

        IdentityModelConfiguration.Apply(modelBuilder);
    }
}
