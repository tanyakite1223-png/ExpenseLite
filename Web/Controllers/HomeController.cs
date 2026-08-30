using System.Diagnostics;
using ExpenseLite.Application.Home;
using ExpenseLite.Application.Identity;
using ExpenseLite.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseLite.Web.Controllers;

public class HomeController : Controller
{
    private readonly HomeAppService _home;

    public HomeController(HomeAppService home)
    {
        _home = home;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var page = await _home.GetPageAsync(User.ToCurrentUser(), cancellationToken);
        return View(page);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
