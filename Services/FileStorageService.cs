using System.Text;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services;

public class FileStorageService : IFileStorageService
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".svg"];
    private readonly IWebHostEnvironment _environment;

    public FileStorageService(IWebHostEnvironment environment) => _environment = environment;

    public async Task<string?> SaveAsync(IFormFile? file, string folder, int? maxWidth = null, int? maxHeight = null)
    {
        if (file == null || file.Length == 0) return null;

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
        ext = ext.ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext)) ext = ".jpg";

        var dir = Path.Combine(_environment.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(dir);

        var forcePng = maxWidth.HasValue && maxHeight.HasValue && ext != ".svg";
        var savedExt = forcePng ? ".png" : ext;
        var fileName = BuildShortFileName(folder, file.FileName, savedExt);
        var path = Path.Combine(dir, fileName);

        if (forcePng)
        {
            await using var stream = file.OpenReadStream();
            await ImageUploadHelper.SaveResizedImageAsync(stream, path, maxWidth!.Value, maxHeight!.Value);
        }
        else
        {
            await using var stream = File.Create(path);
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{folder}/{fileName}";
    }

    public string GetTemplatePhysicalPath(string relativeFolder, string fileName)
        => Path.Combine(_environment.WebRootPath, relativeFolder, fileName);

    private static string BuildShortFileName(string folder, string originalFileName, string extension)
    {
        var rawName = Path.GetFileNameWithoutExtension(originalFileName);
        var safeName = ToSlug(rawName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = folder.Trim().ToLowerInvariant();
        }

        if (safeName.Length > 40)
        {
            safeName = safeName[..40].Trim('-');
        }

        var shortId = Guid.NewGuid().ToString("N")[..8];
        return $"{safeName}-{shortId}{extension}";
    }

    private static string ToSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);
        var previousDash = false;

        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousDash = false;
                continue;
            }

            if (previousDash)
            {
                continue;
            }

            builder.Append('-');
            previousDash = true;
        }

        return builder.ToString().Trim('-');
    }
}

