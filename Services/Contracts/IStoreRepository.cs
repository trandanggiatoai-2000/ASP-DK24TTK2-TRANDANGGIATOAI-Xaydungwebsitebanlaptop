namespace websitebanlaptop.Services.Contracts;

public interface IStoreRepository :
    IInfrastructureRepository,
    IAdminAuthRepository,
    IAdminUserRepository,
    IWebsiteRepository,
    IStorefrontRepository,
    IReviewManagementRepository,
    IInventoryRepository,
    IOrderManagementRepository,
    IPolicyRepository,
    ITechNewsRepository
{
}

