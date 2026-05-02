using websitebanlaptop.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace websitebanlaptop.Controllers;

public class TechNewsController : Controller
{
    private readonly IStorefrontService _storefrontService;

    public TechNewsController(IStorefrontService storefrontService)
    {
        _storefrontService = storefrontService;
    }

    public async Task<IActionResult> Index()
    {
        var posts = await _storefrontService.GetTechNewsPostsAsync();
        return View(posts);
    }

    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return RedirectToAction(nameof(Index));
        var post = await _storefrontService.GetTechNewsPostBySlugAsync(slug);
        if (post == null) return RedirectToAction(nameof(Index));
        ViewData["Title"] = string.IsNullOrWhiteSpace(post.SeoTitle) ? post.Title : post.SeoTitle;
        return View(post);
    }
}

