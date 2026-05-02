using websitebanlaptop.Models;
using Microsoft.AspNetCore.Http;

namespace websitebanlaptop.Extensions;

public static class AdminSessionExtensions
{
    public const string AdminSessionKey = "AdminLoggedIn";

    public static bool IsAdminAuthenticated(this ISession session) => session.GetString(AdminSessionKey) == "1";
    public static bool IsSuperAdmin(this ISession session) => session.GetString("AdminIsSuper") == "1";
    public static bool CanViewOrders(this ISession session) => session.IsSuperAdmin() || session.GetString("CanViewOrders") == "1";
    public static bool CanUpdateOrders(this ISession session) => session.IsSuperAdmin() || session.GetString("CanUpdateOrders") == "1";
    public static bool CanCancelOrders(this ISession session) => session.IsSuperAdmin() || session.GetString("CanCancelOrders") == "1";
    public static bool CanViewReviews(this ISession session) => session.IsSuperAdmin() || session.GetString("CanViewReviews") == "1";
    public static bool CanReplyReviews(this ISession session) => session.IsSuperAdmin() || session.GetString("CanReplyReviews") == "1";
    public static bool CanDeleteReviews(this ISession session) => session.IsSuperAdmin() || session.GetString("CanDeleteReviews") == "1";
    public static bool CanManageInventory(this ISession session) => session.IsSuperAdmin() || session.GetString("CanManageInventory") == "1";
    public static bool CanDeleteInventory(this ISession session) => session.IsSuperAdmin() || session.GetString("CanDeleteInventory") == "1";
    public static bool CanImportInventory(this ISession session) => session.IsSuperAdmin() || session.GetString("CanImportInventory") == "1";
    public static bool CanManageWebsite(this ISession session) => session.IsSuperAdmin() || session.GetString("CanManageWebsite") == "1";
    public static bool CanManageUsers(this ISession session) => session.IsSuperAdmin();

    public static void SignInAdmin(this ISession session, AdminUserSessionModel user)
    {
        session.SetString(AdminSessionKey, "1");
        session.SetString("AdminUserId", user.UserId.ToString());
        session.SetString("AdminUser", user.Username);
        session.SetString("AdminFullName", user.FullName ?? user.Username);
        session.SetString("AdminIsSuper", user.IsSuperAdmin ? "1" : "0");
        session.SetString("CanViewOrders", user.CanViewOrders ? "1" : "0");
        session.SetString("CanUpdateOrders", user.CanUpdateOrders ? "1" : "0");
        session.SetString("CanCancelOrders", user.CanCancelOrders ? "1" : "0");
        session.SetString("CanViewReviews", user.CanViewReviews ? "1" : "0");
        session.SetString("CanReplyReviews", user.CanReplyReviews ? "1" : "0");
        session.SetString("CanDeleteReviews", user.CanDeleteReviews ? "1" : "0");
        session.SetString("CanManageInventory", user.CanManageInventory ? "1" : "0");
        session.SetString("CanDeleteInventory", user.CanDeleteInventory ? "1" : "0");
        session.SetString("CanImportInventory", user.CanImportInventory ? "1" : "0");
        session.SetString("CanManageWebsite", user.CanManageWebsite ? "1" : "0");
    }

    public static int? GetAdminUserId(this ISession session)
    {
        var raw = session.GetString("AdminUserId");
        return int.TryParse(raw, out var userId) ? userId : null;
    }

    public static void SignOutAdmin(this ISession session)
    {
        foreach (var key in new[] { AdminSessionKey, "AdminUserId", "AdminUser", "AdminFullName", "AdminIsSuper", "CanViewOrders", "CanUpdateOrders", "CanCancelOrders", "CanViewReviews", "CanReplyReviews", "CanDeleteReviews", "CanManageInventory", "CanDeleteInventory", "CanImportInventory", "CanManageWebsite" })
            session.Remove(key);
    }
}

