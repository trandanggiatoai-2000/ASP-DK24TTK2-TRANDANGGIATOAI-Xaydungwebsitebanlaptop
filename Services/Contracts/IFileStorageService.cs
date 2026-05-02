namespace websitebanlaptop.Services.Contracts;

public interface IFileStorageService
{
    Task<string?> SaveAsync(IFormFile? file, string folder, int? maxWidth = null, int? maxHeight = null);
    string GetTemplatePhysicalPath(string relativeFolder, string fileName);
}

