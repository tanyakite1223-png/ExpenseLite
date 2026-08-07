using System.Security.Claims;
using ExpenseLite.Application.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ExpenseLite.Infrastructure.Identity;

/// <summary>
/// 登入時把 DisplayName 寫進 cookie 的 claim，避免每頁為了顯示姓名再查一次 users 表。
/// 代價是使用者改名後，要重新登入才會看到新名字。
/// </summary>
public sealed class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim(ExpenseLiteClaimTypes.DisplayName, user.DisplayName));

        if (user.IsProtected)
        {
            identity.AddClaim(new Claim(ExpenseLiteClaimTypes.ProtectedAccount, "true"));
        }

        return identity;
    }
}
