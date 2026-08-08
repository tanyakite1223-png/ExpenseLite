using ExpenseLite.Application.Identity;
using ExpenseLite.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExpenseLite.Infrastructure.Identity;

/// <summary>
/// 開機時確保角色存在，並在「完全沒有使用者」時建立第一個主管帳號。
///
/// 這是 bootstrap 而不是 seed：只負責讓一個空系統有辦法被登入一次，
/// 之後的帳號一律由主管在使用者管理頁建立。
/// 所以不建 demo 帳號，也不會在既有系統上補建任何東西——users 表只要有人就整段跳過。
///
/// 自助註冊拿掉之後（2026-08-08），這段是系統裡唯一「憑空生出帳號」的地方，
/// 也就是說沒有它、或沒設 <c>Identity:SeedPassword</c>，新環境會完全沒有人進得去。
/// </summary>
public static class IdentityBootstrapper
{
    private const string SeedPasswordKey = "Identity:SeedPassword";

    private const string BootstrapUserName = "Admin";
    private const string BootstrapEmail = "admin@expenselite.local";
    private const string BootstrapDisplayName = "管理者";

    public static async Task BootstrapAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var logger = provider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(IdentityBootstrapper));

        var dbContext = provider.GetRequiredService<ExpenseLiteDbContext>();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pendingMigrations.Any())
        {
            logger.LogWarning(
                "尚有未套用的 migration，略過 Identity bootstrap。請先執行 dotnet ef database update。");
            return;
        }

        await EnsureRolesAsync(provider, logger);

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.Users.AnyAsync(cancellationToken))
        {
            // 已經有人了，代表系統被用過（或被 bootstrap 過），不再插手。
            return;
        }

        var configuration = provider.GetRequiredService<IConfiguration>();
        var bootstrapPassword = configuration[SeedPasswordKey];
        if (string.IsNullOrWhiteSpace(bootstrapPassword))
        {
            logger.LogWarning(
                "系統還沒有任何使用者，但未設定 {Key}，無法建立第一個主管帳號。" +
                "設定方式：dotnet user-secrets set \"{Key}\" \"<密碼>\"。",
                SeedPasswordKey,
                SeedPasswordKey);
            return;
        }

        await CreateBootstrapManagerAsync(userManager, bootstrapPassword, logger);
    }

    private static async Task EnsureRolesAsync(IServiceProvider provider, ILogger logger)
    {
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
    }

    private static async Task CreateBootstrapManagerAsync(
        UserManager<ApplicationUser> userManager,
        string password,
        ILogger logger)
    {
        var user = new ApplicationUser
        {
            UserName = BootstrapUserName,
            Email = BootstrapEmail,
            EmailConfirmed = true,
            DisplayName = BootstrapDisplayName,
            Status = UserAccountStatus.Active,
            // 緊急存取帳號（break-glass）：不能被停用、也不能降成員工。
            // 這是全系統唯一會設這個旗標的地方。
            IsProtected = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            logger.LogError(
                "建立第一個主管帳號 {UserName} 失敗：{Errors}",
                BootstrapUserName,
                Describe(createResult));
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(user, ExpenseLiteRoles.Manager);
        if (!roleResult.Succeeded)
        {
            logger.LogError(
                "指派主管角色給 {UserName} 失敗：{Errors}",
                BootstrapUserName,
                Describe(roleResult));
            return;
        }

        logger.LogInformation(
            "系統原本沒有任何使用者，已建立第一個主管帳號 {UserName}（緊急存取帳號，不可停用或降級）。" +
            "上線後第一件事請登入並用「修改密碼」換掉初始密碼，換完可移除設定值 {Key}。" +
            "日常請使用各自的帳號，這個帳號只在沒有其他主管進得來時使用。",
            BootstrapUserName,
            SeedPasswordKey);
    }

    private static string Describe(IdentityResult result)
        => string.Join("；", result.Errors.Select(x => x.Description));
}
