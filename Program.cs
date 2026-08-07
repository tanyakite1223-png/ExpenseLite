using ExpenseLite.Application.CashAdvances;
using ExpenseLite.Application.ExpenseReports;
using ExpenseLite.Application.Identity;
using ExpenseLite.Application.Projects;
using ExpenseLite.Infrastructure;
using ExpenseLite.Infrastructure.Identity;
using ExpenseLite.Web.Filters;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add<ForbiddenOperationExceptionFilter>();
    })
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Clear();
        options.ViewLocationFormats.Add("/Web/Views/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Web/Views/Shared/{0}.cshtml");
    });
builder.Services.AddScoped<ExpenseReportAppService>();
builder.Services.AddScoped<CashAdvanceAppService>();
builder.Services.AddScoped<ProjectAppService>();
builder.Services.AddScoped<UserAccountAppService>();
builder.Services.AddExpenseLiteInfrastructure(builder.Configuration);

// 全站預設都要登入才能看，例外要自己標 [AllowAnonymous]（登入頁、錯誤頁）。
// 用「預設關起來」而不是逐個 controller 加 [Authorize]，可以避免以後新增頁面時忘了加而漏權限。
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// 登入 cookie 的導向路徑屬於 Web 層的路由決定，所以留在這裡而不是 Infrastructure。
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

var app = builder.Build();

await IdentityBootstrapper.BootstrapAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
