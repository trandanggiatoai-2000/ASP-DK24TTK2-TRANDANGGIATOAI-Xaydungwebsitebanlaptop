using websitebanlaptop.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace websitebanlaptop.Controllers;

public class PolicyController : Controller
{
    private readonly IStorefrontService _storefrontService;

    public PolicyController(IStorefrontService storefrontService)
    {
        _storefrontService = storefrontService;
    }

    [HttpGet("support/{key}")]
    public async Task<IActionResult> Details(string key)
    {
        var policy = await _storefrontService.GetPolicyByKeyAsync(key);
        if (policy == null) return NotFound();

        ViewData["Title"] = string.IsNullOrWhiteSpace(policy.SeoTitle) ? policy.Title : policy.SeoTitle;
        ViewData["MetaDescription"] = policy.MetaDescription;
        return View(policy);
    }
}

