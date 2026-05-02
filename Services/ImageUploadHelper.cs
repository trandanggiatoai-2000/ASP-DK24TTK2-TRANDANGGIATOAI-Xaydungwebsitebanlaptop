using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace websitebanlaptop.Services;

public static class ImageUploadHelper
{
    public static async Task SaveResizedImageAsync(Stream input, string outputPath, int maxWidth, int maxHeight)
    {
        using var source = Image.FromStream(input);
        var ratio = Math.Min((double)maxWidth / source.Width, (double)maxHeight / source.Height);
        ratio = Math.Min(ratio, 1d);

        var newWidth = Math.Max(1, (int)Math.Round(source.Width * ratio));
        var newHeight = Math.Max(1, (int)Math.Round(source.Height * ratio));

        using var bitmap = new Bitmap(maxWidth, maxHeight);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var x = (maxWidth - newWidth) / 2;
        var y = (maxHeight - newHeight) / 2;
        graphics.DrawImage(source, x, y, newWidth, newHeight);

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);

        await using var fs = File.Create(outputPath);
        bitmap.Save(fs, ImageFormat.Png);
    }
}

