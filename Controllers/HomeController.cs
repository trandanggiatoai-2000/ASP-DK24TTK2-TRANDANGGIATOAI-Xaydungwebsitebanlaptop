using Microsoft.AspNetCore.Mvc;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Controllers;

public class HomeController : Controller
{
    private readonly IStorefrontService _storefrontService;

    public HomeController(IStorefrontService storefrontService)
    {
        _storefrontService = storefrontService;
    }

    public async Task<IActionResult> Index()
    {
        var vm = await _storefrontService.GetHomePageAsync();
        return View(vm);
    }
}

