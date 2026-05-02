using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Extensions;

public static class ApplicationInitializationExtensions
{
    public static async Task InitializeApplicationDataAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInfrastructureRepository>();

        await repository.BootstrapDatabaseAsync();
        await repository.EnsureAdminSecuritySchemaAsync();
    }
}
