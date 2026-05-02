using websitebanlaptop.Services;
using websitebanlaptop.Services.Contracts;
using websitebanlaptop.Services.Repositories;

namespace websitebanlaptop.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<LaptopStoreRepository>();
        services.AddScoped<IStoreRepository>(sp => sp.GetRequiredService<LaptopStoreRepository>());
        services.AddScoped<IInfrastructureRepository, InfrastructureRepository>();
        services.AddScoped<IAdminAuthRepository, AdminAuthRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IWebsiteRepository, WebsiteRepository>();
        services.AddScoped<IStorefrontRepository, StorefrontRepository>();
        services.AddScoped<IReviewManagementRepository, ReviewManagementRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IOrderManagementRepository, OrderManagementRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<ITechNewsRepository, TechNewsRepository>();
        services.AddScoped<IStorefrontService, StorefrontService>();
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        services.AddScoped<IAdminInventoryService, AdminInventoryService>();
        services.AddScoped<IAdminContentService, AdminContentService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IAdminMediaService, AdminMediaService>();
        services.AddScoped<ILayoutContentService, LayoutContentService>();
        services.AddScoped<CartSessionService>();
        return services;
    }
}

