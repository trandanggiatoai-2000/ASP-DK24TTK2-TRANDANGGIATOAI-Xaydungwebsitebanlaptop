using websitebanlaptop.Extensions;

namespace websitebanlaptop.Middleware;

public class AdminSessionMiddleware
{
    private readonly RequestDelegate _next;
    public AdminSessionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.Equals("/admin", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/admin/", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/admin/login", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/Admin/AdminAuth/Login");
            return;
        }

        if (path.Equals("/admin/logout", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/Admin/AdminAuth/Logout");
            return;
        }

        if ((path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase)) &&
            !path.StartsWith("/Admin/AdminAuth", StringComparison.OrdinalIgnoreCase) &&
            !context.Session.IsAdminAuthenticated())
        {
            var returnUrl = Uri.EscapeDataString($"{context.Request.Path}{context.Request.QueryString}");
            context.Response.Redirect($"/Admin/AdminAuth/Login?returnUrl={returnUrl}");
            return;
        }

        await _next(context);
    }
}

