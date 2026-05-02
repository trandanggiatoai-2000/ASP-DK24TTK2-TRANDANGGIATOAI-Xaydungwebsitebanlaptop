using Microsoft.AspNetCore.DataProtection;
using websitebanlaptop.Extensions;
using websitebanlaptop.Middleware;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddMemoryCache();
var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("websitebanlaptop");
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".websitebanlaptop.Session";
    options.Cookie.HttpOnly = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddApplicationServices();

var app = builder.Build();

await app.InitializeApplicationDataAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseMiddleware<AdminSessionMiddleware>();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=AdminDashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
