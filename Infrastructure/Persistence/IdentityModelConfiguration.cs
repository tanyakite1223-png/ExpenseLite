using System.Text;
using ExpenseLite.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseLite.Infrastructure.Persistence;

/// <summary>
/// ASP.NET Core Identity 預設會建 AspNetUsers 這類 PascalCase 表名，
/// 本專案其他資料表都是 snake_case，這裡統一改名，避免資料庫出現兩套命名慣例。
/// </summary>
internal static class IdentityModelConfiguration
{
    private static readonly Dictionary<Type, string> TableNames = new()
    {
        [typeof(ApplicationUser)] = "users",
        [typeof(IdentityRole<Guid>)] = "roles",
        [typeof(IdentityUserRole<Guid>)] = "user_roles",
        [typeof(IdentityUserClaim<Guid>)] = "user_claims",
        [typeof(IdentityUserLogin<Guid>)] = "user_logins",
        [typeof(IdentityUserToken<Guid>)] = "user_tokens",
        [typeof(IdentityRoleClaim<Guid>)] = "role_claims"
    };

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>(builder =>
        {
            builder.Property(x => x.DisplayName)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.IsProtected)
                .IsRequired();

            builder.Property(x => x.LastSignedInAt);
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!TableNames.TryGetValue(entityType.ClrType, out var tableName))
            {
                continue;
            }

            entityType.SetTableName(tableName);

            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    private static string ToSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(name[i]));
        }

        return builder.ToString();
    }
}
