using ExpenseLite.Application.Identity;
using ExpenseLite.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExpenseLite.Infrastructure.Identity;

/// <summary>
/// 建立角色與初始帳號。角色一定會建；初始帳號只有在設定了 Identity:SeedPassword 時才建，
/// 避免把預設密碼寫死在程式碼或 appsettings 裡進版控。
/// </summary>
public static class IdentitySeeder
{
    private const string SeedPasswordKey = "Identity:SeedPassword";

    private static readonly SeedUser[] SeedUsers =
    [
        new("manager", "manager@expenselite.local", "王主管", ExpenseLiteRoles.Manager),
        new("employee", "employee@expenselite.local", "陳員工", ExpenseLiteRoles.Employee)
    ];

    public static async Task SeedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var logger = provider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(IdentitySeeder));

        var dbContext = provider.GetRequiredService<ExpenseLiteDbContext>();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pendingMigrations.Any())
        {
            logger.LogWarning(
                "尚有未套用的 migration，略過 Identity 種子資料。請先執行 dotnet ef database update。");
            return;
        }

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in ExpenseLiteRoles.All)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            if (!result.Succeeded)
            {
                logger.LogError("建立角色 {Role} 失敗：{Errors}", role, Describe(result));
                continue;
            }

            logger.LogInformation("已建立角色 {Role}。", role);
        }

        var configuration = provider.GetRequiredService<IConfiguration>();
        var seedPassword = configuration[SeedPasswordKey];
        if (string.IsNullOrWhiteSpace(seedPassword))
        {
            logger.LogWarning(
                "未設定 {Key}，略過建立初始帳號。設定方式：dotnet user-secrets set \"{Key}\" \"<密碼>\"。",
                SeedPasswordKey,
                SeedPasswordKey);
            return;
        }

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var seedUser in SeedUsers)
        {
            await EnsureUserAsync(userManager, seedUser, seedPassword, logger);
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        SeedUser seedUser,
        string password,
        ILogger logger)
    {
        var existing = await userManager.FindByEmailAsync(seedUser.Email);
        if (existing is not null)
        {
            // 早期版本的種子資料把完整 email 當帳號，這裡順手校正成帳號前綴，
            // 讓已經建好的帳號不用砍掉重建。
            if (!string.Equals(existing.UserName, seedUser.UserName, StringComparison.Ordinal))
            {
                var renameResult = await userManager.SetUserNameAsync(existing, seedUser.UserName);
                if (renameResult.Succeeded)
                {
                    logger.LogInformation(
                        "已將帳號 {Email} 的登入帳號改為 {UserName}。",
                        seedUser.Email,
                        seedUser.UserName);
                }
                else
                {
                    logger.LogError(
                        "更新 {Email} 的登入帳號失敗：{Errors}",
                        seedUser.Email,
                        Describe(renameResult));
                }
            }

            return;
        }

        var user = new ApplicationUser
        {
            UserName = seedUser.UserName,
            Email = seedUser.Email,
            EmailConfirmed = true,
            DisplayName = seedUser.DisplayName,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            logger.LogError(
                "建立初始帳號 {Email} 失敗：{Errors}",
                seedUser.Email,
                Describe(createResult));
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(user, seedUser.Role);
        if (!roleResult.Succeeded)
        {
            logger.LogError(
                "指派角色給 {Email} 失敗：{Errors}",
                seedUser.Email,
                Describe(roleResult));
            return;
        }

        logger.LogInformation(
            "已建立初始帳號 {UserName}（{Role}）。",
            seedUser.UserName,
            seedUser.Role);
    }

    private static string Describe(IdentityResult result)
        => string.Join("；", result.Errors.Select(x => x.Description));

    private sealed record SeedUser(string UserName, string Email, string DisplayName, string Role);
}
