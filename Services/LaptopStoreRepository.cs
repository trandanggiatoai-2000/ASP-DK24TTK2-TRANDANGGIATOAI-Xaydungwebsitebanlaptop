using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using websitebanlaptop.Models;
using System.Text.RegularExpressions;

using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services;

public class LaptopStoreRepository : IStoreRepository
{
    private readonly string _connectionString;
    private readonly IMemoryCache _cache;
    private const string WebsiteSettingsCacheKey = "websitebanlaptop.WebsiteSettings";
    private const string TopBrandsCacheKey = "websitebanlaptop.TopBrands";
    private const string TopCategoriesCacheKey = "websitebanlaptop.TopCategories";

    public LaptopStoreRepository(IConfiguration configuration, IMemoryCache cache)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        _cache = cache;
    }

    private SqlConnection CreateConnection() => new(_connectionString);



    public async Task<bool> IsDatabaseInitializedAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return false;

        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("SELECT CASE WHEN OBJECT_ID('WebsiteSettings','U') IS NOT NULL THEN 1 ELSE 0 END", conn);
            var result = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return result == 1;
        }
        catch
        {
            return false;
        }
    }

    public async Task BootstrapDatabaseAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("Chuỗi kết nối DefaultConnection đang trống.");

        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            return;
        }
        catch (SqlException ex) when (ex.Number == 4060)
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            var databaseName = builder.InitialCatalog;
            if (string.IsNullOrWhiteSpace(databaseName))
                throw;

            var masterBuilder = new SqlConnectionStringBuilder(_connectionString)
            {
                InitialCatalog = "master"
            };

            using var masterConn = new SqlConnection(masterBuilder.ConnectionString);
            await masterConn.OpenAsync();

            using (var createCmd = new SqlCommand(@"IF DB_ID(@db) IS NULL
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'CREATE DATABASE ' + QUOTENAME(@db);
    EXEC(@sql);
END", masterConn))
            {
                createCmd.Parameters.AddWithValue("@db", databaseName);
                await createCmd.ExecuteNonQueryAsync();
            }

            var sqlScriptPath = Path.Combine(AppContext.BaseDirectory, "SQL", "DBweblaptop.sql");
            if (!File.Exists(sqlScriptPath))
            {
                sqlScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "SQL", "DBweblaptop.sql");
            }

            if (File.Exists(sqlScriptPath))
            {
                var script = await File.ReadAllTextAsync(sqlScriptPath);
                var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x));

                using var dbConn = CreateConnection();
                await dbConn.OpenAsync();
                foreach (var batch in batches)
                {
                    using var cmd = new SqlCommand(batch, dbConn) { CommandTimeout = 120 };
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }

    public async Task EnsureAdminSecuritySchemaAsync()
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();

        async Task RunAsync(string sql, Action<SqlCommand>? configure = null)
        {
            using var cmd = new SqlCommand(sql, conn);
            configure?.Invoke(cmd);
            await cmd.ExecuteNonQueryAsync();
        }

        await RunAsync(@"
IF OBJECT_ID('AdminUsers','U') IS NULL
BEGIN
    CREATE TABLE AdminUsers(
        UserId INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        FullName NVARCHAR(150) NOT NULL CONSTRAINT DF_AdminUsers_FullName DEFAULT(N'Admin'),
        PasswordHash NVARCHAR(256) NOT NULL CONSTRAINT DF_AdminUsers_PasswordHash DEFAULT(N''),
        IsSuperAdmin BIT NOT NULL CONSTRAINT DF_AdminUsers_IsSuperAdmin DEFAULT(0),
        CanViewOrders BIT NOT NULL CONSTRAINT DF_AdminUsers_CanViewOrders DEFAULT(0),
        CanUpdateOrders BIT NOT NULL CONSTRAINT DF_AdminUsers_CanUpdateOrders DEFAULT(0),
        CanCancelOrders BIT NOT NULL CONSTRAINT DF_AdminUsers_CanCancelOrders DEFAULT(0),
        CanViewReviews BIT NOT NULL CONSTRAINT DF_AdminUsers_CanViewReviews DEFAULT(0),
        CanReplyReviews BIT NOT NULL CONSTRAINT DF_AdminUsers_CanReplyReviews DEFAULT(0),
        CanDeleteReviews BIT NOT NULL CONSTRAINT DF_AdminUsers_CanDeleteReviews DEFAULT(0),
        CanManageInventory BIT NOT NULL CONSTRAINT DF_AdminUsers_CanManageInventory DEFAULT(0),
        CanDeleteInventory BIT NOT NULL CONSTRAINT DF_AdminUsers_CanDeleteInventory DEFAULT(0),
        CanImportInventory BIT NOT NULL CONSTRAINT DF_AdminUsers_CanImportInventory DEFAULT(0),
        CanManageWebsite BIT NOT NULL CONSTRAINT DF_AdminUsers_CanManageWebsite DEFAULT(0),
        IsActive BIT NOT NULL CONSTRAINT DF_AdminUsers_IsActive DEFAULT(1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_AdminUsers_CreatedAt DEFAULT(GETDATE())
    )
END
ELSE
BEGIN
    IF COL_LENGTH('AdminUsers','FullName') IS NULL ALTER TABLE AdminUsers ADD FullName NVARCHAR(150) NOT NULL CONSTRAINT DF_AdminUsers_FullName_Alter DEFAULT(N'Admin');
    IF COL_LENGTH('AdminUsers','PasswordHash') IS NULL ALTER TABLE AdminUsers ADD PasswordHash NVARCHAR(256) NOT NULL CONSTRAINT DF_AdminUsers_PasswordHash_Alter DEFAULT(N'');
    IF COL_LENGTH('AdminUsers','IsSuperAdmin') IS NULL ALTER TABLE AdminUsers ADD IsSuperAdmin BIT NOT NULL CONSTRAINT DF_AdminUsers_IsSuperAdmin_Alter DEFAULT(0);
    IF COL_LENGTH('AdminUsers','CanViewOrders') IS NULL ALTER TABLE AdminUsers ADD CanViewOrders BIT NOT NULL CONSTRAINT DF_AdminUsers_CanViewOrders_Alter DEFAULT(0);
    IF COL_LENGTH('AdminUsers','CanUpdateOrders') IS NULL ALTER TABLE AdminUsers ADD CanUpdateOrders BIT NOT NULL CONSTRAINT DF_AdminUsers_CanUpdateOrders_Alter DEFAULT(0);
    IF COL_LENGTH('AdminUsers','CanCancelOrders') IS NULL ALTER TABLE AdminUsers ADD CanCancelOrders BIT NOT NULL CONSTRAINT DF_AdminUsers_CanCancelOrders_Alter DEFAULT(0);
    IF COL_LENGTH('AdminUsers','CanViewReviews') IS NULL ALTER TABLE AdminUsers ADD CanViewReviews BIT NOT NULL CONSTRAINT DF_AdminUsers_CanViewReviews_Alter DEFAULT(0);
    IF COL_LENGTH('AdminUsers','CanReplyReviews') IS NULL ALTER TABLE AdminUsers ADD CanReplyReviews BIT NOT NULL CONSTRAINT DF_AdminUsers_CanReplyReviews_Alter DEFAULT(0);
    IF COL_LENGTH('AdminUsers','CanDeleteReviews') IS NULL ALTER TABLE AdminUsers ADD CanDeleteReviews BIT NOT NULL CONSTRAINT DF_AdminUsers_CanDeleteReviews_Alter DEFAULT(0);
    IF COL_LENGTH('AdminUsers','CanManageInventory') IS NULL ALTER TABLE AdminUsers ADD CanManageInventory BIT NOT NULL CONSTRAINT DF_AdminUsers_CanManageInventory_Alter DEFAULT(0);
    IF COL_LENGTH('AdminUsers','CanDeleteInventory') IS NULL ALTER TABLE AdminUsers ADD CanDeleteInventory BIT NOT NULL CONSTRAINT DF_AdminUsers_CanDeleteInventory_Alter DEFAULT(0);
    IF COL_LENGTH('AdminUsers','CanImportInventory') IS NULL ALTER TABLE AdminUsers ADD CanImportInventory BIT NOT NULL CONSTRAINT DF_AdminUsers_CanImportInventory_Alter DEFAULT(0);
    IF COL_LENGTH('AdminUsers','CanManageWebsite') IS NULL ALTER TABLE AdminUsers ADD CanManageWebsite BIT NOT NULL CONSTRAINT DF_AdminUsers_CanManageWebsite_Alter DEFAULT(0);
    IF COL_LENGTH('AdminUsers','IsActive') IS NULL ALTER TABLE AdminUsers ADD IsActive BIT NOT NULL CONSTRAINT DF_AdminUsers_IsActive_Alter DEFAULT(1);
    IF COL_LENGTH('AdminUsers','CreatedAt') IS NULL ALTER TABLE AdminUsers ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_AdminUsers_CreatedAt_Alter DEFAULT(GETDATE());
END
");

        await RunAsync(@"
IF COL_LENGTH('AdminUsers','CanManageOrders') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE AdminUsers
        SET CanViewOrders = ISNULL(CanViewOrders, 0) | ISNULL(CanManageOrders, 0),
            CanUpdateOrders = ISNULL(CanUpdateOrders, 0) | ISNULL(CanManageOrders, 0),
            CanCancelOrders = ISNULL(CanCancelOrders, 0) | ISNULL(CanManageOrders, 0)
    ')
END
IF COL_LENGTH('AdminUsers','CanManageReviews') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE AdminUsers
        SET CanViewReviews = ISNULL(CanViewReviews, 0) | ISNULL(CanManageReviews, 0),
            CanReplyReviews = ISNULL(CanReplyReviews, 0) | ISNULL(CanManageReviews, 0),
            CanDeleteReviews = ISNULL(CanDeleteReviews, 0) | ISNULL(CanManageReviews, 0)
    ')
END
UPDATE AdminUsers
SET CanDeleteInventory = ISNULL(CanDeleteInventory, 0) | ISNULL(CanManageInventory, 0),
    CanImportInventory = ISNULL(CanImportInventory, 0) | ISNULL(CanManageInventory, 0)
WHERE ISNULL(CanManageInventory, 0) = 1;

UPDATE AdminUsers
SET CanManageWebsite = 1
WHERE ISNULL(IsSuperAdmin, 0) = 1 AND ISNULL(CanManageWebsite, 0) = 0;
");


        await RunAsync(@"
IF OBJECT_ID('TechNewsPosts','U') IS NULL
BEGIN
    CREATE TABLE TechNewsPosts(
        PostId INT IDENTITY(1,1) PRIMARY KEY,
        Slug NVARCHAR(200) NOT NULL UNIQUE,
        CategoryName NVARCHAR(100) NOT NULL CONSTRAINT DF_TechNewsPosts_CategoryName DEFAULT(N'Đánh giá Laptop Hp Probook 650 G4'),
        Title NVARCHAR(300) NOT NULL,
        Summary NVARCHAR(1000) NOT NULL CONSTRAINT DF_TechNewsPosts_Summary DEFAULT(N''),
        ContentHtml NVARCHAR(MAX) NOT NULL CONSTRAINT DF_TechNewsPosts_ContentHtml DEFAULT(N''),
        SeoTitle NVARCHAR(300) NOT NULL CONSTRAINT DF_TechNewsPosts_SeoTitle DEFAULT(N''),
        MetaDescription NVARCHAR(500) NOT NULL CONSTRAINT DF_TechNewsPosts_MetaDescription DEFAULT(N''),
        ThumbnailUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_TechNewsPosts_ThumbnailUrl DEFAULT(N''),
        PublishedAtText NVARCHAR(50) NOT NULL CONSTRAINT DF_TechNewsPosts_PublishedAtText DEFAULT(N'27 Th3'),
        DisplayOrder INT NOT NULL CONSTRAINT DF_TechNewsPosts_DisplayOrder DEFAULT(0),
        IsPublished BIT NOT NULL CONSTRAINT DF_TechNewsPosts_IsPublished DEFAULT(1)
    )
END
ELSE
BEGIN
    IF COL_LENGTH('TechNewsPosts','CategoryName') IS NULL ALTER TABLE TechNewsPosts ADD CategoryName NVARCHAR(100) NOT NULL CONSTRAINT DF_TechNewsPosts_CategoryName_Alter DEFAULT(N'Đánh giá Laptop Hp Probook 650 G4');
    IF COL_LENGTH('TechNewsPosts','Summary') IS NULL ALTER TABLE TechNewsPosts ADD Summary NVARCHAR(1000) NOT NULL CONSTRAINT DF_TechNewsPosts_Summary_Alter DEFAULT(N'');
    IF COL_LENGTH('TechNewsPosts','ContentHtml') IS NULL ALTER TABLE TechNewsPosts ADD ContentHtml NVARCHAR(MAX) NOT NULL CONSTRAINT DF_TechNewsPosts_ContentHtml_Alter DEFAULT(N'');
    IF COL_LENGTH('TechNewsPosts','SeoTitle') IS NULL ALTER TABLE TechNewsPosts ADD SeoTitle NVARCHAR(300) NOT NULL CONSTRAINT DF_TechNewsPosts_SeoTitle_Alter DEFAULT(N'');
    IF COL_LENGTH('TechNewsPosts','MetaDescription') IS NULL ALTER TABLE TechNewsPosts ADD MetaDescription NVARCHAR(500) NOT NULL CONSTRAINT DF_TechNewsPosts_MetaDescription_Alter DEFAULT(N'');
    IF COL_LENGTH('TechNewsPosts','ThumbnailUrl') IS NULL ALTER TABLE TechNewsPosts ADD ThumbnailUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_TechNewsPosts_ThumbnailUrl_Alter DEFAULT(N'');
    IF COL_LENGTH('TechNewsPosts','PublishedAtText') IS NULL ALTER TABLE TechNewsPosts ADD PublishedAtText NVARCHAR(50) NOT NULL CONSTRAINT DF_TechNewsPosts_PublishedAtText_Alter DEFAULT(N'27 Th3');
    IF COL_LENGTH('TechNewsPosts','DisplayOrder') IS NULL ALTER TABLE TechNewsPosts ADD DisplayOrder INT NOT NULL CONSTRAINT DF_TechNewsPosts_DisplayOrder_Alter DEFAULT(0);
    IF COL_LENGTH('TechNewsPosts','IsPublished') IS NULL ALTER TABLE TechNewsPosts ADD IsPublished BIT NOT NULL CONSTRAINT DF_TechNewsPosts_IsPublished_Alter DEFAULT(1);
END
");

        await RunAsync(@"
IF NOT EXISTS (SELECT 1 FROM TechNewsPosts)
BEGIN
    INSERT INTO TechNewsPosts(Slug, CategoryName, Title, Summary, ContentHtml, SeoTitle, MetaDescription, ThumbnailUrl, PublishedAtText, DisplayOrder, IsPublished)
    VALUES
    (N'danh-gia-laptop-dell-precision-3590-do-hoa',N'Tin công nghệ',N'Đánh giá Laptop Dell Precision 3590: Trạm sức mạnh di động cho kỹ sư và lập trình viên 2026',N'Bài viết giới thiệu nhanh về Dell Precision 3590, hiệu năng dành cho công việc kỹ thuật, dựng hình và phát triển phần mềm.',N'<h2>Tổng quan</h2><p>Dell Precision 3590 là lựa chọn phù hợp cho nhóm khách hàng cần máy mạnh, bền và dễ nâng cấp cho công việc chuyên môn.</p><h3>Điểm nổi bật</h3><ul><li>Hiệu năng ổn định cho đồ họa và kỹ thuật</li><li>Thiết kế chuyên nghiệp, tối ưu làm việc dài giờ</li><li>Phù hợp doanh nghiệp, kỹ sư, lập trình viên</li></ul>',N'Đánh giá Laptop Dell Precision 3590 2026',N'Bài đánh giá nhanh Dell Precision 3590 cho nhu cầu đồ họa, kỹ thuật và lập trình.',N'/images/products/dell/dell-032.jpg',N'27 Th3',1,1),
    (N'review-lenovo-thinkpad-l14-gen-5-laptop-ai-gia-re',N'Tin công nghệ',N'Review Lenovo ThinkPad L14 Gen 5: Laptop AI giá rẻ cho dân văn phòng 2026',N'ThinkPad L14 Gen 5 hướng đến độ bền, bàn phím tốt và trải nghiệm văn phòng lâu dài.',N'<h2>Vì sao nên chọn ThinkPad L14 Gen 5</h2><p>Model này cân bằng giữa chi phí và độ ổn định khi dùng cho doanh nghiệp, học tập và văn phòng.</p><ul><li>Bàn phím dễ gõ</li><li>Thiết kế bền bỉ</li><li>Hiệu năng vừa đủ cho công việc hằng ngày</li></ul>',N'Review Lenovo ThinkPad L14 Gen 5',N'Đánh giá nhanh ThinkPad L14 Gen 5 cho nhu cầu học tập và văn phòng.',N'/images/products/thinkbook/thinkbook-001.jpg',N'25 Th3',2,1),
    (N'top-5-laptop-do-hoa-3d-cao-cap',N'Tin công nghệ',N'Top 5 laptop đồ họa 3D cao cấp cho dân kỹ thuật bán chạy tháng 3 2026',N'Gợi ý nhanh nhóm laptop workstation, gaming cao cấp và ultrabook mạnh cho các tác vụ 3D.',N'<h2>Danh sách tham khảo</h2><p>Nhóm máy được chọn ưu tiên hiệu năng CPU, GPU, RAM lớn và khả năng hiển thị tốt.</p><ol><li>Dell Precision</li><li>Lenovo ThinkPad / ThinkBook</li><li>HP ZBook / EliteBook</li><li>ASUS ROG / TUF</li><li>MacBook Pro</li></ol>',N'Top 5 laptop đồ họa 3D cao cấp',N'Danh sách laptop đồ họa 3D cao cấp bán chạy và đáng chú ý.',N'/images/banners/campaign-gaming.jpg',N'22 Th3',3,1),
    (N'review-hp-zbook-power-g11-laptop-do-hoa',N'Tin công nghệ',N'Review chi tiết HP Workstation ZBook Power G11: Laptop đồ họa cho dân sáng tạo 2026',N'ZBook Power G11 là mẫu workstation đáng chú ý cho sáng tạo nội dung, kỹ thuật và doanh nghiệp.',N'<h2>Trải nghiệm thực tế</h2><p>HP ZBook Power G11 cân bằng giữa độ bền, tản nhiệt và hiệu năng cho công việc sáng tạo.</p>',N'Review HP Workstation ZBook Power G11',N'Bài viết giới thiệu HP ZBook Power G11 dành cho đồ họa và sáng tạo nội dung.',N'/images/products/hp/hp-001.jpg',N'19 Th3',4,1),
    (N'danh-gia-lenovo-thinkpad-t14s-gen-4-laptop-van-phong',N'Tin công nghệ',N'Đánh giá Lenovo ThinkPad T14s Gen 4: Laptop văn phòng đáng mua 2026',N'T14s Gen 4 phù hợp người cần máy mỏng nhẹ, bảo mật tốt và làm việc di động.',N'<h2>Tổng kết</h2><p>ThinkPad T14s Gen 4 phù hợp nhóm văn phòng, quản lý và người dùng cần độ cơ động cao.</p>',N'Đánh giá Lenovo ThinkPad T14s Gen 4',N'Bài viết đánh giá nhanh Lenovo ThinkPad T14s Gen 4 cho văn phòng.',N'/images/products/thinkbook/thinkbook-011.jpg',N'17 Th3',5,1)
END
");
        await RunAsync(@"
IF OBJECT_ID('BannerSlides','U') IS NULL
BEGIN
    CREATE TABLE BannerSlides(
        SlideId INT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(200) NULL,
        Subtitle NVARCHAR(500) NULL,
        ImageUrl NVARCHAR(1000) NULL,
        LinkUrl NVARCHAR(1000) NULL,
        ButtonText NVARCHAR(100) NOT NULL CONSTRAINT DF_BannerSlides_ButtonText DEFAULT(N'Khám phá ngay'),
        SecondaryButtonText NVARCHAR(100) NOT NULL CONSTRAINT DF_BannerSlides_SecondaryButtonText DEFAULT(N'Xem chi tiết'),
        SecondaryButtonLink NVARCHAR(1000) NOT NULL CONSTRAINT DF_BannerSlides_SecondaryButtonLink DEFAULT(N'/Product/Catalog'),
        KickerText NVARCHAR(100) NOT NULL CONSTRAINT DF_BannerSlides_KickerText DEFAULT(N'ƯU ĐÃI NỔI BẬT'),
        ThemePrimaryColor NVARCHAR(20) NOT NULL CONSTRAINT DF_BannerSlides_ThemePrimaryColor DEFAULT(N'#cf2027'),
        ThemeAccentColor NVARCHAR(20) NOT NULL CONSTRAINT DF_BannerSlides_ThemeAccentColor DEFAULT(N'#7c3aed'),
        TitleFontSize INT NOT NULL CONSTRAINT DF_BannerSlides_TitleFontSize DEFAULT(54),
        SubtitleFontSize INT NOT NULL CONSTRAINT DF_BannerSlides_SubtitleFontSize DEFAULT(18),
        KickerFontSize INT NOT NULL CONSTRAINT DF_BannerSlides_KickerFontSize DEFAULT(13),
        ButtonFontSize INT NOT NULL CONSTRAINT DF_BannerSlides_ButtonFontSize DEFAULT(17),
        ChipFontSize INT NOT NULL CONSTRAINT DF_BannerSlides_ChipFontSize DEFAULT(13),
        PanelOpacityPercent INT NOT NULL CONSTRAINT DF_BannerSlides_PanelOpacityPercent DEFAULT(88),
        PanelWidthPercent INT NOT NULL CONSTRAINT DF_BannerSlides_PanelWidthPercent DEFAULT(42),
        PanelPadding INT NOT NULL CONSTRAINT DF_BannerSlides_PanelPadding DEFAULT(30),
        PanelRadius INT NOT NULL CONSTRAINT DF_BannerSlides_PanelRadius DEFAULT(28),
        DisplayOrder INT NOT NULL CONSTRAINT DF_BannerSlides_DisplayOrder DEFAULT(0)
    )
END
ELSE
BEGIN
    IF COL_LENGTH('BannerSlides','Title') IS NULL ALTER TABLE BannerSlides ADD Title NVARCHAR(200) NULL;
    IF COL_LENGTH('BannerSlides','Subtitle') IS NULL ALTER TABLE BannerSlides ADD Subtitle NVARCHAR(500) NULL;
    IF COL_LENGTH('BannerSlides','ImageUrl') IS NULL ALTER TABLE BannerSlides ADD ImageUrl NVARCHAR(1000) NULL;
    IF COL_LENGTH('BannerSlides','LinkUrl') IS NULL ALTER TABLE BannerSlides ADD LinkUrl NVARCHAR(1000) NULL;
    IF COL_LENGTH('BannerSlides','ButtonText') IS NULL ALTER TABLE BannerSlides ADD ButtonText NVARCHAR(100) NOT NULL CONSTRAINT DF_BannerSlides_ButtonText_Alter DEFAULT(N'Khám phá ngay');
    IF COL_LENGTH('BannerSlides','SecondaryButtonText') IS NULL ALTER TABLE BannerSlides ADD SecondaryButtonText NVARCHAR(100) NOT NULL CONSTRAINT DF_BannerSlides_SecondaryButtonText_Alter DEFAULT(N'Xem chi tiết');
    IF COL_LENGTH('BannerSlides','SecondaryButtonLink') IS NULL ALTER TABLE BannerSlides ADD SecondaryButtonLink NVARCHAR(1000) NOT NULL CONSTRAINT DF_BannerSlides_SecondaryButtonLink_Alter DEFAULT(N'/Product/Catalog');
    IF COL_LENGTH('BannerSlides','KickerText') IS NULL ALTER TABLE BannerSlides ADD KickerText NVARCHAR(100) NOT NULL CONSTRAINT DF_BannerSlides_KickerText_Alter DEFAULT(N'ƯU ĐÃI NỔI BẬT');
    IF COL_LENGTH('BannerSlides','ThemePrimaryColor') IS NULL ALTER TABLE BannerSlides ADD ThemePrimaryColor NVARCHAR(20) NOT NULL CONSTRAINT DF_BannerSlides_ThemePrimaryColor_Alter DEFAULT(N'#cf2027');
    IF COL_LENGTH('BannerSlides','ThemeAccentColor') IS NULL ALTER TABLE BannerSlides ADD ThemeAccentColor NVARCHAR(20) NOT NULL CONSTRAINT DF_BannerSlides_ThemeAccentColor_Alter DEFAULT(N'#7c3aed');
    IF COL_LENGTH('BannerSlides','TitleFontSize') IS NULL ALTER TABLE BannerSlides ADD TitleFontSize INT NOT NULL CONSTRAINT DF_BannerSlides_TitleFontSize_Alter DEFAULT(54);
    IF COL_LENGTH('BannerSlides','SubtitleFontSize') IS NULL ALTER TABLE BannerSlides ADD SubtitleFontSize INT NOT NULL CONSTRAINT DF_BannerSlides_SubtitleFontSize_Alter DEFAULT(18);
    IF COL_LENGTH('BannerSlides','KickerFontSize') IS NULL ALTER TABLE BannerSlides ADD KickerFontSize INT NOT NULL CONSTRAINT DF_BannerSlides_KickerFontSize_Alter DEFAULT(13);
    IF COL_LENGTH('BannerSlides','ButtonFontSize') IS NULL ALTER TABLE BannerSlides ADD ButtonFontSize INT NOT NULL CONSTRAINT DF_BannerSlides_ButtonFontSize_Alter DEFAULT(17);
    IF COL_LENGTH('BannerSlides','ChipFontSize') IS NULL ALTER TABLE BannerSlides ADD ChipFontSize INT NOT NULL CONSTRAINT DF_BannerSlides_ChipFontSize_Alter DEFAULT(13);
    IF COL_LENGTH('BannerSlides','PanelOpacityPercent') IS NULL ALTER TABLE BannerSlides ADD PanelOpacityPercent INT NOT NULL CONSTRAINT DF_BannerSlides_PanelOpacityPercent_Alter DEFAULT(88);
    IF COL_LENGTH('BannerSlides','PanelWidthPercent') IS NULL ALTER TABLE BannerSlides ADD PanelWidthPercent INT NOT NULL CONSTRAINT DF_BannerSlides_PanelWidthPercent_Alter DEFAULT(42);
    IF COL_LENGTH('BannerSlides','PanelPadding') IS NULL ALTER TABLE BannerSlides ADD PanelPadding INT NOT NULL CONSTRAINT DF_BannerSlides_PanelPadding_Alter DEFAULT(30);
    IF COL_LENGTH('BannerSlides','PanelRadius') IS NULL ALTER TABLE BannerSlides ADD PanelRadius INT NOT NULL CONSTRAINT DF_BannerSlides_PanelRadius_Alter DEFAULT(28);
    IF COL_LENGTH('BannerSlides','DisplayOrder') IS NULL ALTER TABLE BannerSlides ADD DisplayOrder INT NOT NULL CONSTRAINT DF_BannerSlides_DisplayOrder_Alter DEFAULT(0);
END

IF OBJECT_ID('Products','U') IS NULL
BEGIN
    CREATE TABLE Products(
        ProductId INT IDENTITY(1,1) PRIMARY KEY,
        Brand NVARCHAR(100) NOT NULL CONSTRAINT DF_Products_Brand DEFAULT(N''),
        CategoryName NVARCHAR(100) NOT NULL CONSTRAINT DF_Products_CategoryName DEFAULT(N''),
        ProductName NVARCHAR(255) NOT NULL CONSTRAINT DF_Products_ProductName DEFAULT(N''),
        Cpu NVARCHAR(150) NOT NULL CONSTRAINT DF_Products_Cpu DEFAULT(N''),
        Ram NVARCHAR(100) NOT NULL CONSTRAINT DF_Products_Ram DEFAULT(N''),
        Ssd NVARCHAR(100) NOT NULL CONSTRAINT DF_Products_Ssd DEFAULT(N''),
        Price DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_Price DEFAULT(0),
        OldPrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_OldPrice DEFAULT(0),
        StockQty INT NOT NULL CONSTRAINT DF_Products_StockQty DEFAULT(0),
        IsFeatured BIT NOT NULL CONSTRAINT DF_Products_IsFeatured DEFAULT(0),
        BadgeText NVARCHAR(100) NOT NULL CONSTRAINT DF_Products_BadgeText DEFAULT(N''),
        DescriptionText NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Products_DescriptionText DEFAULT(N''),
        DiscountPercent INT NOT NULL CONSTRAINT DF_Products_DiscountPercent DEFAULT(0),
        SortOrder INT NOT NULL CONSTRAINT DF_Products_SortOrder DEFAULT(0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT(GETDATE())
    )
END
ELSE
BEGIN
    IF COL_LENGTH('Products','Brand') IS NULL ALTER TABLE Products ADD Brand NVARCHAR(100) NOT NULL CONSTRAINT DF_Products_Brand_Alter DEFAULT(N'');
    IF COL_LENGTH('Products','CategoryName') IS NULL ALTER TABLE Products ADD CategoryName NVARCHAR(100) NOT NULL CONSTRAINT DF_Products_CategoryName_Alter DEFAULT(N'');
    IF COL_LENGTH('Products','ProductName') IS NULL ALTER TABLE Products ADD ProductName NVARCHAR(255) NOT NULL CONSTRAINT DF_Products_ProductName_Alter DEFAULT(N'');
    IF COL_LENGTH('Products','Cpu') IS NULL ALTER TABLE Products ADD Cpu NVARCHAR(150) NOT NULL CONSTRAINT DF_Products_Cpu_Alter DEFAULT(N'');
    IF COL_LENGTH('Products','Ram') IS NULL ALTER TABLE Products ADD Ram NVARCHAR(100) NOT NULL CONSTRAINT DF_Products_Ram_Alter DEFAULT(N'');
    IF COL_LENGTH('Products','Ssd') IS NULL ALTER TABLE Products ADD Ssd NVARCHAR(100) NOT NULL CONSTRAINT DF_Products_Ssd_Alter DEFAULT(N'');
    IF COL_LENGTH('Products','Price') IS NULL ALTER TABLE Products ADD Price DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_Price_Alter DEFAULT(0);
    IF COL_LENGTH('Products','OldPrice') IS NULL ALTER TABLE Products ADD OldPrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_OldPrice_Alter DEFAULT(0);
    IF COL_LENGTH('Products','StockQty') IS NULL ALTER TABLE Products ADD StockQty INT NOT NULL CONSTRAINT DF_Products_StockQty_Alter DEFAULT(0);
    IF COL_LENGTH('Products','IsFeatured') IS NULL ALTER TABLE Products ADD IsFeatured BIT NOT NULL CONSTRAINT DF_Products_IsFeatured_Alter DEFAULT(0);
    IF COL_LENGTH('Products','BadgeText') IS NULL ALTER TABLE Products ADD BadgeText NVARCHAR(100) NOT NULL CONSTRAINT DF_Products_BadgeText_Alter DEFAULT(N'');
    IF COL_LENGTH('Products','DescriptionText') IS NULL ALTER TABLE Products ADD DescriptionText NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Products_DescriptionText_Alter DEFAULT(N'');
    IF COL_LENGTH('Products','DiscountPercent') IS NULL ALTER TABLE Products ADD DiscountPercent INT NOT NULL CONSTRAINT DF_Products_DiscountPercent_Alter DEFAULT(0);
    IF COL_LENGTH('Products','SortOrder') IS NULL ALTER TABLE Products ADD SortOrder INT NOT NULL CONSTRAINT DF_Products_SortOrder_Alter DEFAULT(0);
    IF COL_LENGTH('Products','CreatedAt') IS NULL ALTER TABLE Products ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_Products_CreatedAt_Alter DEFAULT(GETDATE());
END

IF OBJECT_ID('ProductImages','U') IS NULL
BEGIN
    CREATE TABLE ProductImages(
        ImageId INT IDENTITY(1,1) PRIMARY KEY,
        ProductId INT NOT NULL,
        ImageUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_ProductImages_ImageUrl DEFAULT(N''),
        DisplayOrder INT NOT NULL CONSTRAINT DF_ProductImages_DisplayOrder DEFAULT(0)
    )
END
ELSE
BEGIN
    IF COL_LENGTH('ProductImages','ProductId') IS NULL ALTER TABLE ProductImages ADD ProductId INT NOT NULL CONSTRAINT DF_ProductImages_ProductId_Alter DEFAULT(0);
    IF COL_LENGTH('ProductImages','ImageUrl') IS NULL ALTER TABLE ProductImages ADD ImageUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_ProductImages_ImageUrl_Alter DEFAULT(N'');
    IF COL_LENGTH('ProductImages','DisplayOrder') IS NULL ALTER TABLE ProductImages ADD DisplayOrder INT NOT NULL CONSTRAINT DF_ProductImages_DisplayOrder_Alter DEFAULT(0);
END

IF OBJECT_ID('ProductReviews','U') IS NULL
BEGIN
    CREATE TABLE ProductReviews(
        ReviewId INT IDENTITY(1,1) PRIMARY KEY,
        ProductId INT NOT NULL,
        ReviewerName NVARCHAR(150) NOT NULL CONSTRAINT DF_ProductReviews_ReviewerName DEFAULT(N'Khách hàng'),
        Rating INT NOT NULL CONSTRAINT DF_ProductReviews_Rating DEFAULT(5),
        CommentText NVARCHAR(MAX) NOT NULL CONSTRAINT DF_ProductReviews_CommentText DEFAULT(N''),
        ImageUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_ProductReviews_ImageUrl DEFAULT(N''),
        ReplyText NVARCHAR(MAX) NOT NULL CONSTRAINT DF_ProductReviews_ReplyText DEFAULT(N''),
        ReplyCreatedAt DATETIME NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_ProductReviews_CreatedAt DEFAULT(GETDATE())
    )
END
ELSE
BEGIN
    IF COL_LENGTH('ProductReviews','ProductId') IS NULL ALTER TABLE ProductReviews ADD ProductId INT NOT NULL CONSTRAINT DF_ProductReviews_ProductId_Alter DEFAULT(0);
    IF COL_LENGTH('ProductReviews','ReviewerName') IS NULL ALTER TABLE ProductReviews ADD ReviewerName NVARCHAR(150) NOT NULL CONSTRAINT DF_ProductReviews_ReviewerName_Alter DEFAULT(N'Khách hàng');
    IF COL_LENGTH('ProductReviews','Rating') IS NULL ALTER TABLE ProductReviews ADD Rating INT NOT NULL CONSTRAINT DF_ProductReviews_Rating_Alter DEFAULT(5);
    IF COL_LENGTH('ProductReviews','CommentText') IS NULL ALTER TABLE ProductReviews ADD CommentText NVARCHAR(MAX) NOT NULL CONSTRAINT DF_ProductReviews_CommentText_Alter DEFAULT(N'');
    IF COL_LENGTH('ProductReviews','ImageUrl') IS NULL ALTER TABLE ProductReviews ADD ImageUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_ProductReviews_ImageUrl_Alter DEFAULT(N'');
    IF COL_LENGTH('ProductReviews','ReplyText') IS NULL ALTER TABLE ProductReviews ADD ReplyText NVARCHAR(MAX) NOT NULL CONSTRAINT DF_ProductReviews_ReplyText_Alter DEFAULT(N'');
    IF COL_LENGTH('ProductReviews','ReplyCreatedAt') IS NULL ALTER TABLE ProductReviews ADD ReplyCreatedAt DATETIME NULL;
    IF COL_LENGTH('ProductReviews','CreatedAt') IS NULL ALTER TABLE ProductReviews ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_ProductReviews_CreatedAt_Alter DEFAULT(GETDATE());
END

IF OBJECT_ID('Orders','U') IS NULL
BEGIN
    CREATE TABLE Orders(
        OrderId INT IDENTITY(1,1) PRIMARY KEY,
        OrderCode NVARCHAR(50) NOT NULL CONSTRAINT DF_Orders_OrderCode DEFAULT(N''),
        CustomerName NVARCHAR(150) NOT NULL CONSTRAINT DF_Orders_CustomerName DEFAULT(N''),
        Phone NVARCHAR(50) NOT NULL CONSTRAINT DF_Orders_Phone DEFAULT(N''),
        AddressLine NVARCHAR(500) NOT NULL CONSTRAINT DF_Orders_AddressLine DEFAULT(N''),
        Note NVARCHAR(1000) NOT NULL CONSTRAINT DF_Orders_Note DEFAULT(N''),
        PaymentMethod NVARCHAR(100) NOT NULL CONSTRAINT DF_Orders_PaymentMethod DEFAULT(N'COD'),
        IsPaid BIT NOT NULL CONSTRAINT DF_Orders_IsPaid DEFAULT(0),
        OrderStatus NVARCHAR(100) NOT NULL CONSTRAINT DF_Orders_OrderStatus DEFAULT(N'Chờ xác nhận'),
        TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_TotalAmount DEFAULT(0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT(GETDATE())
    )
END
ELSE
BEGIN
    IF COL_LENGTH('Orders','OrderCode') IS NULL ALTER TABLE Orders ADD OrderCode NVARCHAR(50) NOT NULL CONSTRAINT DF_Orders_OrderCode_Alter DEFAULT(N'');
    IF COL_LENGTH('Orders','CustomerName') IS NULL ALTER TABLE Orders ADD CustomerName NVARCHAR(150) NOT NULL CONSTRAINT DF_Orders_CustomerName_Alter DEFAULT(N'');
    IF COL_LENGTH('Orders','Phone') IS NULL ALTER TABLE Orders ADD Phone NVARCHAR(50) NOT NULL CONSTRAINT DF_Orders_Phone_Alter DEFAULT(N'');
    IF COL_LENGTH('Orders','AddressLine') IS NULL ALTER TABLE Orders ADD AddressLine NVARCHAR(500) NOT NULL CONSTRAINT DF_Orders_AddressLine_Alter DEFAULT(N'');
    IF COL_LENGTH('Orders','Note') IS NULL ALTER TABLE Orders ADD Note NVARCHAR(1000) NOT NULL CONSTRAINT DF_Orders_Note_Alter DEFAULT(N'');
    IF COL_LENGTH('Orders','PaymentMethod') IS NULL ALTER TABLE Orders ADD PaymentMethod NVARCHAR(100) NOT NULL CONSTRAINT DF_Orders_PaymentMethod_Alter DEFAULT(N'COD');
    IF COL_LENGTH('Orders','IsPaid') IS NULL ALTER TABLE Orders ADD IsPaid BIT NOT NULL CONSTRAINT DF_Orders_IsPaid_Alter DEFAULT(0);
    IF COL_LENGTH('Orders','OrderStatus') IS NULL ALTER TABLE Orders ADD OrderStatus NVARCHAR(100) NOT NULL CONSTRAINT DF_Orders_OrderStatus_Alter DEFAULT(N'Chờ xác nhận');
    IF COL_LENGTH('Orders','TotalAmount') IS NULL ALTER TABLE Orders ADD TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_TotalAmount_Alter DEFAULT(0);
    IF COL_LENGTH('Orders','CreatedAt') IS NULL ALTER TABLE Orders ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_Orders_CreatedAt_Alter DEFAULT(GETDATE());
END

IF OBJECT_ID('OrderItems','U') IS NULL
BEGIN
    CREATE TABLE OrderItems(
        OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL,
        ProductId INT NOT NULL,
        ProductName NVARCHAR(255) NOT NULL CONSTRAINT DF_OrderItems_ProductName DEFAULT(N''),
        UnitPrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrderItems_UnitPrice DEFAULT(0),
        Quantity INT NOT NULL CONSTRAINT DF_OrderItems_Quantity DEFAULT(1),
        LineTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrderItems_LineTotal DEFAULT(0)
    )
END
ELSE
BEGIN
    IF COL_LENGTH('OrderItems','OrderId') IS NULL ALTER TABLE OrderItems ADD OrderId INT NOT NULL CONSTRAINT DF_OrderItems_OrderId_Alter DEFAULT(0);
    IF COL_LENGTH('OrderItems','ProductId') IS NULL ALTER TABLE OrderItems ADD ProductId INT NOT NULL CONSTRAINT DF_OrderItems_ProductId_Alter DEFAULT(0);
    IF COL_LENGTH('OrderItems','ProductName') IS NULL ALTER TABLE OrderItems ADD ProductName NVARCHAR(255) NOT NULL CONSTRAINT DF_OrderItems_ProductName_Alter DEFAULT(N'');
    IF COL_LENGTH('OrderItems','UnitPrice') IS NULL ALTER TABLE OrderItems ADD UnitPrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrderItems_UnitPrice_Alter DEFAULT(0);
    IF COL_LENGTH('OrderItems','Quantity') IS NULL ALTER TABLE OrderItems ADD Quantity INT NOT NULL CONSTRAINT DF_OrderItems_Quantity_Alter DEFAULT(1);
    IF COL_LENGTH('OrderItems','LineTotal') IS NULL ALTER TABLE OrderItems ADD LineTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrderItems_LineTotal_Alter DEFAULT(0);
END
");

        await RunAsync(@"
IF OBJECT_ID('WebsiteSettings','U') IS NULL
BEGIN
    CREATE TABLE WebsiteSettings(
        SettingId INT IDENTITY(1,1) PRIMARY KEY,
        PromoText NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_PromoText DEFAULT(N'Miễn phí giao hàng nội thành cho đơn từ 15 triệu • Hotline: 1900 1234'),
        StoreName NVARCHAR(200) NOT NULL CONSTRAINT DF_WebsiteSettings_StoreName DEFAULT(N'Laptop Store Premium'),
        StoreTagline NVARCHAR(300) NOT NULL CONSTRAINT DF_WebsiteSettings_StoreTagline DEFAULT(N'Laptop • Gaming • Văn phòng • Doanh nhân'),
        LogoText NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_LogoText DEFAULT(N'LS'),
        WebsiteLogoUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_WebsiteLogoUrl DEFAULT(N''),
        AdminLogoUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_AdminLogoUrl DEFAULT(N''),
        FaviconUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_FaviconUrl DEFAULT(N''),
        WebsiteLogoCropMode NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_WebsiteLogoCropMode DEFAULT(N'square'),
        AdminLogoCropMode NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_AdminLogoCropMode DEFAULT(N'square'),
        HeaderButtonText NVARCHAR(100) NOT NULL CONSTRAINT DF_WebsiteSettings_HeaderButtonText DEFAULT(N'Xem sản phẩm'),
        HeaderButtonLink NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_HeaderButtonLink DEFAULT(N'/Product/Catalog'),
        HeroTitle NVARCHAR(300) NOT NULL CONSTRAINT DF_WebsiteSettings_HeroTitle DEFAULT(N'Giao diện bán laptop hiện đại, chuyên nghiệp và dễ quản trị'),
        HeroSubtitle NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_HeroSubtitle DEFAULT(N'Tối ưu trải nghiệm mua hàng với banner đẹp, danh mục rõ ràng và quản trị nội dung trực tiếp từ admin.'),
        HeroButtonText NVARCHAR(100) NOT NULL CONSTRAINT DF_WebsiteSettings_HeroButtonText DEFAULT(N'Khám phá ngay'),
        HeroButtonLink NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_HeroButtonLink DEFAULT(N'/Product/Catalog'),
        FeaturedBrandsTitle NVARCHAR(150) NOT NULL CONSTRAINT DF_WebsiteSettings_FeaturedBrandsTitle DEFAULT(N'Thương hiệu nổi bật'),
        DemandSectionTitle NVARCHAR(150) NOT NULL CONSTRAINT DF_WebsiteSettings_DemandSectionTitle DEFAULT(N'Nhu cầu sử dụng'),
        FeaturedProductsTitle NVARCHAR(150) NOT NULL CONSTRAINT DF_WebsiteSettings_FeaturedProductsTitle DEFAULT(N'Sản phẩm nổi bật'),
        FlashTitle NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_FlashTitle DEFAULT(N'Laptop giá chạm đáy. Đừng bỏ lỡ!'),
        FlashBadgeText NVARCHAR(120) NOT NULL CONSTRAINT DF_WebsiteSettings_FlashBadgeText DEFAULT(N'FLASH SALE'),
        FlashCountdownLabel NVARCHAR(120) NOT NULL CONSTRAINT DF_WebsiteSettings_FlashCountdownLabel DEFAULT(N'Kết thúc trong:'),
        FlashCountdownEndsAt NVARCHAR(40) NOT NULL CONSTRAINT DF_WebsiteSettings_FlashCountdownEndsAt DEFAULT(N'2030-12-31T23:59:59'),
        FlashImageUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_FlashImageUrl DEFAULT(N'/images/banners/flash-pro.svg'),
        PrimaryColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_PrimaryColor DEFAULT(N'#cf2027'),
        SecondaryColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_SecondaryColor DEFAULT(N'#1d4ed8'),
        AccentColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_AccentColor DEFAULT(N'#7c3aed'),
        HeaderBackgroundColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_HeaderBackgroundColor DEFAULT(N'#ffffff'),
        HeaderTextColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_HeaderTextColor DEFAULT(N'#0f172a'),
        StoreNameFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_StoreNameFontSize DEFAULT(18),
        StoreTaglineFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_StoreTaglineFontSize DEFAULT(14),
        PromoTextFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_PromoTextFontSize DEFAULT(14),
        HeroBackgroundColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_HeroBackgroundColor DEFAULT(N'#ffffff'),
        HeroTextColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_HeroTextColor DEFAULT(N'#0f172a'),
        HeroTitleFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_HeroTitleFontSize DEFAULT(42),
        HeroSubtitleFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_HeroSubtitleFontSize DEFAULT(18),
        HeroButtonFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_HeroButtonFontSize DEFAULT(18),
        PopupBackgroundColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_PopupBackgroundColor DEFAULT(N'#ffffff'),
        PopupTextColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_PopupTextColor DEFAULT(N'#0f172a'),
        PopupTitleFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_PopupTitleFontSize DEFAULT(38),
        PopupSubtitleFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_PopupSubtitleFontSize DEFAULT(16),
        PopupButtonFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_PopupButtonFontSize DEFAULT(17),
        SectionTitleColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_SectionTitleColor DEFAULT(N'#0f172a'),
        SectionSubtextColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_SectionSubtextColor DEFAULT(N'#475569'),
        SectionTitleFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_SectionTitleFontSize DEFAULT(22),
        FooterAbout NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterAbout DEFAULT(N'Website chuyên đề xây dựng website bán laptop với giỏ hàng, thanh toán giả lập và quản lý đơn hàng.'),
        FooterSupportTitle NVARCHAR(150) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterSupportTitle DEFAULT(N'Hỗ trợ'),
        FooterSupportLine1 NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterSupportLine1 DEFAULT(N'Giao hàng toàn quốc'),
        FooterSupportLine2 NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterSupportLine2 DEFAULT(N'Đổi trả dễ dàng'),
        FooterSupportLine3 NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterSupportLine3 DEFAULT(N'Bảo hành chính hãng'),
        FooterBankTitle NVARCHAR(150) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterBankTitle DEFAULT(N'Thông tin chuyển khoản'),
        FooterBankLine1 NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterBankLine1 DEFAULT(N'VCB - Vietcombank'),
        FooterBankLine2 NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterBankLine2 DEFAULT(N'STK: 190012340001'),
        FooterBankLine3 NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterBankLine3 DEFAULT(N'Chủ TK: LAPTOP STORE PREMIUM'),
        TopPhone1 NVARCHAR(50) NOT NULL CONSTRAINT DF_WebsiteSettings_TopPhone1 DEFAULT(N'0903 344 188'),
        TopPhone2 NVARCHAR(50) NOT NULL CONSTRAINT DF_WebsiteSettings_TopPhone2 DEFAULT(N'0909 344 188'),
        HeaderAddress NVARCHAR(300) NOT NULL CONSTRAINT DF_WebsiteSettings_HeaderAddress DEFAULT(N'617 Đường 3 Tháng 2, P.8, Quận 10, HCM'),
        FooterCompanyHeading NVARCHAR(150) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterCompanyHeading DEFAULT(N'CỬA HÀNG'),
        FooterShowroomTitle NVARCHAR(150) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShowroomTitle DEFAULT(N'Showroom bán hàng'),
        FooterShowroomAddress NVARCHAR(300) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShowroomAddress DEFAULT(N'Địa chỉ: 617 Đường 3 tháng 2, Phường 8, Quận 10, TP. Hồ Chí Minh'),
        FooterShowroomHotline NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShowroomHotline DEFAULT(N'Hotline: 0903 344 188 - 0909 344 188'),
        FooterWarrantyTitle NVARCHAR(150) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterWarrantyTitle DEFAULT(N'Trung tâm bảo hành'),
        FooterWarrantyAddress NVARCHAR(300) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterWarrantyAddress DEFAULT(N'Địa chỉ: 530 Đường 3 tháng 2, Phường 14, Quận 10, TP. Hồ Chí Minh'),
        FooterWarrantyHotline NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterWarrantyHotline DEFAULT(N'Hỗ trợ kỹ thuật: 0909 054 758'),
        FooterWorkingHoursTitle NVARCHAR(150) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterWorkingHoursTitle DEFAULT(N'Thời gian làm việc'),
        FooterWorkingHoursLine1 NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterWorkingHoursLine1 DEFAULT(N'Showroom: Thứ 2 – Chủ Nhật (8:00 – 21:00)'),
        FooterWorkingHoursLine2 NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterWorkingHoursLine2 DEFAULT(N'TT Bảo hành: Thứ 2 – Thứ 7 (8:00 – 17:00)'),
        FooterShippingTitle NVARCHAR(150) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingTitle DEFAULT(N'DỊCH VỤ GIAO HÀNG'),
        FooterShippingLine1 NVARCHAR(100) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingLine1 DEFAULT(N'VIETNAM POST'),
        FooterShippingLine2 NVARCHAR(100) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingLine2 DEFAULT(N'GHN'),
        FooterShippingLine3 NVARCHAR(100) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingLine3 DEFAULT(N'Viettel Post'),
        FooterShippingLine4 NVARCHAR(100) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingLine4 DEFAULT(N'AhaMove'),
        FooterQrLabel NVARCHAR(100) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterQrLabel DEFAULT(N'QR THANH TOÁN'),
        FooterCopyright NVARCHAR(300) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterCopyright DEFAULT(N'© Copyright Laptop Store - Chuyên Đề Xây Dựng Website Bán Laptop.'),
        CategoryBannerGamingUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_CategoryBannerGamingUrl DEFAULT(N'/images/banners/campaign-gaming.jpg'),
        CategoryBannerOfficeUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_CategoryBannerOfficeUrl DEFAULT(N'/images/banners/campaign-office.jpg'),
        CategoryBannerPremiumUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_CategoryBannerPremiumUrl DEFAULT(N'/images/banners/campaign-weekend.jpg'),
        CategoryBannerGraphicsUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_CategoryBannerGraphicsUrl DEFAULT(N'/images/banners/campaign-macbook.jpg'),
        CategoryBannerAccessoryUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_CategoryBannerAccessoryUrl DEFAULT(N'/images/accessories/accessory-keyboard.svg'),
        CategoryBannerComponentUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_CategoryBannerComponentUrl DEFAULT(N'/images/accessories/component-ram.svg'),
        FooterShippingImage1Url NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingImage1Url DEFAULT(N''),
        FooterShippingImage2Url NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingImage2Url DEFAULT(N''),
        FooterShippingImage3Url NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingImage3Url DEFAULT(N''),
        FooterShippingImage4Url NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingImage4Url DEFAULT(N''),
        FooterQrImageUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterQrImageUrl DEFAULT(N''),
        FooterInfoLink1Text NVARCHAR(200) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink1Text DEFAULT(N'Đánh giá Laptop Hp Probook 650 G4'),
        FooterInfoLink1Url NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink1Url DEFAULT(N'/TechNews/Details?slug=danh-gia-hp-probook-650-g4-van-phong-man-hinh-to'),
        FooterInfoLink2Text NVARCHAR(200) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink2Text DEFAULT(N'Laptop xách tay là gì?'),
        FooterInfoLink2Url NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink2Url DEFAULT(N'/TechNews/Details?slug=laptop-xach-tay-la-gi-nen-mua-khong'),
        FooterInfoLink3Text NVARCHAR(200) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink3Text DEFAULT(N'Mẹo dùng laptop bền và mượt'),
        FooterInfoLink3Url NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink3Url DEFAULT(N'/TechNews/Details?slug=thu-thuat-meo-su-dung-laptop-ben-va-muot'),
        FooterInfoLink4Text NVARCHAR(200) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink4Text DEFAULT(N'Top game phù hợp laptop gaming'),
        FooterInfoLink4Url NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink4Url DEFAULT(N'/TechNews/Details?slug=top-game-phu-hop-laptop-gaming'),
        FooterInfoLink5Text NVARCHAR(200) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink5Text DEFAULT(N'Hỏi đáp khi mua laptop xách tay'),
        FooterInfoLink5Url NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink5Url DEFAULT(N'/TechNews/Details?slug=hoi-dap-khi-mua-laptop-xach-tay'),
        FooterInfoLink6Text NVARCHAR(200) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink6Text DEFAULT(N'Chính sách bảo hành và hỗ trợ'),
        FooterInfoLink6Url NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink6Url DEFAULT(N'/support/warranty'),
        PopupTitle NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_PopupTitle_Create DEFAULT(N'Siêu ưu đãi cuối tuần'),
        PopupSubtitle NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_PopupSubtitle_Create DEFAULT(N'Giảm sâu cho laptop văn phòng, gaming và phụ kiện khi đặt online hôm nay.'),
        PopupButtonText NVARCHAR(100) NOT NULL CONSTRAINT DF_WebsiteSettings_PopupButtonText_Create DEFAULT(N'Xem ưu đãi'),
        PopupButtonLink NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_PopupButtonLink_Create DEFAULT(N'/Product/Catalog'),
        PopupImageUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_PopupImageUrl_Create DEFAULT(N'/images/banners/hero-2.svg'),
        PopupEnabled BIT NOT NULL CONSTRAINT DF_WebsiteSettings_PopupEnabled_Create DEFAULT(1),
        ShowHeroSection BIT NOT NULL CONSTRAINT DF_WebsiteSettings_ShowHeroSection_Create DEFAULT(1),
        ShowMegaMenu BIT NOT NULL CONSTRAINT DF_WebsiteSettings_ShowMegaMenu_Create DEFAULT(1),
        ShowFlashBanner BIT NOT NULL CONSTRAINT DF_WebsiteSettings_ShowFlashBanner_Create DEFAULT(1)
    )
END

IF COL_LENGTH('WebsiteSettings','PopupTitle') IS NULL ALTER TABLE WebsiteSettings ADD PopupTitle NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_PopupTitle DEFAULT(N'Siêu ưu đãi cuối tuần');
IF COL_LENGTH('WebsiteSettings','TopPhone1') IS NULL ALTER TABLE WebsiteSettings ADD TopPhone1 NVARCHAR(50) NOT NULL CONSTRAINT DF_WebsiteSettings_TopPhone1_Alter DEFAULT(N'0903 344 188');
IF COL_LENGTH('WebsiteSettings','TopPhone2') IS NULL ALTER TABLE WebsiteSettings ADD TopPhone2 NVARCHAR(50) NOT NULL CONSTRAINT DF_WebsiteSettings_TopPhone2_Alter DEFAULT(N'0909 344 188');
IF COL_LENGTH('WebsiteSettings','HeaderAddress') IS NULL ALTER TABLE WebsiteSettings ADD HeaderAddress NVARCHAR(300) NOT NULL CONSTRAINT DF_WebsiteSettings_HeaderAddress_Alter DEFAULT(N'617 Đường 3 Tháng 2, P.8, Quận 10, HCM');
IF COL_LENGTH('WebsiteSettings','FooterCompanyHeading') IS NULL ALTER TABLE WebsiteSettings ADD FooterCompanyHeading NVARCHAR(150) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterCompanyHeading_Alter DEFAULT(N'CỬA HÀNG');
IF COL_LENGTH('WebsiteSettings','FooterShowroomTitle') IS NULL ALTER TABLE WebsiteSettings ADD FooterShowroomTitle NVARCHAR(150) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShowroomTitle_Alter DEFAULT(N'Showroom bán hàng');
IF COL_LENGTH('WebsiteSettings','FooterShowroomAddress') IS NULL ALTER TABLE WebsiteSettings ADD FooterShowroomAddress NVARCHAR(300) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShowroomAddress_Alter DEFAULT(N'Địa chỉ: 617 Đường 3 tháng 2, Phường 8, Quận 10, TP. Hồ Chí Minh');
IF COL_LENGTH('WebsiteSettings','FooterShowroomHotline') IS NULL ALTER TABLE WebsiteSettings ADD FooterShowroomHotline NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShowroomHotline_Alter DEFAULT(N'Hotline: 0903 344 188 - 0909 344 188');
IF COL_LENGTH('WebsiteSettings','FooterWarrantyTitle') IS NULL ALTER TABLE WebsiteSettings ADD FooterWarrantyTitle NVARCHAR(150) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterWarrantyTitle_Alter DEFAULT(N'Trung tâm bảo hành');
IF COL_LENGTH('WebsiteSettings','FooterWarrantyAddress') IS NULL ALTER TABLE WebsiteSettings ADD FooterWarrantyAddress NVARCHAR(300) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterWarrantyAddress_Alter DEFAULT(N'Địa chỉ: 530 Đường 3 tháng 2, Phường 14, Quận 10, TP. Hồ Chí Minh');
IF COL_LENGTH('WebsiteSettings','FooterWarrantyHotline') IS NULL ALTER TABLE WebsiteSettings ADD FooterWarrantyHotline NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterWarrantyHotline_Alter DEFAULT(N'Hỗ trợ kỹ thuật: 0909 054 758');
IF COL_LENGTH('WebsiteSettings','FooterWorkingHoursTitle') IS NULL ALTER TABLE WebsiteSettings ADD FooterWorkingHoursTitle NVARCHAR(150) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterWorkingHoursTitle_Alter DEFAULT(N'Thời gian làm việc');
IF COL_LENGTH('WebsiteSettings','FooterWorkingHoursLine1') IS NULL ALTER TABLE WebsiteSettings ADD FooterWorkingHoursLine1 NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterWorkingHoursLine1_Alter DEFAULT(N'Showroom: Thứ 2 – Chủ Nhật (8:00 – 21:00)');
IF COL_LENGTH('WebsiteSettings','FooterWorkingHoursLine2') IS NULL ALTER TABLE WebsiteSettings ADD FooterWorkingHoursLine2 NVARCHAR(250) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterWorkingHoursLine2_Alter DEFAULT(N'TT Bảo hành: Thứ 2 – Thứ 7 (8:00 – 17:00)');
IF COL_LENGTH('WebsiteSettings','FooterShippingTitle') IS NULL ALTER TABLE WebsiteSettings ADD FooterShippingTitle NVARCHAR(150) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingTitle_Alter DEFAULT(N'DỊCH VỤ GIAO HÀNG');
IF COL_LENGTH('WebsiteSettings','FooterShippingLine1') IS NULL ALTER TABLE WebsiteSettings ADD FooterShippingLine1 NVARCHAR(100) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingLine1_Alter DEFAULT(N'VIETNAM POST');
IF COL_LENGTH('WebsiteSettings','FooterShippingLine2') IS NULL ALTER TABLE WebsiteSettings ADD FooterShippingLine2 NVARCHAR(100) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingLine2_Alter DEFAULT(N'GHN');
IF COL_LENGTH('WebsiteSettings','FooterShippingLine3') IS NULL ALTER TABLE WebsiteSettings ADD FooterShippingLine3 NVARCHAR(100) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingLine3_Alter DEFAULT(N'Viettel Post');
IF COL_LENGTH('WebsiteSettings','FooterShippingLine4') IS NULL ALTER TABLE WebsiteSettings ADD FooterShippingLine4 NVARCHAR(100) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingLine4_Alter DEFAULT(N'AhaMove');
IF COL_LENGTH('WebsiteSettings','FooterQrLabel') IS NULL ALTER TABLE WebsiteSettings ADD FooterQrLabel NVARCHAR(100) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterQrLabel_Alter DEFAULT(N'QR THANH TOÁN');
IF COL_LENGTH('WebsiteSettings','FooterCopyright') IS NULL ALTER TABLE WebsiteSettings ADD FooterCopyright NVARCHAR(300) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterCopyright_Alter DEFAULT(N'© Copyright Laptop Store  - Chuyên Đề Xây Dựng Website Bán Laptop.');
IF COL_LENGTH('WebsiteSettings','CategoryBannerGamingUrl') IS NULL ALTER TABLE WebsiteSettings ADD CategoryBannerGamingUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_CategoryBannerGamingUrl_Alter DEFAULT(N'/images/banners/campaign-gaming.jpg');
IF COL_LENGTH('WebsiteSettings','CategoryBannerOfficeUrl') IS NULL ALTER TABLE WebsiteSettings ADD CategoryBannerOfficeUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_CategoryBannerOfficeUrl_Alter DEFAULT(N'/images/banners/campaign-office.jpg');
IF COL_LENGTH('WebsiteSettings','CategoryBannerPremiumUrl') IS NULL ALTER TABLE WebsiteSettings ADD CategoryBannerPremiumUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_CategoryBannerPremiumUrl_Alter DEFAULT(N'/images/banners/campaign-weekend.jpg');
IF COL_LENGTH('WebsiteSettings','CategoryBannerGraphicsUrl') IS NULL ALTER TABLE WebsiteSettings ADD CategoryBannerGraphicsUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_CategoryBannerGraphicsUrl_Alter DEFAULT(N'/images/banners/campaign-macbook.jpg');
IF COL_LENGTH('WebsiteSettings','CategoryBannerAccessoryUrl') IS NULL ALTER TABLE WebsiteSettings ADD CategoryBannerAccessoryUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_CategoryBannerAccessoryUrl_Alter DEFAULT(N'/images/accessories/accessory-keyboard.svg');
IF COL_LENGTH('WebsiteSettings','CategoryBannerComponentUrl') IS NULL ALTER TABLE WebsiteSettings ADD CategoryBannerComponentUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_CategoryBannerComponentUrl_Alter DEFAULT(N'/images/accessories/component-ram.svg');
IF COL_LENGTH('WebsiteSettings','FooterShippingImage1Url') IS NULL ALTER TABLE WebsiteSettings ADD FooterShippingImage1Url NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingImage1Url_Alter DEFAULT(N'');
IF COL_LENGTH('WebsiteSettings','FooterShippingImage2Url') IS NULL ALTER TABLE WebsiteSettings ADD FooterShippingImage2Url NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingImage2Url_Alter DEFAULT(N'');
IF COL_LENGTH('WebsiteSettings','FooterShippingImage3Url') IS NULL ALTER TABLE WebsiteSettings ADD FooterShippingImage3Url NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingImage3Url_Alter DEFAULT(N'');
IF COL_LENGTH('WebsiteSettings','FooterShippingImage4Url') IS NULL ALTER TABLE WebsiteSettings ADD FooterShippingImage4Url NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterShippingImage4Url_Alter DEFAULT(N'');
IF COL_LENGTH('WebsiteSettings','FooterQrImageUrl') IS NULL ALTER TABLE WebsiteSettings ADD FooterQrImageUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterQrImageUrl_Alter DEFAULT(N'');
IF COL_LENGTH('WebsiteSettings','FooterInfoLink1Text') IS NULL ALTER TABLE WebsiteSettings ADD FooterInfoLink1Text NVARCHAR(200) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink1Text_Alter DEFAULT(N'Đánh giá Laptop Hp Probook 650 G4');
IF COL_LENGTH('WebsiteSettings','FooterInfoLink1Url') IS NULL ALTER TABLE WebsiteSettings ADD FooterInfoLink1Url NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink1Url_Alter DEFAULT(N'/TechNews/Details?slug=danh-gia-hp-probook-650-g4-van-phong-man-hinh-to');
IF COL_LENGTH('WebsiteSettings','FooterInfoLink2Text') IS NULL ALTER TABLE WebsiteSettings ADD FooterInfoLink2Text NVARCHAR(200) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink2Text_Alter DEFAULT(N'Laptop xách tay là gì?');
IF COL_LENGTH('WebsiteSettings','FooterInfoLink2Url') IS NULL ALTER TABLE WebsiteSettings ADD FooterInfoLink2Url NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink2Url_Alter DEFAULT(N'/TechNews/Details?slug=laptop-xach-tay-la-gi-nen-mua-khong');
IF COL_LENGTH('WebsiteSettings','FooterInfoLink3Text') IS NULL ALTER TABLE WebsiteSettings ADD FooterInfoLink3Text NVARCHAR(200) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink3Text_Alter DEFAULT(N'Mẹo dùng laptop bền và mượt');
IF COL_LENGTH('WebsiteSettings','FooterInfoLink3Url') IS NULL ALTER TABLE WebsiteSettings ADD FooterInfoLink3Url NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink3Url_Alter DEFAULT(N'/TechNews/Details?slug=thu-thuat-meo-su-dung-laptop-ben-va-muot');
IF COL_LENGTH('WebsiteSettings','FooterInfoLink4Text') IS NULL ALTER TABLE WebsiteSettings ADD FooterInfoLink4Text NVARCHAR(200) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink4Text_Alter DEFAULT(N'Top game phù hợp laptop gaming');
IF COL_LENGTH('WebsiteSettings','FooterInfoLink4Url') IS NULL ALTER TABLE WebsiteSettings ADD FooterInfoLink4Url NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink4Url_Alter DEFAULT(N'/TechNews/Details?slug=top-game-phu-hop-laptop-gaming');
IF COL_LENGTH('WebsiteSettings','FooterInfoLink5Text') IS NULL ALTER TABLE WebsiteSettings ADD FooterInfoLink5Text NVARCHAR(200) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink5Text_Alter DEFAULT(N'Hỏi đáp khi mua laptop xách tay');
IF COL_LENGTH('WebsiteSettings','FooterInfoLink5Url') IS NULL ALTER TABLE WebsiteSettings ADD FooterInfoLink5Url NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink5Url_Alter DEFAULT(N'/TechNews/Details?slug=hoi-dap-khi-mua-laptop-xach-tay');
IF COL_LENGTH('WebsiteSettings','FooterInfoLink6Text') IS NULL ALTER TABLE WebsiteSettings ADD FooterInfoLink6Text NVARCHAR(200) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink6Text_Alter DEFAULT(N'Chính sách bảo hành và hỗ trợ');
IF COL_LENGTH('WebsiteSettings','FooterInfoLink6Url') IS NULL ALTER TABLE WebsiteSettings ADD FooterInfoLink6Url NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_FooterInfoLink6Url_Alter DEFAULT(N'/support/warranty');
IF COL_LENGTH('WebsiteSettings','PopupSubtitle') IS NULL ALTER TABLE WebsiteSettings ADD PopupSubtitle NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_PopupSubtitle DEFAULT(N'Giảm sâu cho laptop văn phòng, gaming và phụ kiện khi đặt online hôm nay.');
IF COL_LENGTH('WebsiteSettings','PopupButtonText') IS NULL ALTER TABLE WebsiteSettings ADD PopupButtonText NVARCHAR(100) NOT NULL CONSTRAINT DF_WebsiteSettings_PopupButtonText DEFAULT(N'Xem ưu đãi');
IF COL_LENGTH('WebsiteSettings','PopupButtonLink') IS NULL ALTER TABLE WebsiteSettings ADD PopupButtonLink NVARCHAR(500) NOT NULL CONSTRAINT DF_WebsiteSettings_PopupButtonLink DEFAULT(N'/Product/Catalog');
IF COL_LENGTH('WebsiteSettings','PopupImageUrl') IS NULL ALTER TABLE WebsiteSettings ADD PopupImageUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_PopupImageUrl DEFAULT(N'/images/banners/hero-2.svg');
IF COL_LENGTH('WebsiteSettings','PopupEnabled') IS NULL ALTER TABLE WebsiteSettings ADD PopupEnabled BIT NOT NULL CONSTRAINT DF_WebsiteSettings_PopupEnabled DEFAULT(1);
IF COL_LENGTH('WebsiteSettings','ShowHeroSection') IS NULL ALTER TABLE WebsiteSettings ADD ShowHeroSection BIT NOT NULL CONSTRAINT DF_WebsiteSettings_ShowHeroSection DEFAULT(1);
IF COL_LENGTH('WebsiteSettings','ShowMegaMenu') IS NULL ALTER TABLE WebsiteSettings ADD ShowMegaMenu BIT NOT NULL CONSTRAINT DF_WebsiteSettings_ShowMegaMenu DEFAULT(1);
IF COL_LENGTH('WebsiteSettings','ShowFlashBanner') IS NULL ALTER TABLE WebsiteSettings ADD ShowFlashBanner BIT NOT NULL CONSTRAINT DF_WebsiteSettings_ShowFlashBanner DEFAULT(1);
IF COL_LENGTH('WebsiteSettings','FlashBadgeText') IS NULL ALTER TABLE WebsiteSettings ADD FlashBadgeText NVARCHAR(120) NOT NULL CONSTRAINT DF_WebsiteSettings_FlashBadgeText_Alter DEFAULT(N'FLASH SALE');
IF COL_LENGTH('WebsiteSettings','FlashCountdownLabel') IS NULL ALTER TABLE WebsiteSettings ADD FlashCountdownLabel NVARCHAR(120) NOT NULL CONSTRAINT DF_WebsiteSettings_FlashCountdownLabel_Alter DEFAULT(N'Kết thúc trong:');
IF COL_LENGTH('WebsiteSettings','FlashCountdownEndsAt') IS NULL ALTER TABLE WebsiteSettings ADD FlashCountdownEndsAt NVARCHAR(40) NOT NULL CONSTRAINT DF_WebsiteSettings_FlashCountdownEndsAt_Alter DEFAULT(N'2030-12-31T23:59:59');
IF COL_LENGTH('WebsiteSettings','WebsiteLogoUrl') IS NULL ALTER TABLE WebsiteSettings ADD WebsiteLogoUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_WebsiteLogoUrl_Alter DEFAULT(N'');
IF COL_LENGTH('WebsiteSettings','AdminLogoUrl') IS NULL ALTER TABLE WebsiteSettings ADD AdminLogoUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_AdminLogoUrl_Alter DEFAULT(N'');
IF COL_LENGTH('WebsiteSettings','FaviconUrl') IS NULL ALTER TABLE WebsiteSettings ADD FaviconUrl NVARCHAR(1000) NOT NULL CONSTRAINT DF_WebsiteSettings_FaviconUrl_Alter DEFAULT(N'');
IF COL_LENGTH('WebsiteSettings','WebsiteLogoCropMode') IS NULL ALTER TABLE WebsiteSettings ADD WebsiteLogoCropMode NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_WebsiteLogoCropMode_Alter DEFAULT(N'square');
IF COL_LENGTH('WebsiteSettings','AdminLogoCropMode') IS NULL ALTER TABLE WebsiteSettings ADD AdminLogoCropMode NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_AdminLogoCropMode_Alter DEFAULT(N'square');
IF COL_LENGTH('WebsiteSettings','HeaderBackgroundColor') IS NULL ALTER TABLE WebsiteSettings ADD HeaderBackgroundColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_HeaderBackgroundColor_Alter DEFAULT(N'#ffffff');
IF COL_LENGTH('WebsiteSettings','HeaderTextColor') IS NULL ALTER TABLE WebsiteSettings ADD HeaderTextColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_HeaderTextColor_Alter DEFAULT(N'#0f172a');
IF COL_LENGTH('WebsiteSettings','StoreNameFontSize') IS NULL ALTER TABLE WebsiteSettings ADD StoreNameFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_StoreNameFontSize_Alter DEFAULT(18);
IF COL_LENGTH('WebsiteSettings','StoreTaglineFontSize') IS NULL ALTER TABLE WebsiteSettings ADD StoreTaglineFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_StoreTaglineFontSize_Alter DEFAULT(14);
IF COL_LENGTH('WebsiteSettings','PromoTextFontSize') IS NULL ALTER TABLE WebsiteSettings ADD PromoTextFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_PromoTextFontSize_Alter DEFAULT(14);
IF COL_LENGTH('WebsiteSettings','HeroBackgroundColor') IS NULL ALTER TABLE WebsiteSettings ADD HeroBackgroundColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_HeroBackgroundColor_Alter DEFAULT(N'#ffffff');
IF COL_LENGTH('WebsiteSettings','HeroTextColor') IS NULL ALTER TABLE WebsiteSettings ADD HeroTextColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_HeroTextColor_Alter DEFAULT(N'#0f172a');
IF COL_LENGTH('WebsiteSettings','HeroTitleFontSize') IS NULL ALTER TABLE WebsiteSettings ADD HeroTitleFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_HeroTitleFontSize_Alter DEFAULT(42);
IF COL_LENGTH('WebsiteSettings','HeroSubtitleFontSize') IS NULL ALTER TABLE WebsiteSettings ADD HeroSubtitleFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_HeroSubtitleFontSize_Alter DEFAULT(18);
IF COL_LENGTH('WebsiteSettings','HeroButtonFontSize') IS NULL ALTER TABLE WebsiteSettings ADD HeroButtonFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_HeroButtonFontSize_Alter DEFAULT(18);
IF COL_LENGTH('WebsiteSettings','PopupBackgroundColor') IS NULL ALTER TABLE WebsiteSettings ADD PopupBackgroundColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_PopupBackgroundColor_Alter DEFAULT(N'#ffffff');
IF COL_LENGTH('WebsiteSettings','PopupTextColor') IS NULL ALTER TABLE WebsiteSettings ADD PopupTextColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_PopupTextColor_Alter DEFAULT(N'#0f172a');
IF COL_LENGTH('WebsiteSettings','PopupTitleFontSize') IS NULL ALTER TABLE WebsiteSettings ADD PopupTitleFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_PopupTitleFontSize_Alter DEFAULT(38);
IF COL_LENGTH('WebsiteSettings','PopupSubtitleFontSize') IS NULL ALTER TABLE WebsiteSettings ADD PopupSubtitleFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_PopupSubtitleFontSize_Alter DEFAULT(16);
IF COL_LENGTH('WebsiteSettings','PopupButtonFontSize') IS NULL ALTER TABLE WebsiteSettings ADD PopupButtonFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_PopupButtonFontSize_Alter DEFAULT(17);
IF COL_LENGTH('WebsiteSettings','SectionTitleColor') IS NULL ALTER TABLE WebsiteSettings ADD SectionTitleColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_SectionTitleColor_Alter DEFAULT(N'#0f172a');
IF COL_LENGTH('WebsiteSettings','SectionSubtextColor') IS NULL ALTER TABLE WebsiteSettings ADD SectionSubtextColor NVARCHAR(20) NOT NULL CONSTRAINT DF_WebsiteSettings_SectionSubtextColor_Alter DEFAULT(N'#475569');
IF COL_LENGTH('WebsiteSettings','SectionTitleFontSize') IS NULL ALTER TABLE WebsiteSettings ADD SectionTitleFontSize INT NOT NULL CONSTRAINT DF_WebsiteSettings_SectionTitleFontSize_Alter DEFAULT(22);

IF NOT EXISTS (SELECT 1 FROM WebsiteSettings)
BEGIN
    INSERT INTO WebsiteSettings DEFAULT VALUES
END
");

        await RunAsync(@"
IF OBJECT_ID('PolicyPages','U') IS NULL
BEGIN
    CREATE TABLE PolicyPages(
        PolicyId INT IDENTITY(1,1) PRIMARY KEY,
        PolicyKey NVARCHAR(100) NOT NULL UNIQUE,
        MenuLabel NVARCHAR(150) NOT NULL CONSTRAINT DF_PolicyPages_MenuLabel DEFAULT(N''),
        Title NVARCHAR(250) NOT NULL CONSTRAINT DF_PolicyPages_Title DEFAULT(N''),
        Summary NVARCHAR(1000) NOT NULL CONSTRAINT DF_PolicyPages_Summary DEFAULT(N''),
        ContentHtml NVARCHAR(MAX) NOT NULL CONSTRAINT DF_PolicyPages_ContentHtml DEFAULT(N''),
        SeoTitle NVARCHAR(255) NOT NULL CONSTRAINT DF_PolicyPages_SeoTitle DEFAULT(N''),
        MetaDescription NVARCHAR(500) NOT NULL CONSTRAINT DF_PolicyPages_MetaDescription DEFAULT(N''),
        DisplayOrder INT NOT NULL CONSTRAINT DF_PolicyPages_DisplayOrder DEFAULT(0)
    )
END
ELSE
BEGIN
    IF COL_LENGTH('PolicyPages','PolicyKey') IS NULL ALTER TABLE PolicyPages ADD PolicyKey NVARCHAR(100) NOT NULL CONSTRAINT DF_PolicyPages_PolicyKey_Alter DEFAULT(N'');
    IF COL_LENGTH('PolicyPages','MenuLabel') IS NULL ALTER TABLE PolicyPages ADD MenuLabel NVARCHAR(150) NOT NULL CONSTRAINT DF_PolicyPages_MenuLabel_Alter DEFAULT(N'');
    IF COL_LENGTH('PolicyPages','Title') IS NULL ALTER TABLE PolicyPages ADD Title NVARCHAR(250) NOT NULL CONSTRAINT DF_PolicyPages_Title_Alter DEFAULT(N'');
    IF COL_LENGTH('PolicyPages','Summary') IS NULL ALTER TABLE PolicyPages ADD Summary NVARCHAR(1000) NOT NULL CONSTRAINT DF_PolicyPages_Summary_Alter DEFAULT(N'');
    IF COL_LENGTH('PolicyPages','ContentHtml') IS NULL ALTER TABLE PolicyPages ADD ContentHtml NVARCHAR(MAX) NOT NULL CONSTRAINT DF_PolicyPages_ContentHtml_Alter DEFAULT(N'');
    IF COL_LENGTH('PolicyPages','SeoTitle') IS NULL ALTER TABLE PolicyPages ADD SeoTitle NVARCHAR(255) NOT NULL CONSTRAINT DF_PolicyPages_SeoTitle_Alter DEFAULT(N'');
    IF COL_LENGTH('PolicyPages','MetaDescription') IS NULL ALTER TABLE PolicyPages ADD MetaDescription NVARCHAR(500) NOT NULL CONSTRAINT DF_PolicyPages_MetaDescription_Alter DEFAULT(N'');
    IF COL_LENGTH('PolicyPages','DisplayOrder') IS NULL ALTER TABLE PolicyPages ADD DisplayOrder INT NOT NULL CONSTRAINT DF_PolicyPages_DisplayOrder_Alter DEFAULT(0);
END

IF NOT EXISTS (SELECT 1 FROM PolicyPages)
BEGIN
    INSERT INTO PolicyPages(PolicyKey, MenuLabel, Title, Summary, ContentHtml, SeoTitle, MetaDescription, DisplayOrder)
    VALUES
    (N'lich-su', N'Lịch sử', N'Lịch sử phát triển Laptop Store Premium', N'Tìm hiểu hành trình hình thành, phát triển và định hướng lâu dài của Laptop Store Premium trên thị trường laptop và thiết bị công nghệ.',
     N'<h2>Hành trình xây dựng thương hiệu</h2><p>Laptop Store Premium được hình thành từ định hướng trở thành điểm đến đáng tin cậy cho khách hàng đang tìm kiếm laptop, phụ kiện và giải pháp công nghệ phù hợp với nhu cầu thực tế. Từ giai đoạn đầu hoạt động, cửa hàng tập trung vào chất lượng sản phẩm, tính minh bạch trong tư vấn và trải nghiệm mua sắm thuận tiện cả tại showroom lẫn trên website.</p><div class=""policy-block""><h3>Khởi đầu từ nhu cầu thực tế của người dùng</h3><p>Chúng tôi nhận thấy nhiều khách hàng gặp khó khăn khi chọn mua laptop vì thông tin thị trường quá nhiều nhưng thiếu sự chọn lọc rõ ràng. Vì vậy, Laptop Store Premium xây dựng định hướng tư vấn theo nhu cầu sử dụng thật: học tập, văn phòng, đồ họa, gaming hay vận hành doanh nghiệp.</p></div><div class=""policy-block""><h3>Mở rộng danh mục và dịch vụ</h3><p>Không chỉ dừng ở các dòng laptop phổ biến, cửa hàng từng bước phát triển thêm phụ kiện, linh kiện, máy tính bộ và dịch vụ hậu mãi. Song song với bán hàng, chúng tôi đầu tư vào quy trình kiểm tra máy, chăm sóc sau mua, hỗ trợ kỹ thuật và quản lý đơn hàng chuyên nghiệp hơn qua hệ thống số hóa.</p></div><div class=""policy-block""><h3>Định hướng phát triển dài hạn</h3><p>Trong giai đoạn tiếp theo, Laptop Store Premium tiếp tục hướng đến mô hình bán lẻ công nghệ hiện đại, nơi khách hàng có thể dễ dàng cập nhật thông tin sản phẩm, nhận tư vấn nhanh, theo dõi đơn hàng thuận tiện và tiếp cận các chính sách hậu mãi rõ ràng. Sự phát triển của chúng tôi luôn gắn với niềm tin của khách hàng và chất lượng phục vụ mỗi ngày.</p></div>',
     N'Lịch sử phát triển Laptop Store Premium', N'Tìm hiểu quá trình hình thành, phát triển và định hướng lâu dài của Laptop Store Premium trong lĩnh vực bán lẻ laptop và thiết bị công nghệ.', 1),
    (N'dao-duc-va-chinh-truc', N'Bảo đức và chính trực', N'Đạo đức và chính trực trong kinh doanh', N'Laptop Store Premium theo đuổi giá trị minh bạch, trung thực và trách nhiệm trong từng sản phẩm, thông tin và trải nghiệm phục vụ khách hàng.',
     N'<h2>Minh bạch là nền tảng vận hành</h2><p>Tại Laptop Store Premium, mọi thông tin về sản phẩm, cấu hình, tình trạng máy, giá bán và chính sách hỗ trợ đều được trình bày rõ ràng để khách hàng dễ dàng cân nhắc trước khi đưa ra quyết định mua hàng. Chúng tôi xem sự minh bạch là nguyên tắc cốt lõi trong hoạt động kinh doanh.</p><div class=""policy-block""><h3>Trung thực trong tư vấn</h3><p>Đội ngũ tư vấn không định hướng khách mua sản phẩm vượt quá nhu cầu sử dụng. Thay vào đó, chúng tôi ưu tiên giải pháp phù hợp với ngân sách, mục tiêu sử dụng và thời gian khai thác thực tế của từng khách hàng.</p></div><div class=""policy-block""><h3>Tôn trọng chất lượng sản phẩm</h3><ul><li>Không kinh doanh hàng giả, hàng không rõ nguồn gốc.</li><li>Kiểm tra sản phẩm trước khi bàn giao.</li><li>Cung cấp thông tin bảo hành và điều kiện hỗ trợ rõ ràng.</li><li>Minh bạch chi phí trước khi xác nhận đơn hàng.</li></ul></div><div class=""policy-block""><h3>Trách nhiệm với khách hàng sau bán</h3><p>Chính trực không chỉ thể hiện ở lúc tư vấn mà còn ở cách chúng tôi hỗ trợ sau mua. Khi phát sinh vấn đề, cửa hàng chủ động tiếp nhận, hướng dẫn xử lý và đồng hành cùng khách trong phạm vi chính sách đã công bố.</p></div>',
     N'Đạo đức và chính trực trong kinh doanh | Laptop Store Premium', N'Giá trị đạo đức, sự minh bạch và chính trực trong hoạt động kinh doanh tại Laptop Store Premium.', 2),
    (N'cam-ket-cua-chung-toi', N'Cam kết của chúng tôi', N'Cam kết của Laptop Store Premium', N'Chúng tôi cam kết mang đến sản phẩm đúng mô tả, dịch vụ rõ ràng và trải nghiệm mua sắm đáng tin cậy cho từng khách hàng.',
     N'<h2>Cam kết về sản phẩm</h2><p>Laptop Store Premium cam kết cung cấp sản phẩm đúng mô tả, đúng cấu hình và được kiểm tra kỹ trước khi bàn giao. Mỗi đơn hàng đều được đối chiếu thông tin để hạn chế tối đa sai sót trong quá trình đóng gói và vận chuyển.</p><div class=""policy-block""><h3>Cam kết về dịch vụ</h3><ul><li>Tư vấn đúng nhu cầu và ngân sách.</li><li>Hỗ trợ đặt hàng nhanh chóng qua website, điện thoại hoặc tại showroom.</li><li>Xác nhận đơn rõ ràng trước khi giao.</li><li>Hỗ trợ sau bán theo đúng chính sách công bố.</li></ul></div><div class=""policy-block""><h3>Cam kết về trải nghiệm mua sắm</h3><p>Chúng tôi liên tục cải thiện giao diện website, quy trình xử lý đơn hàng và dịch vụ chăm sóc khách hàng để khách có thể mua sắm dễ hơn, tra cứu thông tin nhanh hơn và nhận được hỗ trợ kịp thời hơn.</p></div><div class=""policy-block""><h3>Cam kết phát triển bền vững</h3><p>Mục tiêu của Laptop Store Premium không dừng ở một giao dịch đơn lẻ, mà hướng đến mối quan hệ lâu dài với khách hàng bằng chất lượng ổn định, tinh thần trách nhiệm và sự đồng hành nhất quán.</p></div>',
     N'Cam kết chất lượng và dịch vụ | Laptop Store Premium', N'Tìm hiểu các cam kết về sản phẩm, dịch vụ và trải nghiệm khách hàng tại Laptop Store Premium.', 3),
    (N'dao-tao-nhan-su', N'Đào tạo nhân sự', N'Đào tạo nhân sự tại Laptop Store Premium', N'Đội ngũ nhân sự được đào tạo liên tục về sản phẩm, kỹ thuật và chăm sóc khách hàng để mang đến trải nghiệm tư vấn chuyên nghiệp hơn.',
     N'<h2>Đào tạo chuyên môn theo nhu cầu thực tế</h2><p>Nhân sự tại Laptop Store Premium được cập nhật thường xuyên về cấu hình laptop, xu hướng công nghệ, linh kiện, phần mềm phổ biến và các tình huống sử dụng thực tế để quá trình tư vấn đạt hiệu quả cao hơn.</p><div class=""policy-block""><h3>Đào tạo kỹ năng phục vụ</h3><p>Bên cạnh kiến thức sản phẩm, đội ngũ còn được rèn luyện kỹ năng giao tiếp, tiếp nhận yêu cầu, xử lý phản hồi và chăm sóc khách hàng sau bán. Điều này giúp mỗi trải nghiệm mua sắm diễn ra rõ ràng, thuận tiện và chuyên nghiệp hơn.</p></div><div class=""policy-block""><h3>Chuẩn hóa quy trình nội bộ</h3><ul><li>Quy trình tiếp nhận nhu cầu mua hàng.</li><li>Quy trình kiểm tra và bàn giao sản phẩm.</li><li>Quy trình phối hợp với kho, giao hàng và bảo hành.</li><li>Quy trình phản hồi khiếu nại và hỗ trợ kỹ thuật.</li></ul></div><div class=""policy-block""><h3>Nâng cao chất lượng dịch vụ mỗi ngày</h3><p>Chúng tôi xem đào tạo là hoạt động liên tục, không phải nhiệm vụ ngắn hạn. Việc đầu tư cho con người giúp cửa hàng cải thiện chất lượng phục vụ và tạo nên sự đồng đều trong mọi điểm chạm với khách hàng.</p></div>',
     N'Đào tạo nhân sự chuyên nghiệp | Laptop Store Premium', N'Khám phá định hướng đào tạo nhân sự về chuyên môn, quy trình và dịch vụ khách hàng tại Laptop Store Premium.', 4),
    (N'huong-dan-mua-hang', N'Hướng dẫn mua hàng', N'Hướng dẫn mua hàng tại Laptop Store Premium', N'Hướng dẫn chi tiết cách tìm sản phẩm, đặt hàng, thanh toán và nhận hàng trên website Laptop Store Premium.',
     N'<h2>Cách mua hàng trên website</h2><p>Để đặt hàng tại Laptop Store Premium, khách hàng có thể truy cập các danh mục sản phẩm, sử dụng ô tìm kiếm hoặc xem các bộ lọc theo thương hiệu, nhu cầu sử dụng và khoảng giá phù hợp.</p><div class=""policy-block""><h3>Bước 1: Tìm và chọn sản phẩm</h3><p>Truy cập trang danh mục, xem hình ảnh, thông số kỹ thuật, giá bán và các thông tin liên quan trước khi thêm sản phẩm vào giỏ hàng.</p></div><div class=""policy-block""><h3>Bước 2: Kiểm tra giỏ hàng</h3><p>Sau khi chọn sản phẩm, khách có thể cập nhật số lượng, kiểm tra giá trị đơn hàng và xác nhận lại thông tin trước khi chuyển sang bước thanh toán.</p></div><div class=""policy-block""><h3>Bước 3: Điền thông tin nhận hàng</h3><ul><li>Họ tên người nhận.</li><li>Số điện thoại liên hệ.</li><li>Địa chỉ giao hàng chính xác.</li><li>Ghi chú thêm nếu cần hỗ trợ đặc biệt.</li></ul></div><div class=""policy-block""><h3>Bước 4: Xác nhận và nhận hàng</h3><p>Sau khi đơn hàng được xác nhận, hệ thống hoặc nhân viên sẽ liên hệ để chốt thông tin. Khách hàng có thể theo dõi tình trạng đơn và nhận hỗ trợ trong suốt quá trình giao nhận.</p></div>',
     N'Hướng dẫn mua hàng online | Laptop Store Premium', N'Hướng dẫn chi tiết quy trình mua hàng, đặt hàng và nhận hàng trên website Laptop Store Premium.', 5),
    (N'chinh-sach-ban-hang', N'Chính sách bán hàng', N'Chính sách bán hàng tại Laptop Store Premium', N'Chính sách bán hàng được xây dựng theo hướng rõ ràng, thuận tiện và bảo vệ quyền lợi khách hàng trong toàn bộ quá trình mua sắm.',
     N'<h2>Chính sách thanh toán</h2><p>Laptop Store Premium hỗ trợ nhiều hình thức thanh toán như thanh toán khi nhận hàng, chuyển khoản hoặc các phương thức linh hoạt theo từng thời điểm triển khai. Tất cả thông tin thanh toán đều được xác nhận minh bạch trước khi giao dịch hoàn tất.</p><div class=""policy-block""><h3>Chính sách giao hàng</h3><p>Đơn hàng được xử lý theo thứ tự xác nhận và chuyển đến khách hàng thông qua các đối tác vận chuyển phù hợp. Thời gian giao hàng có thể thay đổi theo khu vực, thời điểm đặt hàng và tình trạng sản phẩm.</p></div><div class=""policy-block""><h3>Chính sách đổi trả và bảo hành</h3><p>Khách hàng được hỗ trợ theo đúng điều kiện đổi trả, bảo hành đã được công bố trên website hoặc tư vấn trực tiếp khi mua hàng. Chúng tôi luôn ưu tiên cách xử lý rõ ràng và thuận tiện cho khách trong phạm vi chính sách.</p></div><div class=""policy-block""><h3>Quyền lợi của khách hàng</h3><ul><li>Được tư vấn trước khi đặt hàng.</li><li>Được xác nhận thông tin đơn rõ ràng.</li><li>Được hỗ trợ khi phát sinh lỗi từ nhà sản xuất hoặc sai sót đơn hàng.</li><li>Được tiếp nhận phản hồi qua các kênh chăm sóc khách hàng của cửa hàng.</li></ul></div>',
     N'Chính sách bán hàng và hỗ trợ | Laptop Store Premium', N'Xem chính sách thanh toán, giao hàng, đổi trả, bảo hành và quyền lợi khách hàng tại Laptop Store Premium.', 6),
    (N'shipping', N'Dịch vụ giao hàng', N'Dịch vụ giao hàng tại Laptop Store Premium', N'Thông tin về quy trình đóng gói, đối tác vận chuyển, thời gian giao hàng và lưu ý khi nhận hàng tại Laptop Store Premium.',
     N'<h2>Quy trình giao hàng rõ ràng và chủ động</h2><p>Laptop Store Premium phối hợp với các đơn vị vận chuyển uy tín để đưa sản phẩm đến tay khách hàng nhanh chóng, an toàn và đúng thông tin đơn hàng. Ngay sau khi đơn được xác nhận, hệ thống sẽ chuyển sang bước chuẩn bị hàng, kiểm tra sản phẩm, đóng gói và bàn giao cho đơn vị vận chuyển phù hợp theo khu vực nhận hàng.</p><div class=""policy-block""><h3>Đối tác giao hàng</h3><p>Tùy theo địa chỉ nhận hàng, cửa hàng có thể sắp xếp giao qua các đối tác như Vietnam Post, GHN, Viettel Post hoặc Ahamove đối với một số khu vực nội thành. Việc lựa chọn đơn vị giao hàng được ưu tiên theo tiêu chí tốc độ, độ an toàn và tính phù hợp với sản phẩm.</p></div><div class=""policy-block""><h3>Thời gian giao hàng dự kiến</h3><ul><li>Nội thành: thường từ vài giờ đến 1 ngày làm việc tùy khung giờ xác nhận đơn.</li><li>Ngoại thành và liên tỉnh: thường từ 1 đến 5 ngày làm việc tùy khu vực.</li><li>Một số thời điểm cao điểm, lễ tết hoặc thời tiết bất lợi có thể phát sinh chậm hơn dự kiến.</li></ul></div><div class=""policy-block""><h3>Lưu ý khi nhận hàng</h3><ul><li>Khách hàng nên kiểm tra ngoại quan kiện hàng khi nhận.</li><li>Đối chiếu tên sản phẩm, phiên bản và phụ kiện đi kèm theo đơn.</li><li>Nếu phát hiện sai lệch, vui lòng liên hệ ngay với cửa hàng để được hỗ trợ.</li></ul></div><div class=""policy-block""><h3>Phí giao hàng</h3><p>Chi phí vận chuyển được thông báo trước khi chốt đơn và có thể thay đổi theo khu vực, khối lượng hàng hóa hoặc hình thức giao nhanh. Chúng tôi luôn cố gắng tối ưu để khách hàng có mức phí hợp lý và minh bạch.</p></div>',
     N'Dịch vụ giao hàng | Laptop Store Premium', N'Xem thông tin về quy trình đóng gói, đối tác vận chuyển, thời gian giao hàng và các lưu ý khi nhận hàng tại Laptop Store Premium.', 7),
    (N'warranty', N'Chính sách bảo hành và hỗ trợ', N'Chính sách bảo hành và hỗ trợ tại Laptop Store Premium', N'Thông tin chi tiết về chính sách bảo hành, phạm vi hỗ trợ kỹ thuật và quy trình tiếp nhận bảo hành tại Laptop Store Premium.',
     N'<h2>Hỗ trợ sau bán rõ ràng và thuận tiện</h2><p>Laptop Store Premium xây dựng chính sách bảo hành và hỗ trợ theo hướng minh bạch để khách hàng dễ theo dõi trong suốt quá trình sử dụng sản phẩm. Tùy từng nhóm hàng, chế độ bảo hành có thể áp dụng theo nhà sản xuất, nhà phân phối hoặc chính sách riêng được cửa hàng thông báo tại thời điểm mua hàng.</p><div class=""policy-block""><h3>Phạm vi bảo hành</h3><ul><li>Sản phẩm lỗi kỹ thuật do nhà sản xuất trong thời gian còn bảo hành.</li><li>Thiết bị còn nguyên tem, nhãn nhận diện và không thuộc trường hợp bị tác động ngoại lực, vào nước hoặc sử dụng sai hướng dẫn.</li><li>Một số phụ kiện, linh kiện hoặc quà tặng đi kèm có thể có điều kiện hỗ trợ riêng.</li></ul></div><div class=""policy-block""><h3>Quy trình tiếp nhận</h3><p>Khi cần bảo hành hoặc hỗ trợ kỹ thuật, khách hàng có thể liên hệ hotline, fanpage hoặc mang sản phẩm đến trung tâm bảo hành của cửa hàng. Bộ phận tiếp nhận sẽ kiểm tra tình trạng sản phẩm, đối chiếu thông tin mua hàng và hướng dẫn phương án xử lý phù hợp.</p></div><div class=""policy-block""><h3>Hỗ trợ kỹ thuật</h3><p>Ngoài bảo hành phần cứng theo điều kiện áp dụng, cửa hàng cũng hỗ trợ khách hàng ở các bước kiểm tra lỗi cơ bản, tư vấn sử dụng, hướng dẫn cài đặt hoặc kết nối các chức năng thông dụng trong phạm vi có thể hỗ trợ từ xa hoặc tại cửa hàng.</p></div><div class=""policy-block""><h3>Thời gian xử lý</h3><p>Thời gian xử lý bảo hành phụ thuộc vào tình trạng sản phẩm, linh kiện thay thế và chính sách từ hãng hoặc nhà cung cấp. Chúng tôi luôn cố gắng cập nhật tiến độ rõ ràng để khách hàng chủ động sắp xếp công việc.</p></div>',
     N'Chính sách bảo hành và hỗ trợ | Laptop Store Premium', N'Xem chi tiết chính sách bảo hành, hỗ trợ kỹ thuật và quy trình tiếp nhận bảo hành tại Laptop Store Premium.', 8)
END

IF NOT EXISTS (SELECT 1 FROM PolicyPages WHERE PolicyKey = N'lich-su')
BEGIN
    INSERT INTO PolicyPages(PolicyKey, MenuLabel, Title, Summary, ContentHtml, SeoTitle, MetaDescription, DisplayOrder) VALUES (N'lich-su', N'Lịch sử', N'Lịch sử phát triển Laptop Store Premium', N'Tìm hiểu hành trình hình thành, phát triển và định hướng lâu dài của Laptop Store Premium trên thị trường laptop và thiết bị công nghệ.', N'<h2>Hành trình xây dựng thương hiệu</h2><p>Laptop Store Premium được hình thành từ định hướng trở thành điểm đến đáng tin cậy cho khách hàng đang tìm kiếm laptop, phụ kiện và giải pháp công nghệ phù hợp với nhu cầu thực tế. Từ giai đoạn đầu hoạt động, cửa hàng tập trung vào chất lượng sản phẩm, tính minh bạch trong tư vấn và trải nghiệm mua sắm thuận tiện cả tại showroom lẫn trên website.</p><div class=""policy-block""><h3>Khởi đầu từ nhu cầu thực tế của người dùng</h3><p>Chúng tôi nhận thấy nhiều khách hàng gặp khó khăn khi chọn mua laptop vì thông tin thị trường quá nhiều nhưng thiếu sự chọn lọc rõ ràng. Vì vậy, Laptop Store Premium xây dựng định hướng tư vấn theo nhu cầu sử dụng thật: học tập, văn phòng, đồ họa, gaming hay vận hành doanh nghiệp.</p></div><div class=""policy-block""><h3>Mở rộng danh mục và dịch vụ</h3><p>Không chỉ dừng ở các dòng laptop phổ biến, cửa hàng từng bước phát triển thêm phụ kiện, linh kiện, máy tính bộ và dịch vụ hậu mãi. Song song với bán hàng, chúng tôi đầu tư vào quy trình kiểm tra máy, chăm sóc sau mua, hỗ trợ kỹ thuật và quản lý đơn hàng chuyên nghiệp hơn qua hệ thống số hóa.</p></div><div class=""policy-block""><h3>Định hướng phát triển dài hạn</h3><p>Trong giai đoạn tiếp theo, Laptop Store Premium tiếp tục hướng đến mô hình bán lẻ công nghệ hiện đại, nơi khách hàng có thể dễ dàng cập nhật thông tin sản phẩm, nhận tư vấn nhanh, theo dõi đơn hàng thuận tiện và tiếp cận các chính sách hậu mãi rõ ràng. Sự phát triển của chúng tôi luôn gắn với niềm tin của khách hàng và chất lượng phục vụ mỗi ngày.</p></div>', N'Lịch sử phát triển Laptop Store Premium', N'Tìm hiểu quá trình hình thành, phát triển và định hướng lâu dài của Laptop Store Premium trong lĩnh vực bán lẻ laptop và thiết bị công nghệ.', 1);
END

IF NOT EXISTS (SELECT 1 FROM PolicyPages WHERE PolicyKey = N'dao-duc-va-chinh-truc')
BEGIN
    INSERT INTO PolicyPages(PolicyKey, MenuLabel, Title, Summary, ContentHtml, SeoTitle, MetaDescription, DisplayOrder) VALUES (N'dao-duc-va-chinh-truc', N'Bảo đức và chính trực', N'Đạo đức và chính trực trong kinh doanh', N'Laptop Store Premium theo đuổi giá trị minh bạch, trung thực và trách nhiệm trong từng sản phẩm, thông tin và trải nghiệm phục vụ khách hàng.', N'<h2>Minh bạch là nền tảng vận hành</h2><p>Tại Laptop Store Premium, mọi thông tin về sản phẩm, cấu hình, tình trạng máy, giá bán và chính sách hỗ trợ đều được trình bày rõ ràng để khách hàng dễ dàng cân nhắc trước khi đưa ra quyết định mua hàng. Chúng tôi xem sự minh bạch là nguyên tắc cốt lõi trong hoạt động kinh doanh.</p><div class=""policy-block""><h3>Trung thực trong tư vấn</h3><p>Đội ngũ tư vấn không định hướng khách mua sản phẩm vượt quá nhu cầu sử dụng. Thay vào đó, chúng tôi ưu tiên giải pháp phù hợp với ngân sách, mục tiêu sử dụng và thời gian khai thác thực tế của từng khách hàng.</p></div><div class=""policy-block""><h3>Tôn trọng chất lượng sản phẩm</h3><ul><li>Không kinh doanh hàng giả, hàng không rõ nguồn gốc.</li><li>Kiểm tra sản phẩm trước khi bàn giao.</li><li>Cung cấp thông tin bảo hành và điều kiện hỗ trợ rõ ràng.</li><li>Minh bạch chi phí trước khi xác nhận đơn hàng.</li></ul></div><div class=""policy-block""><h3>Trách nhiệm với khách hàng sau bán</h3><p>Chính trực không chỉ thể hiện ở lúc tư vấn mà còn ở cách chúng tôi hỗ trợ sau mua. Khi phát sinh vấn đề, cửa hàng chủ động tiếp nhận, hướng dẫn xử lý và đồng hành cùng khách trong phạm vi chính sách đã công bố.</p></div>', N'Đạo đức và chính trực trong kinh doanh | Laptop Store Premium', N'Giá trị đạo đức, sự minh bạch và chính trực trong hoạt động kinh doanh tại Laptop Store Premium.', 2);
END

IF NOT EXISTS (SELECT 1 FROM PolicyPages WHERE PolicyKey = N'cam-ket-cua-chung-toi')
BEGIN
    INSERT INTO PolicyPages(PolicyKey, MenuLabel, Title, Summary, ContentHtml, SeoTitle, MetaDescription, DisplayOrder) VALUES (N'cam-ket-cua-chung-toi', N'Cam kết của chúng tôi', N'Cam kết của Laptop Store Premium', N'Chúng tôi cam kết mang đến sản phẩm đúng mô tả, dịch vụ rõ ràng và trải nghiệm mua sắm đáng tin cậy cho từng khách hàng.', N'<h2>Cam kết về sản phẩm</h2><p>Laptop Store Premium cam kết cung cấp sản phẩm đúng mô tả, đúng cấu hình và được kiểm tra kỹ trước khi bàn giao. Mỗi đơn hàng đều được đối chiếu thông tin để hạn chế tối đa sai sót trong quá trình đóng gói và vận chuyển.</p><div class=""policy-block""><h3>Cam kết về dịch vụ</h3><ul><li>Tư vấn đúng nhu cầu và ngân sách.</li><li>Hỗ trợ đặt hàng nhanh chóng qua website, điện thoại hoặc tại showroom.</li><li>Xác nhận đơn rõ ràng trước khi giao.</li><li>Hỗ trợ sau bán theo đúng chính sách công bố.</li></ul></div><div class=""policy-block""><h3>Cam kết về trải nghiệm mua sắm</h3><p>Chúng tôi liên tục cải thiện giao diện website, quy trình xử lý đơn hàng và dịch vụ chăm sóc khách hàng để khách có thể mua sắm dễ hơn, tra cứu thông tin nhanh hơn và nhận được hỗ trợ kịp thời hơn.</p></div><div class=""policy-block""><h3>Cam kết phát triển bền vững</h3><p>Mục tiêu của Laptop Store Premium không dừng ở một giao dịch đơn lẻ, mà hướng đến mối quan hệ lâu dài với khách hàng bằng chất lượng ổn định, tinh thần trách nhiệm và sự đồng hành nhất quán.</p></div>', N'Cam kết chất lượng và dịch vụ | Laptop Store Premium', N'Tìm hiểu các cam kết về sản phẩm, dịch vụ và trải nghiệm khách hàng tại Laptop Store Premium.', 3);
END

IF NOT EXISTS (SELECT 1 FROM PolicyPages WHERE PolicyKey = N'dao-tao-nhan-su')
BEGIN
    INSERT INTO PolicyPages(PolicyKey, MenuLabel, Title, Summary, ContentHtml, SeoTitle, MetaDescription, DisplayOrder) VALUES (N'dao-tao-nhan-su', N'Đào tạo nhân sự', N'Đào tạo nhân sự tại Laptop Store Premium', N'Đội ngũ nhân sự được đào tạo liên tục về sản phẩm, kỹ thuật và chăm sóc khách hàng để mang đến trải nghiệm tư vấn chuyên nghiệp hơn.', N'<h2>Đào tạo chuyên môn theo nhu cầu thực tế</h2><p>Nhân sự tại Laptop Store Premium được cập nhật thường xuyên về cấu hình laptop, xu hướng công nghệ, linh kiện, phần mềm phổ biến và các tình huống sử dụng thực tế để quá trình tư vấn đạt hiệu quả cao hơn.</p><div class=""policy-block""><h3>Đào tạo kỹ năng phục vụ</h3><p>Bên cạnh kiến thức sản phẩm, đội ngũ còn được rèn luyện kỹ năng giao tiếp, tiếp nhận yêu cầu, xử lý phản hồi và chăm sóc khách hàng sau bán. Điều này giúp mỗi trải nghiệm mua sắm diễn ra rõ ràng, thuận tiện và chuyên nghiệp hơn.</p></div><div class=""policy-block""><h3>Chuẩn hóa quy trình nội bộ</h3><ul><li>Quy trình tiếp nhận nhu cầu mua hàng.</li><li>Quy trình kiểm tra và bàn giao sản phẩm.</li><li>Quy trình phối hợp với kho, giao hàng và bảo hành.</li><li>Quy trình phản hồi khiếu nại và hỗ trợ kỹ thuật.</li></ul></div><div class=""policy-block""><h3>Nâng cao chất lượng dịch vụ mỗi ngày</h3><p>Chúng tôi xem đào tạo là hoạt động liên tục, không phải nhiệm vụ ngắn hạn. Việc đầu tư cho con người giúp cửa hàng cải thiện chất lượng phục vụ và tạo nên sự đồng đều trong mọi điểm chạm với khách hàng.</p></div>', N'Đào tạo nhân sự chuyên nghiệp | Laptop Store Premium', N'Khám phá định hướng đào tạo nhân sự về chuyên môn, quy trình và dịch vụ khách hàng tại Laptop Store Premium.', 4);
END

IF NOT EXISTS (SELECT 1 FROM PolicyPages WHERE PolicyKey = N'huong-dan-mua-hang')
BEGIN
    INSERT INTO PolicyPages(PolicyKey, MenuLabel, Title, Summary, ContentHtml, SeoTitle, MetaDescription, DisplayOrder) VALUES (N'huong-dan-mua-hang', N'Hướng dẫn mua hàng', N'Hướng dẫn mua hàng tại Laptop Store Premium', N'Hướng dẫn chi tiết cách tìm sản phẩm, đặt hàng, thanh toán và nhận hàng trên website Laptop Store Premium.', N'<h2>Cách mua hàng trên website</h2><p>Để đặt hàng tại Laptop Store Premium, khách hàng có thể truy cập các danh mục sản phẩm, sử dụng ô tìm kiếm hoặc xem các bộ lọc theo thương hiệu, nhu cầu sử dụng và khoảng giá phù hợp.</p><div class=""policy-block""><h3>Bước 1: Tìm và chọn sản phẩm</h3><p>Truy cập trang danh mục, xem hình ảnh, thông số kỹ thuật, giá bán và các thông tin liên quan trước khi thêm sản phẩm vào giỏ hàng.</p></div><div class=""policy-block""><h3>Bước 2: Kiểm tra giỏ hàng</h3><p>Sau khi chọn sản phẩm, khách có thể cập nhật số lượng, kiểm tra giá trị đơn hàng và xác nhận lại thông tin trước khi chuyển sang bước thanh toán.</p></div><div class=""policy-block""><h3>Bước 3: Điền thông tin nhận hàng</h3><ul><li>Họ tên người nhận.</li><li>Số điện thoại liên hệ.</li><li>Địa chỉ giao hàng chính xác.</li><li>Ghi chú thêm nếu cần hỗ trợ đặc biệt.</li></ul></div><div class=""policy-block""><h3>Bước 4: Xác nhận và nhận hàng</h3><p>Sau khi đơn hàng được xác nhận, hệ thống hoặc nhân viên sẽ liên hệ để chốt thông tin. Khách hàng có thể theo dõi tình trạng đơn và nhận hỗ trợ trong suốt quá trình giao nhận.</p></div>', N'Hướng dẫn mua hàng online | Laptop Store Premium', N'Hướng dẫn chi tiết quy trình mua hàng, đặt hàng và nhận hàng trên website Laptop Store Premium.', 5);
END

IF NOT EXISTS (SELECT 1 FROM PolicyPages WHERE PolicyKey = N'chinh-sach-ban-hang')
BEGIN
    INSERT INTO PolicyPages(PolicyKey, MenuLabel, Title, Summary, ContentHtml, SeoTitle, MetaDescription, DisplayOrder) VALUES (N'chinh-sach-ban-hang', N'Chính sách bán hàng', N'Chính sách bán hàng tại Laptop Store Premium', N'Chính sách bán hàng được xây dựng theo hướng rõ ràng, thuận tiện và bảo vệ quyền lợi khách hàng trong toàn bộ quá trình mua sắm.', N'<h2>Chính sách thanh toán</h2><p>Laptop Store Premium hỗ trợ nhiều hình thức thanh toán như thanh toán khi nhận hàng, chuyển khoản hoặc các phương thức linh hoạt theo từng thời điểm triển khai. Tất cả thông tin thanh toán đều được xác nhận minh bạch trước khi giao dịch hoàn tất.</p><div class=""policy-block""><h3>Chính sách giao hàng</h3><p>Đơn hàng được xử lý theo thứ tự xác nhận và chuyển đến khách hàng thông qua các đối tác vận chuyển phù hợp. Thời gian giao hàng có thể thay đổi theo khu vực, thời điểm đặt hàng và tình trạng sản phẩm.</p></div><div class=""policy-block""><h3>Chính sách đổi trả và bảo hành</h3><p>Khách hàng được hỗ trợ theo đúng điều kiện đổi trả, bảo hành đã được công bố trên website hoặc tư vấn trực tiếp khi mua hàng. Chúng tôi luôn ưu tiên cách xử lý rõ ràng và thuận tiện cho khách trong phạm vi chính sách.</p></div><div class=""policy-block""><h3>Quyền lợi của khách hàng</h3><ul><li>Được tư vấn trước khi đặt hàng.</li><li>Được xác nhận thông tin đơn rõ ràng.</li><li>Được hỗ trợ khi phát sinh lỗi từ nhà sản xuất hoặc sai sót đơn hàng.</li><li>Được tiếp nhận phản hồi qua các kênh chăm sóc khách hàng của cửa hàng.</li></ul></div>', N'Chính sách bán hàng và hỗ trợ | Laptop Store Premium', N'Xem chính sách thanh toán, giao hàng, đổi trả, bảo hành và quyền lợi khách hàng tại Laptop Store Premium.', 6);
END

IF NOT EXISTS (SELECT 1 FROM PolicyPages WHERE PolicyKey = N'shipping')
BEGIN
    INSERT INTO PolicyPages(PolicyKey, MenuLabel, Title, Summary, ContentHtml, SeoTitle, MetaDescription, DisplayOrder) VALUES (N'shipping', N'Dịch vụ giao hàng', N'Dịch vụ giao hàng tại Laptop Store Premium', N'Thông tin về quy trình đóng gói, đối tác vận chuyển, thời gian giao hàng và lưu ý khi nhận hàng tại Laptop Store Premium.', N'<h2>Quy trình giao hàng rõ ràng và chủ động</h2><p>Laptop Store Premium phối hợp với các đơn vị vận chuyển uy tín để đưa sản phẩm đến tay khách hàng nhanh chóng, an toàn và đúng thông tin đơn hàng. Ngay sau khi đơn được xác nhận, hệ thống sẽ chuyển sang bước chuẩn bị hàng, kiểm tra sản phẩm, đóng gói và bàn giao cho đơn vị vận chuyển phù hợp theo khu vực nhận hàng.</p><div class=""policy-block""><h3>Đối tác giao hàng</h3><p>Tùy theo địa chỉ nhận hàng, cửa hàng có thể sắp xếp giao qua các đối tác như Vietnam Post, GHN, Viettel Post hoặc Ahamove đối với một số khu vực nội thành. Việc lựa chọn đơn vị giao hàng được ưu tiên theo tiêu chí tốc độ, độ an toàn và tính phù hợp với sản phẩm.</p></div><div class=""policy-block""><h3>Thời gian giao hàng dự kiến</h3><ul><li>Nội thành: thường từ vài giờ đến 1 ngày làm việc tùy khung giờ xác nhận đơn.</li><li>Ngoại thành và liên tỉnh: thường từ 1 đến 5 ngày làm việc tùy khu vực.</li><li>Một số thời điểm cao điểm, lễ tết hoặc thời tiết bất lợi có thể phát sinh chậm hơn dự kiến.</li></ul></div><div class=""policy-block""><h3>Lưu ý khi nhận hàng</h3><ul><li>Khách hàng nên kiểm tra ngoại quan kiện hàng khi nhận.</li><li>Đối chiếu tên sản phẩm, phiên bản và phụ kiện đi kèm theo đơn.</li><li>Nếu phát hiện sai lệch, vui lòng liên hệ ngay với cửa hàng để được hỗ trợ.</li></ul></div><div class=""policy-block""><h3>Phí giao hàng</h3><p>Chi phí vận chuyển được thông báo trước khi chốt đơn và có thể thay đổi theo khu vực, khối lượng hàng hóa hoặc hình thức giao nhanh. Chúng tôi luôn cố gắng tối ưu để khách hàng có mức phí hợp lý và minh bạch.</p></div>', N'Dịch vụ giao hàng | Laptop Store Premium', N'Xem thông tin về quy trình đóng gói, đối tác vận chuyển, thời gian giao hàng và các lưu ý khi nhận hàng tại Laptop Store Premium.', 7);
END

IF NOT EXISTS (SELECT 1 FROM PolicyPages WHERE PolicyKey = N'warranty')
BEGIN
    INSERT INTO PolicyPages(PolicyKey, MenuLabel, Title, Summary, ContentHtml, SeoTitle, MetaDescription, DisplayOrder) VALUES (N'warranty', N'Chính sách bảo hành và hỗ trợ', N'Chính sách bảo hành và hỗ trợ tại Laptop Store Premium', N'Thông tin chi tiết về chính sách bảo hành, phạm vi hỗ trợ kỹ thuật và quy trình tiếp nhận bảo hành tại Laptop Store Premium.', N'<h2>Hỗ trợ sau bán rõ ràng và thuận tiện</h2><p>Laptop Store Premium xây dựng chính sách bảo hành và hỗ trợ theo hướng minh bạch để khách hàng dễ theo dõi trong suốt quá trình sử dụng sản phẩm. Tùy từng nhóm hàng, chế độ bảo hành có thể áp dụng theo nhà sản xuất, nhà phân phối hoặc chính sách riêng được cửa hàng thông báo tại thời điểm mua hàng.</p><div class=""policy-block""><h3>Phạm vi bảo hành</h3><ul><li>Sản phẩm lỗi kỹ thuật do nhà sản xuất trong thời gian còn bảo hành.</li><li>Thiết bị còn nguyên tem, nhãn nhận diện và không thuộc trường hợp bị tác động ngoại lực, vào nước hoặc sử dụng sai hướng dẫn.</li><li>Một số phụ kiện, linh kiện hoặc quà tặng đi kèm có thể có điều kiện hỗ trợ riêng.</li></ul></div><div class=""policy-block""><h3>Quy trình tiếp nhận</h3><p>Khi cần bảo hành hoặc hỗ trợ kỹ thuật, khách hàng có thể liên hệ hotline, fanpage hoặc mang sản phẩm đến trung tâm bảo hành của cửa hàng. Bộ phận tiếp nhận sẽ kiểm tra tình trạng sản phẩm, đối chiếu thông tin mua hàng và hướng dẫn phương án xử lý phù hợp.</p></div><div class=""policy-block""><h3>Hỗ trợ kỹ thuật</h3><p>Ngoài bảo hành phần cứng theo điều kiện áp dụng, cửa hàng cũng hỗ trợ khách hàng ở các bước kiểm tra lỗi cơ bản, tư vấn sử dụng, hướng dẫn cài đặt hoặc kết nối các chức năng thông dụng trong phạm vi có thể hỗ trợ từ xa hoặc tại cửa hàng.</p></div><div class=""policy-block""><h3>Thời gian xử lý</h3><p>Thời gian xử lý bảo hành phụ thuộc vào tình trạng sản phẩm, linh kiện thay thế và chính sách từ hãng hoặc nhà cung cấp. Chúng tôi luôn cố gắng cập nhật tiến độ rõ ràng để khách hàng chủ động sắp xếp công việc.</p></div>', N'Chính sách bảo hành và hỗ trợ | Laptop Store Premium', N'Xem chi tiết chính sách bảo hành, hỗ trợ kỹ thuật và quy trình tiếp nhận bảo hành tại Laptop Store Premium.', 8);
END
");

        await RunAsync(@"
IF NOT EXISTS (SELECT 1 FROM BannerSlides)
BEGIN
    INSERT INTO BannerSlides(Title, Subtitle, ImageUrl, LinkUrl, ButtonText, SecondaryButtonText, SecondaryButtonLink, KickerText, ThemePrimaryColor, ThemeAccentColor, TitleFontSize, SubtitleFontSize, KickerFontSize, ButtonFontSize, ChipFontSize, PanelOpacityPercent, PanelWidthPercent, PanelPadding, PanelRadius, DisplayOrder)
    VALUES
    (N'GAMING BỨT TỐC HIỆU NĂNG', N'Laptop gaming RTX, quà tặng gear và voucher giảm thêm khi mua online.', N'/images/banners/slide-gaming.jpg', N'/Product/Catalog?category=Gaming', N'Mua laptop gaming', N'Xem ưu đãi RTX', N'/Product/Catalog?category=Gaming', N'GAMING PERFORMANCE', N'#7f1d1d', N'#ef4444', 54, 18, 13, 17, 13, 88, 42, 30, 28, 1),
    (N'LAPTOP VĂN PHÒNG GỌN NHẸ, LÀM VIỆC MƯỢT', N'Mỏng nhẹ, pin bền, phù hợp học tập và công việc mỗi ngày.', N'/images/banners/slide-office.jpg', N'/Product/Catalog?category=Văn phòng', N'Chọn laptop văn phòng', N'Xem mẫu pin trâu', N'/Product/Catalog?category=Văn phòng', N'WORK SMART EVERYDAY', N'#0f766e', N'#14b8a6', 52, 18, 13, 17, 13, 88, 42, 30, 28, 2),
    (N'MACBOOK AIR & MACBOOK PRO GIÁ TỐT', N'Ưu đãi phụ kiện Apple, hỗ trợ trả góp và thu cũ đổi mới.', N'/images/banners/slide-macbook.jpg', N'/Product/Catalog?brand=Apple', N'Khám phá MacBook', N'Xem ưu đãi Apple', N'/Product/Catalog?brand=Apple', N'MACBOOK ECOSYSTEM', N'#111827', N'#6b7280', 52, 18, 13, 17, 13, 88, 42, 30, 28, 3),
    (N'SALE CUỐI TUẦN - DEAL GIẢM SÂU', N'Giảm giá, mã voucher và freeship nội thành cho nhiều mẫu hot.', N'/images/banners/slide-weekend.jpg', N'/Product/Catalog', N'Săn deal cuối tuần', N'Nhận voucher ngay', N'/Product/Catalog', N'FLASH SALE CUỐI TUẦN', N'#b91c1c', N'#f97316', 54, 18, 13, 17, 13, 88, 42, 30, 28, 4)
END

IF NOT EXISTS (SELECT 1 FROM BannerSlides WHERE DisplayOrder = 4)
BEGIN
    INSERT INTO BannerSlides(Title, Subtitle, ImageUrl, LinkUrl, ButtonText, SecondaryButtonText, SecondaryButtonLink, KickerText, ThemePrimaryColor, ThemeAccentColor, TitleFontSize, SubtitleFontSize, KickerFontSize, ButtonFontSize, ChipFontSize, PanelOpacityPercent, PanelWidthPercent, PanelPadding, PanelRadius, DisplayOrder)
    VALUES (N'SALE CUỐI TUẦN - DEAL GIẢM SÂU', N'Giảm giá, mã voucher và freeship nội thành cho nhiều mẫu hot.', N'/images/banners/slide-weekend.jpg', N'/Product/Catalog', N'Săn deal cuối tuần', N'Nhận voucher ngay', N'/Product/Catalog', N'FLASH SALE CUỐI TUẦN', N'#b91c1c', N'#f97316', 54, 18, 13, 17, 13, 88, 42, 30, 28, 4)
END

UPDATE BannerSlides SET
    Title = CASE WHEN Title IS NULL OR LTRIM(RTRIM(Title)) = '' THEN N'GAMING BỨT TỐC HIỆU NĂNG' ELSE Title END,
    Subtitle = CASE WHEN Subtitle IS NULL OR LTRIM(RTRIM(Subtitle)) = '' THEN N'Laptop gaming RTX, quà tặng gear và voucher giảm thêm khi mua online.' ELSE Subtitle END,
    LinkUrl = CASE WHEN LinkUrl IS NULL OR LTRIM(RTRIM(LinkUrl)) = '' OR LinkUrl = N'/' THEN N'/Product/Catalog?category=Gaming' ELSE LinkUrl END,
    ButtonText = CASE WHEN ButtonText IS NULL OR LTRIM(RTRIM(ButtonText)) = '' THEN N'Mua laptop gaming' ELSE ButtonText END,
    SecondaryButtonText = CASE WHEN SecondaryButtonText IS NULL OR LTRIM(RTRIM(SecondaryButtonText)) = '' THEN N'Xem ưu đãi RTX' ELSE SecondaryButtonText END,
    SecondaryButtonLink = CASE WHEN SecondaryButtonLink IS NULL OR LTRIM(RTRIM(SecondaryButtonLink)) = '' THEN N'/Product/Catalog?category=Gaming' ELSE SecondaryButtonLink END,
    KickerText = CASE WHEN KickerText IS NULL OR LTRIM(RTRIM(KickerText)) = '' THEN N'GAMING PERFORMANCE' ELSE KickerText END,
    ThemePrimaryColor = CASE WHEN ThemePrimaryColor IS NULL OR LTRIM(RTRIM(ThemePrimaryColor)) = '' THEN N'#7f1d1d' ELSE ThemePrimaryColor END,
    ThemeAccentColor = CASE WHEN ThemeAccentColor IS NULL OR LTRIM(RTRIM(ThemeAccentColor)) = '' THEN N'#ef4444' ELSE ThemeAccentColor END,
    ImageUrl = CASE WHEN ImageUrl IS NULL OR LTRIM(RTRIM(ImageUrl)) = '' OR ImageUrl LIKE '%hero-gaming.svg' OR ImageUrl LIKE '%hero-1.svg' OR ImageUrl LIKE '%slide1.svg' THEN N'/images/banners/slide-gaming.jpg' ELSE ImageUrl END
WHERE DisplayOrder = 1;

UPDATE BannerSlides SET
    Title = CASE WHEN Title IS NULL OR LTRIM(RTRIM(Title)) = '' THEN N'LAPTOP VĂN PHÒNG GỌN NHẸ, LÀM VIỆC MƯỢT' ELSE Title END,
    Subtitle = CASE WHEN Subtitle IS NULL OR LTRIM(RTRIM(Subtitle)) = '' THEN N'Mỏng nhẹ, pin bền, phù hợp học tập và công việc mỗi ngày.' ELSE Subtitle END,
    LinkUrl = CASE WHEN LinkUrl IS NULL OR LTRIM(RTRIM(LinkUrl)) = '' OR LinkUrl = N'/' THEN N'/Product/Catalog?category=Văn phòng' ELSE LinkUrl END,
    ButtonText = CASE WHEN ButtonText IS NULL OR LTRIM(RTRIM(ButtonText)) = '' THEN N'Chọn laptop văn phòng' ELSE ButtonText END,
    SecondaryButtonText = CASE WHEN SecondaryButtonText IS NULL OR LTRIM(RTRIM(SecondaryButtonText)) = '' THEN N'Xem mẫu pin trâu' ELSE SecondaryButtonText END,
    SecondaryButtonLink = CASE WHEN SecondaryButtonLink IS NULL OR LTRIM(RTRIM(SecondaryButtonLink)) = '' THEN N'/Product/Catalog?category=Văn phòng' ELSE SecondaryButtonLink END,
    KickerText = CASE WHEN KickerText IS NULL OR LTRIM(RTRIM(KickerText)) = '' THEN N'WORK SMART EVERYDAY' ELSE KickerText END,
    ThemePrimaryColor = CASE WHEN ThemePrimaryColor IS NULL OR LTRIM(RTRIM(ThemePrimaryColor)) = '' THEN N'#0f766e' ELSE ThemePrimaryColor END,
    ThemeAccentColor = CASE WHEN ThemeAccentColor IS NULL OR LTRIM(RTRIM(ThemeAccentColor)) = '' THEN N'#14b8a6' ELSE ThemeAccentColor END,
    ImageUrl = CASE WHEN ImageUrl IS NULL OR LTRIM(RTRIM(ImageUrl)) = '' OR ImageUrl LIKE '%hero-office.svg' OR ImageUrl LIKE '%hero-2.svg' OR ImageUrl LIKE '%slide2.svg' THEN N'/images/banners/slide-office.jpg' ELSE ImageUrl END
WHERE DisplayOrder = 2;

UPDATE BannerSlides SET
    Title = CASE WHEN Title IS NULL OR LTRIM(RTRIM(Title)) = '' THEN N'MACBOOK AIR & MACBOOK PRO GIÁ TỐT' ELSE Title END,
    Subtitle = CASE WHEN Subtitle IS NULL OR LTRIM(RTRIM(Subtitle)) = '' THEN N'Ưu đãi phụ kiện Apple, hỗ trợ trả góp và thu cũ đổi mới.' ELSE Subtitle END,
    LinkUrl = CASE WHEN LinkUrl IS NULL OR LTRIM(RTRIM(LinkUrl)) = '' OR LinkUrl = N'/' THEN N'/Product/Catalog?brand=Apple' ELSE LinkUrl END,
    ButtonText = CASE WHEN ButtonText IS NULL OR LTRIM(RTRIM(ButtonText)) = '' THEN N'Khám phá MacBook' ELSE ButtonText END,
    SecondaryButtonText = CASE WHEN SecondaryButtonText IS NULL OR LTRIM(RTRIM(SecondaryButtonText)) = '' THEN N'Xem ưu đãi Apple' ELSE SecondaryButtonText END,
    SecondaryButtonLink = CASE WHEN SecondaryButtonLink IS NULL OR LTRIM(RTRIM(SecondaryButtonLink)) = '' THEN N'/Product/Catalog?brand=Apple' ELSE SecondaryButtonLink END,
    KickerText = CASE WHEN KickerText IS NULL OR LTRIM(RTRIM(KickerText)) = '' THEN N'MACBOOK ECOSYSTEM' ELSE KickerText END,
    ThemePrimaryColor = CASE WHEN ThemePrimaryColor IS NULL OR LTRIM(RTRIM(ThemePrimaryColor)) = '' THEN N'#111827' ELSE ThemePrimaryColor END,
    ThemeAccentColor = CASE WHEN ThemeAccentColor IS NULL OR LTRIM(RTRIM(ThemeAccentColor)) = '' THEN N'#6b7280' ELSE ThemeAccentColor END,
    ImageUrl = CASE WHEN ImageUrl IS NULL OR LTRIM(RTRIM(ImageUrl)) = '' OR ImageUrl LIKE '%hero-macbook.svg' OR ImageUrl LIKE '%hero-3.svg' OR ImageUrl LIKE '%slide3.svg' THEN N'/images/banners/slide-macbook.jpg' ELSE ImageUrl END
WHERE DisplayOrder = 3;

UPDATE BannerSlides SET
    Title = CASE WHEN Title IS NULL OR LTRIM(RTRIM(Title)) = '' THEN N'SALE CUỐI TUẦN - DEAL GIẢM SÂU' ELSE Title END,
    Subtitle = CASE WHEN Subtitle IS NULL OR LTRIM(RTRIM(Subtitle)) = '' THEN N'Giảm giá, mã voucher và freeship nội thành cho nhiều mẫu hot.' ELSE Subtitle END,
    LinkUrl = CASE WHEN LinkUrl IS NULL OR LTRIM(RTRIM(LinkUrl)) = '' OR LinkUrl = N'/' THEN N'/Product/Catalog' ELSE LinkUrl END,
    ButtonText = CASE WHEN ButtonText IS NULL OR LTRIM(RTRIM(ButtonText)) = '' THEN N'Săn deal cuối tuần' ELSE ButtonText END,
    SecondaryButtonText = CASE WHEN SecondaryButtonText IS NULL OR LTRIM(RTRIM(SecondaryButtonText)) = '' THEN N'Nhận voucher ngay' ELSE SecondaryButtonText END,
    SecondaryButtonLink = CASE WHEN SecondaryButtonLink IS NULL OR LTRIM(RTRIM(SecondaryButtonLink)) = '' THEN N'/Product/Catalog' ELSE SecondaryButtonLink END,
    KickerText = CASE WHEN KickerText IS NULL OR LTRIM(RTRIM(KickerText)) = '' THEN N'FLASH SALE CUỐI TUẦN' ELSE KickerText END,
    ThemePrimaryColor = CASE WHEN ThemePrimaryColor IS NULL OR LTRIM(RTRIM(ThemePrimaryColor)) = '' THEN N'#b91c1c' ELSE ThemePrimaryColor END,
    ThemeAccentColor = CASE WHEN ThemeAccentColor IS NULL OR LTRIM(RTRIM(ThemeAccentColor)) = '' THEN N'#f97316' ELSE ThemeAccentColor END,
    ImageUrl = CASE WHEN ImageUrl IS NULL OR LTRIM(RTRIM(ImageUrl)) = '' OR ImageUrl LIKE '%hero-weekend.svg' OR ImageUrl LIKE '%slide4.svg' THEN N'/images/banners/slide-weekend.jpg' ELSE ImageUrl END
WHERE DisplayOrder = 4;

UPDATE WebsiteSettings SET FlashImageUrl = N'/images/banners/flash-pro.svg' WHERE FlashImageUrl IS NULL OR LTRIM(RTRIM(FlashImageUrl)) = '' OR FlashImageUrl LIKE 'https://placehold.co%' OR FlashImageUrl LIKE '%slide1.svg';
UPDATE WebsiteSettings SET FlashBadgeText = N'FLASH SALE' WHERE FlashBadgeText IS NULL OR LTRIM(RTRIM(FlashBadgeText))='';
UPDATE WebsiteSettings SET FlashCountdownLabel = N'Kết thúc trong:' WHERE FlashCountdownLabel IS NULL OR LTRIM(RTRIM(FlashCountdownLabel))='';
UPDATE WebsiteSettings SET FlashCountdownEndsAt = N'2030-12-31T23:59:59' WHERE FlashCountdownEndsAt IS NULL OR LTRIM(RTRIM(FlashCountdownEndsAt))='';
UPDATE WebsiteSettings SET PopupImageUrl = N'/images/banners/slide-weekend.jpg' WHERE PopupImageUrl IS NULL OR LTRIM(RTRIM(PopupImageUrl)) = '' OR PopupImageUrl LIKE 'https://placehold.co%' OR PopupImageUrl LIKE '%slide1.svg' OR PopupImageUrl LIKE '%hero-2.svg';
UPDATE WebsiteSettings SET TopPhone1 = N'0903 344 188' WHERE TopPhone1 IS NULL OR LTRIM(RTRIM(TopPhone1))='';
UPDATE WebsiteSettings SET TopPhone2 = N'0909 344 188' WHERE TopPhone2 IS NULL OR LTRIM(RTRIM(TopPhone2))='';
UPDATE WebsiteSettings SET HeaderAddress = N'617 Đường 3 Tháng 2, P.8, Quận 10, HCM' WHERE HeaderAddress IS NULL OR LTRIM(RTRIM(HeaderAddress))='';
UPDATE WebsiteSettings SET FooterCompanyHeading = N'CỬA HÀNG' WHERE FooterCompanyHeading IS NULL OR LTRIM(RTRIM(FooterCompanyHeading))='';
UPDATE WebsiteSettings SET FooterShowroomTitle = N'Showroom bán hàng' WHERE FooterShowroomTitle IS NULL OR LTRIM(RTRIM(FooterShowroomTitle))='';
UPDATE WebsiteSettings SET FooterShowroomAddress = N'Địa chỉ: 617 Đường 3 tháng 2, Phường 8, Quận 10, TP. Hồ Chí Minh' WHERE FooterShowroomAddress IS NULL OR LTRIM(RTRIM(FooterShowroomAddress))='';
UPDATE WebsiteSettings SET FooterShowroomHotline = N'Hotline: 0903 344 188 - 0909 344 188' WHERE FooterShowroomHotline IS NULL OR LTRIM(RTRIM(FooterShowroomHotline))='';
UPDATE WebsiteSettings SET FooterWarrantyTitle = N'Trung tâm bảo hành' WHERE FooterWarrantyTitle IS NULL OR LTRIM(RTRIM(FooterWarrantyTitle))='';
UPDATE WebsiteSettings SET FooterWarrantyAddress = N'Địa chỉ: 530 Đường 3 tháng 2, Phường 14, Quận 10, TP. Hồ Chí Minh' WHERE FooterWarrantyAddress IS NULL OR LTRIM(RTRIM(FooterWarrantyAddress))='';
UPDATE WebsiteSettings SET FooterWarrantyHotline = N'Hỗ trợ kỹ thuật: 0909 054 758' WHERE FooterWarrantyHotline IS NULL OR LTRIM(RTRIM(FooterWarrantyHotline))='';
UPDATE WebsiteSettings SET FooterWorkingHoursTitle = N'Thời gian làm việc' WHERE FooterWorkingHoursTitle IS NULL OR LTRIM(RTRIM(FooterWorkingHoursTitle))='';
UPDATE WebsiteSettings SET FooterWorkingHoursLine1 = N'Showroom: Thứ 2 – Chủ Nhật (8:00 – 21:00)' WHERE FooterWorkingHoursLine1 IS NULL OR LTRIM(RTRIM(FooterWorkingHoursLine1))='';
UPDATE WebsiteSettings SET FooterWorkingHoursLine2 = N'TT Bảo hành: Thứ 2 – Thứ 7 (8:00 – 17:00)' WHERE FooterWorkingHoursLine2 IS NULL OR LTRIM(RTRIM(FooterWorkingHoursLine2))='';
UPDATE WebsiteSettings SET FooterShippingTitle = N'DỊCH VỤ GIAO HÀNG' WHERE FooterShippingTitle IS NULL OR LTRIM(RTRIM(FooterShippingTitle))='';
UPDATE WebsiteSettings SET FooterShippingLine1 = N'VIETNAM POST' WHERE FooterShippingLine1 IS NULL OR LTRIM(RTRIM(FooterShippingLine1))='';
UPDATE WebsiteSettings SET FooterShippingLine2 = N'GHN' WHERE FooterShippingLine2 IS NULL OR LTRIM(RTRIM(FooterShippingLine2))='';
UPDATE WebsiteSettings SET FooterShippingLine3 = N'Viettel Post' WHERE FooterShippingLine3 IS NULL OR LTRIM(RTRIM(FooterShippingLine3))='';
UPDATE WebsiteSettings SET FooterShippingLine4 = N'AhaMove' WHERE FooterShippingLine4 IS NULL OR LTRIM(RTRIM(FooterShippingLine4))='';
UPDATE WebsiteSettings SET FooterQrLabel = N'QR THANH TOÁN' WHERE FooterQrLabel IS NULL OR LTRIM(RTRIM(FooterQrLabel))='';
UPDATE WebsiteSettings SET FooterCopyright = N'© Copyright Laptop Store - Chuyên Đề Xây Dựng Website Bán Laptop.' WHERE FooterCopyright IS NULL OR LTRIM(RTRIM(FooterCopyright))='';
UPDATE WebsiteSettings SET CategoryBannerGamingUrl = N'/images/banners/campaign-gaming.jpg' WHERE CategoryBannerGamingUrl IS NULL OR LTRIM(RTRIM(CategoryBannerGamingUrl))='';
UPDATE WebsiteSettings SET CategoryBannerOfficeUrl = N'/images/banners/campaign-office.jpg' WHERE CategoryBannerOfficeUrl IS NULL OR LTRIM(RTRIM(CategoryBannerOfficeUrl))='';
UPDATE WebsiteSettings SET CategoryBannerPremiumUrl = N'/images/banners/campaign-weekend.jpg' WHERE CategoryBannerPremiumUrl IS NULL OR LTRIM(RTRIM(CategoryBannerPremiumUrl))='';
UPDATE WebsiteSettings SET CategoryBannerGraphicsUrl = N'/images/banners/campaign-macbook.jpg' WHERE CategoryBannerGraphicsUrl IS NULL OR LTRIM(RTRIM(CategoryBannerGraphicsUrl))='';
UPDATE WebsiteSettings SET CategoryBannerAccessoryUrl = N'/images/accessories/accessory-keyboard.svg' WHERE CategoryBannerAccessoryUrl IS NULL OR LTRIM(RTRIM(CategoryBannerAccessoryUrl))='';
UPDATE WebsiteSettings SET CategoryBannerComponentUrl = N'/images/accessories/component-ram.svg' WHERE CategoryBannerComponentUrl IS NULL OR LTRIM(RTRIM(CategoryBannerComponentUrl))='';
UPDATE WebsiteSettings SET FooterShippingImage1Url = N'' WHERE FooterShippingImage1Url IS NULL;
UPDATE WebsiteSettings SET FooterShippingImage2Url = N'' WHERE FooterShippingImage2Url IS NULL;
UPDATE WebsiteSettings SET FooterShippingImage3Url = N'' WHERE FooterShippingImage3Url IS NULL;
UPDATE WebsiteSettings SET FooterShippingImage4Url = N'' WHERE FooterShippingImage4Url IS NULL;
UPDATE WebsiteSettings SET FooterQrImageUrl = N'' WHERE FooterQrImageUrl IS NULL;
UPDATE WebsiteSettings SET FooterInfoLink1Text = N'Đánh giá Laptop Hp Probook 650 G4' WHERE FooterInfoLink1Text IS NULL OR LTRIM(RTRIM(FooterInfoLink1Text))='';
UPDATE WebsiteSettings SET FooterInfoLink1Url = N'/TechNews/Details?slug=danh-gia-hp-probook-650-g4-van-phong-man-hinh-to' WHERE FooterInfoLink1Url IS NULL OR LTRIM(RTRIM(FooterInfoLink1Url))='';
UPDATE WebsiteSettings SET FooterInfoLink2Text = N'Laptop xách tay là gì?' WHERE FooterInfoLink2Text IS NULL OR LTRIM(RTRIM(FooterInfoLink2Text))='';
UPDATE WebsiteSettings SET FooterInfoLink2Url = N'/TechNews/Details?slug=laptop-xach-tay-la-gi-nen-mua-khong' WHERE FooterInfoLink2Url IS NULL OR LTRIM(RTRIM(FooterInfoLink2Url))='';
UPDATE WebsiteSettings SET FooterInfoLink3Text = N'Mẹo dùng laptop bền và mượt' WHERE FooterInfoLink3Text IS NULL OR LTRIM(RTRIM(FooterInfoLink3Text))='';
UPDATE WebsiteSettings SET FooterInfoLink3Url = N'/TechNews/Details?slug=thu-thuat-meo-su-dung-laptop-ben-va-muot' WHERE FooterInfoLink3Url IS NULL OR LTRIM(RTRIM(FooterInfoLink3Url))='';
UPDATE WebsiteSettings SET FooterInfoLink4Text = N'Top game phù hợp laptop gaming' WHERE FooterInfoLink4Text IS NULL OR LTRIM(RTRIM(FooterInfoLink4Text))='';
UPDATE WebsiteSettings SET FooterInfoLink4Url = N'/TechNews/Details?slug=top-game-phu-hop-laptop-gaming' WHERE FooterInfoLink4Url IS NULL OR LTRIM(RTRIM(FooterInfoLink4Url))='';
UPDATE WebsiteSettings SET FooterInfoLink5Text = N'Hỏi đáp khi mua laptop xách tay' WHERE FooterInfoLink5Text IS NULL OR LTRIM(RTRIM(FooterInfoLink5Text))='';
UPDATE WebsiteSettings SET FooterInfoLink5Url = N'/TechNews/Details?slug=hoi-dap-khi-mua-laptop-xach-tay' WHERE FooterInfoLink5Url IS NULL OR LTRIM(RTRIM(FooterInfoLink5Url))='';
UPDATE WebsiteSettings SET FooterInfoLink6Text = N'Chính sách bảo hành và hỗ trợ' WHERE FooterInfoLink6Text IS NULL OR LTRIM(RTRIM(FooterInfoLink6Text))='';
UPDATE WebsiteSettings SET FooterInfoLink6Url = N'/support/warranty' WHERE FooterInfoLink6Url IS NULL OR LTRIM(RTRIM(FooterInfoLink6Url))='';
");

        await RunAsync(@"
IF NOT EXISTS (SELECT 1 FROM AdminUsers WHERE LTRIM(RTRIM(LOWER(Username))) = N'admin')
BEGIN
    INSERT INTO AdminUsers(Username, FullName, PasswordHash, IsSuperAdmin, CanViewOrders, CanUpdateOrders, CanCancelOrders, CanViewReviews, CanReplyReviews, CanDeleteReviews, CanManageInventory, CanDeleteInventory, CanImportInventory, CanManageWebsite, IsActive, CreatedAt)
    VALUES(N'admin', N'Quản trị hệ thống', @PasswordHash, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, GETDATE())
END
ELSE
BEGIN
    UPDATE AdminUsers
    SET FullName = ISNULL(NULLIF(FullName, N''), N'Quản trị hệ thống'),
        PasswordHash = CASE WHEN LTRIM(RTRIM(LOWER(Username))) = N'admin' AND ISNULL(NULLIF(PasswordHash, N''), N'') = N'' THEN @PasswordHash ELSE PasswordHash END,
        IsSuperAdmin = 1,
        CanViewOrders = 1,
        CanUpdateOrders = 1,
        CanCancelOrders = 1,
        CanViewReviews = 1,
        CanReplyReviews = 1,
        CanDeleteReviews = 1,
        CanManageInventory = 1,
        CanDeleteInventory = 1,
        CanImportInventory = 1,
        CanManageWebsite = 1,
        IsActive = 1
    WHERE LTRIM(RTRIM(LOWER(Username))) = N'admin'
END
", cmd => cmd.Parameters.AddWithValue("@PasswordHash", HashPassword("123456")));

        await RunAsync(@"
IF NOT EXISTS (SELECT 1 FROM AdminUsers WHERE LTRIM(RTRIM(LOWER(Username))) = N'sale')
BEGIN
    INSERT INTO AdminUsers(Username, FullName, PasswordHash, IsSuperAdmin, CanViewOrders, CanUpdateOrders, CanCancelOrders, CanViewReviews, CanReplyReviews, CanDeleteReviews, CanManageInventory, CanDeleteInventory, CanImportInventory, CanManageWebsite, IsActive, CreatedAt)
    VALUES(N'sale', N'Nhân viên bán hàng', @PasswordHash, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, GETDATE())
END
", cmd => cmd.Parameters.AddWithValue("@PasswordHash", HashPassword("123456")));

        await RunAsync(@"
IF NOT EXISTS (SELECT 1 FROM Products)
BEGIN
    INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
    VALUES
    (N'Acer', N'Mỏng nhẹ', N'Acer Aspire 2025', N'AMD Ryzen 7 7730U', N'24GB', N'512GB SSD', 35990000, 38990000, 17, 1, N'Bán chạy', N'Laptop mỏng nhẹ cho học tập và làm việc.', 8, 1, GETDATE()),
    (N'Acer', N'Mỏng nhẹ', N'Acer Aspire 2026', N'Intel Core 7 150U', N'16GB', N'512GB SSD', 41990000, 43990000, 11, 1, N'Mới', N'Mẫu máy mới cân bằng hiệu năng và di động.', 5, 2, GETDATE()),
    (N'Acer', N'Gaming', N'Acer Nitro V 2024', N'AMD Ryzen 7 7730U', N'24GB', N'512GB SSD', 24990000, 27990000, 8, 0, N'Ưu đãi', N'Laptop gaming phổ thông hiệu năng tốt.', 10, 3, GETDATE()),
    (N'ASUS', N'Gaming', N'ASUS TUF Gaming A15', N'AMD Ryzen 7 8845HS', N'16GB', N'512GB SSD', 26990000, 28990000, 9, 1, N'Hot', N'Laptop gaming mạnh mẽ cho học tập và giải trí.', 7, 4, GETDATE()),
    (N'Dell', N'Văn phòng', N'Dell Inspiron 15', N'Intel Core i5 1335U', N'16GB', N'512GB SSD', 18990000, 20990000, 14, 0, N'Ổn định', N'Laptop văn phòng bền bỉ, tối ưu công việc.', 6, 5, GETDATE()),
    (N'HP', N'Mỏng nhẹ', N'HP Pavilion 14', N'Intel Core i5 1340P', N'16GB', N'512GB SSD', 19990000, 21990000, 12, 0, N'Tiện dụng', N'Lựa chọn phù hợp cho sinh viên và nhân viên văn phòng.', 6, 6, GETDATE()),
    (N'Lenovo', N'Văn phòng', N'Lenovo IdeaPad Slim 5', N'AMD Ryzen 5 7530U', N'16GB', N'512GB SSD', 17990000, 19990000, 15, 0, N'Giá tốt', N'Thiết kế gọn nhẹ, dùng hằng ngày mượt mà.', 5, 7, GETDATE()),
    (N'Apple', N'Cao cấp', N'MacBook Air M2', N'Apple M2', N'8GB', N'256GB SSD', 26990000, 28990000, 6, 1, N'Cao cấp', N'MacBook Air gọn nhẹ, pin tốt, trải nghiệm mượt mà.', 7, 8, GETDATE())
END
");

        await RunAsync(@"
IF NOT EXISTS (SELECT 1 FROM Products WHERE CategoryName IN (N'Phụ kiện', N'Linh kiện'))
BEGIN
    INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
    VALUES
    (N'Phụ kiện', N'Phụ kiện', N'Bàn phím cơ Gaming EK87', N'Bàn phím', N'Led RGB', N'USB', 519000, 699000, 35, 1, N'Hot', N'Bàn phím cơ cho gaming và làm việc.', 26, 1001, GETDATE()),
    (N'Phụ kiện', N'Phụ kiện', N'Chuột Logitech G102 Gen 2', N'Chuột', N'8000 DPI', N'USB', 479000, 579000, 41, 1, N'Mới', N'Chuột gaming quốc dân, bền bỉ và chính xác.', 17, 1002, GETDATE()),
    (N'Phụ kiện', N'Phụ kiện', N'Tai nghe chụp tai H3', N'Tai nghe', N'Over-ear', N'3.5mm', 699000, 890000, 18, 0, N'Giá tốt', N'Tai nghe đàm thoại và giải trí hằng ngày.', 22, 1003, GETDATE()),
    (N'Phụ kiện', N'Phụ kiện', N'Hub chuyển đổi Type-C 6 in 1', N'Hub Type-C', N'6 cổng', N'USB-C', 649000, 990000, 24, 0, N'Ưu đãi', N'Hub chuyển đổi đa năng cho laptop văn phòng.', 35, 1004, GETDATE()),
    (N'Phụ kiện', N'Phụ kiện', N'Đế tản nhiệt laptop 6 quạt', N'Đế tản nhiệt', N'6 fan', N'USB', 399000, 599000, 16, 0, N'Tiện dụng', N'Đế tản nhiệt giúp máy mát hơn khi tải nặng.', 20, 1005, GETDATE()),
    (N'Linh kiện', N'Linh kiện', N'RAM Laptop DDR4 16GB 3200MHz', N'RAM DDR4', N'16GB', N'', 890000, 1190000, 30, 1, N'Bán chạy', N'Nâng cấp RAM cho laptop và PC.', 25, 1010, GETDATE()),
    (N'Linh kiện', N'Linh kiện', N'SSD M.2 NVMe 512GB', N'SSD NVMe', N'', N'512GB', 1190000, 1490000, 28, 1, N'Hot', N'Ổ cứng SSD NVMe tốc độ cao.', 20, 1011, GETDATE()),
    (N'Linh kiện', N'Linh kiện', N'Sạc zin Laptop Dell 65W', N'Sạc laptop', N'65W', N'', 490000, 590000, 22, 0, N'Giảm sốc', N'Sạc zin chính hãng cho laptop Dell.', 17, 1012, GETDATE()),
    (N'Linh kiện', N'Linh kiện', N'Pin Laptop HP ProBook', N'Pin laptop', N'4 cell', N'', 850000, 1050000, 12, 0, N'Mới về', N'Pin thay thế cho dòng HP ProBook.', 19, 1013, GETDATE()),
    (N'Linh kiện', N'Linh kiện', N'RAM PC DDR4 8GB 2666MHz', N'RAM DDR4', N'8GB', N'', 690000, 890000, 26, 0, N'Ổn định', N'RAM PC DDR4 cho máy tính bàn.', 22, 1014, GETDATE())
END
");
        await RunAsync(@"
IF NOT EXISTS (SELECT 1 FROM Products WHERE CategoryName IN (N'Máy tính bộ', N'Máy tính bàn'))
BEGIN
    INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
    VALUES
    (N'HP', N'Máy tính bộ', N'Máy tính bộ HP ProDesk 400 G4 Mini i5 9500T', N'Intel Core i5 9500T', N'16GB', N'256GB SSD', 6990000, 8590000, 9, 1, N'Bán chạy', N'Máy tính bộ HP ProDesk 400 G4 Mini phù hợp văn phòng, siêu gọn và bền bỉ.', 19, 1101, GETDATE()),
    (N'HP', N'Máy tính bộ', N'Máy tính bộ HP ProDesk 400 G5 Mini i5 8500T', N'Intel Core i5 8500T', N'16GB', N'512GB SSD', 7390000, 8990000, 8, 0, N'Hot', N'HP ProDesk mini cho nhu cầu văn phòng và kế toán.', 18, 1102, GETDATE()),
    (N'Dell', N'Máy tính bộ', N'Máy tính bộ Dell OptiPlex 7060 Micro i5 8500T', N'Intel Core i5 8500T', N'16GB', N'256GB SSD', 7190000, 8790000, 7, 0, N'Ưu đãi', N'Dell OptiPlex micro tiết kiệm diện tích, vận hành ổn định.', 18, 1103, GETDATE()),
    (N'Dell', N'Máy tính bộ', N'Máy tính bộ Dell Precision 3430 SFF i7 8700', N'Intel Core i7 8700', N'16GB', N'512GB SSD', 9990000, 11990000, 6, 1, N'Doanh nghiệp', N'Dell Precision SFF phục vụ kỹ thuật, dựng hình và văn phòng nâng cao.', 17, 1104, GETDATE()),
    (N'Lenovo', N'Máy tính bộ', N'Máy tính bộ Lenovo ThinkCentre M720q Tiny i5 9500T', N'Intel Core i5 9500T', N'16GB', N'512GB SSD', 7590000, 9190000, 6, 0, N'Mới về', N'ThinkCentre Tiny nhỏ gọn, phù hợp quầy thu ngân và văn phòng.', 17, 1105, GETDATE()),
    (N'Lenovo', N'Máy tính bộ', N'Máy tính bộ Lenovo ThinkStation P330 Tiny i7 9700', N'Intel Core i7 9700', N'32GB', N'1TB SSD', 14990000, 16990000, 4, 1, N'Cao cấp', N'ThinkStation cho đồ họa 2D/3D và kỹ thuật chuyên sâu.', 12, 1106, GETDATE()),
    (N'HP', N'Máy tính bộ', N'Máy tính bộ HP EliteDesk 800 G5 Mini i7 9700T', N'Intel Core i7 9700T', N'16GB', N'512GB SSD', 10990000, 12990000, 5, 0, N'Ổn định', N'EliteDesk mini hiệu năng tốt cho doanh nghiệp và quản trị.', 15, 1107, GETDATE()),
    (N'ASUS', N'Máy tính bộ', N'Máy tính bộ ASUS ExpertCenter D5 SFF i5 12400', N'Intel Core i5 12400', N'16GB', N'512GB SSD', 11990000, 13990000, 6, 0, N'Chính hãng', N'ExpertCenter desktop chính hãng cho công việc hàng ngày.', 14, 1108, GETDATE()),
    (N'Acer', N'Máy tính bộ', N'Máy tính bộ Acer Veriton X2690G i5 12400', N'Intel Core i5 12400', N'16GB', N'512GB SSD', 11490000, 13490000, 6, 0, N'Văn phòng', N'Acer Veriton phù hợp môi trường văn phòng và giáo dục.', 15, 1109, GETDATE()),
    (N'MSI', N'Máy tính bộ', N'Máy tính bộ MSI PRO DP130 i5 13400', N'Intel Core i5 13400', N'16GB', N'512GB SSD', 13990000, 15990000, 5, 1, N'Hiệu năng', N'MSI PRO desktop cho văn phòng nâng cao và sáng tạo nội dung.', 13, 1110, GETDATE())
END
");

        await RunAsync(@"
IF NOT EXISTS (SELECT 1 FROM ProductImages)
BEGIN
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder)
    SELECT ProductId, ImageUrl, DisplayOrder
    FROM (
        SELECT p.ProductId, v.ImageUrl, v.DisplayOrder
        FROM Products p
        CROSS APPLY (VALUES
            (CASE 
                WHEN p.Brand = N'Acer' AND p.ProductName LIKE N'%2025%' THEN N'/images/products/acer/acer-001.jpg'
                WHEN p.Brand = N'Acer' AND p.ProductName LIKE N'%2026%' THEN N'/images/products/acer/acer-002.jpg'
                WHEN p.Brand = N'Acer' AND p.ProductName LIKE N'%Nitro%' THEN N'/images/products/acer/acer-003.jpg'
                WHEN p.Brand = N'ASUS' THEN N'/images/products/asus/asus-001.jpg'
                WHEN p.Brand = N'Dell' THEN N'/images/products/dell/dell-001.jpg'
                WHEN p.Brand = N'HP' THEN N'/images/products/hp/hp-001.jpg'
                WHEN p.Brand = N'Lenovo' THEN N'/images/products/lenovo/lenovo-001.jpg'
                WHEN p.Brand = N'Apple' THEN N'/images/products/macbook/macbook-001.jpg'
                ELSE N'/images/banners/slide1.svg' END, 1),
            (CASE 
                WHEN p.Brand = N'Acer' THEN N'/images/products/acer/acer-004.jpg'
                WHEN p.Brand = N'ASUS' THEN N'/images/products/asus/asus-002.jpg'
                WHEN p.Brand = N'Dell' THEN N'/images/products/dell/dell-002.jpg'
                WHEN p.Brand = N'HP' THEN N'/images/products/hp/hp-002.jpg'
                WHEN p.Brand = N'Lenovo' THEN N'/images/products/lenovo/lenovo-002.jpg'
                WHEN p.Brand = N'Apple' THEN N'/images/products/macbook/macbook-002.jpg'
                ELSE N'/images/banners/slide2.svg' END, 2),
            (CASE 
                WHEN p.Brand = N'Acer' THEN N'/images/products/acer/acer-005.jpg'
                WHEN p.Brand = N'ASUS' THEN N'/images/products/asus/asus-003.jpg'
                WHEN p.Brand = N'Dell' THEN N'/images/products/dell/dell-003.jpg'
                WHEN p.Brand = N'HP' THEN N'/images/products/hp/hp-003.jpg'
                WHEN p.Brand = N'Lenovo' THEN N'/images/products/lenovo/lenovo-003.jpg'
                WHEN p.Brand = N'Apple' THEN N'/images/products/macbook/macbook-003.jpg'
                ELSE N'/images/banners/slide3.svg' END, 3)
        ) v(ImageUrl, DisplayOrder)
    ) x
END

UPDATE pi
SET pi.ImageUrl = CASE
    WHEN p.Brand = N'Acer' AND p.ProductName LIKE N'%2025%' THEN N'/images/products/acer/acer-001.jpg'
    WHEN p.Brand = N'Acer' AND p.ProductName LIKE N'%2026%' THEN N'/images/products/acer/acer-002.jpg'
    WHEN p.Brand = N'Acer' AND p.ProductName LIKE N'%Nitro%' THEN N'/images/products/acer/acer-003.jpg'
    WHEN p.Brand = N'ASUS' THEN N'/images/products/asus/asus-001.jpg'
    WHEN p.Brand = N'Dell' THEN N'/images/products/dell/dell-001.jpg'
    WHEN p.Brand = N'HP' THEN N'/images/products/hp/hp-001.jpg'
    WHEN p.Brand = N'Lenovo' THEN N'/images/products/lenovo/lenovo-001.jpg'
    WHEN p.Brand = N'Apple' THEN N'/images/products/macbook/macbook-001.jpg'
    ELSE N'/images/products/acer/acer-001.jpg'
END
FROM ProductImages pi
INNER JOIN Products p ON p.ProductId = pi.ProductId
WHERE pi.DisplayOrder = 1 AND (
    pi.ImageUrl IS NULL OR LTRIM(RTRIM(pi.ImageUrl)) = '' OR
    pi.ImageUrl LIKE '/images/products/% %.jpg' OR
    pi.ImageUrl LIKE 'https://placehold.co%'
);

INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder)
SELECT p.ProductId,
       CASE
           WHEN p.Brand = N'Acer' AND p.ProductName LIKE N'%2025%' THEN N'/images/products/acer/acer-001.jpg'
           WHEN p.Brand = N'Acer' AND p.ProductName LIKE N'%2026%' THEN N'/images/products/acer/acer-002.jpg'
           WHEN p.Brand = N'Acer' AND p.ProductName LIKE N'%Nitro%' THEN N'/images/products/acer/acer-003.jpg'
           WHEN p.Brand = N'ASUS' THEN N'/images/products/asus/asus-001.jpg'
           WHEN p.Brand = N'Dell' THEN N'/images/products/dell/dell-001.jpg'
           WHEN p.Brand = N'HP' THEN N'/images/products/hp/hp-001.jpg'
           WHEN p.Brand = N'Lenovo' THEN N'/images/products/lenovo/lenovo-001.jpg'
           WHEN p.Brand = N'Apple' THEN N'/images/products/macbook/macbook-001.jpg'
           ELSE N'/images/products/acer/acer-001.jpg'
       END,
       1
FROM Products p
WHERE NOT EXISTS (SELECT 1 FROM ProductImages pi WHERE pi.ProductId = p.ProductId);
");

        await RunAsync(@"
IF NOT EXISTS (SELECT 1 FROM ProductReviews)
BEGIN
    INSERT INTO ProductReviews(ProductId, ReviewerName, Rating, CommentText, ImageUrl, ReplyText, ReplyCreatedAt, CreatedAt)
    SELECT ProductId, ReviewerName, Rating, CommentText, N'', ReplyText, GETDATE(), GETDATE()
    FROM (
        SELECT p.ProductId, N'Gia Hân' AS ReviewerName, 5 AS Rating, N'Máy đẹp, lên hình đúng như tư vấn, dùng học online và làm văn phòng rất ổn.' AS CommentText, N'Cảm ơn chị Hân đã tin tưởng. Shop luôn hỗ trợ thêm khi chị cần nâng cấp hoặc bảo hành.' AS ReplyText
        FROM Products p WHERE p.ProductName = N'Dell Inspiron 15'
        UNION ALL
        SELECT p.ProductId, N'Tuấn Khang', 4, N'Đóng gói chắc chắn, máy chạy mượt. Phần pin dùng ổn trong tầm giá.', N'Cảm ơn anh Khang đã phản hồi. Shop sẽ tiếp tục cải thiện dịch vụ giao hàng và tư vấn.'
        FROM Products p WHERE p.ProductName = N'HP Pavilion 14'
        UNION ALL
        SELECT p.ProductId, N'Ngọc Mai', 5, N'Mua cho em trai học thiết kế cơ bản, máy gọn và thao tác nhanh, nhân viên tư vấn dễ hiểu.', N'Cảm ơn chị Mai. Chúc em mình học tập hiệu quả cùng sản phẩm mới.'
        FROM Products p WHERE p.ProductName = N'ASUS TUF Gaming A15'
    ) x
END
");

        await SeedExtendedCatalogAsync();

        await RunAsync(@"
IF OBJECT_ID('Products','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName = N'PCAPT0174 | ASUS ROG RTX 5090 Ultra 9 285K 96GB 1TB')
        INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
        VALUES (N'ASUS', N'Máy tính bộ', N'PCAPT0174 | ASUS ROG RTX 5090 Ultra 9 285K 96GB 1TB', N'Ultra 9 285K / RTX 5090 32GB AI', N'96GB DDR5', N'1TB SSD', 51296000, 54990000, 3, 1, N'Mã 51296', N'Powered by ASUS 2026.', 7, 1201, GETDATE());
    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName = N'PCAPT0250 | ASUS RTX 4060 Game Master i5 14400F 16GB 500GB')
        INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
        VALUES (N'ASUS', N'Máy tính bộ', N'PCAPT0250 | ASUS RTX 4060 Game Master i5 14400F 16GB 500GB', N'i5 14400F / RTX 4060 8GB AI', N'16GB RAM', N'500GB SSD', 53229000, 55990000, 5, 1, N'Mã 53229', N'PSU 650W - Powered by ASUS 2026.', 5, 1202, GETDATE());
    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName = N'PCAPT0266 | ASUS RTX 5050 Game Master i5 14400F 16GB 500GB')
        INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
        VALUES (N'ASUS', N'Máy tính bộ', N'PCAPT0266 | ASUS RTX 5050 Game Master i5 14400F 16GB 500GB', N'i5 14400F / RTX 5050 8GB AI', N'16GB RAM', N'500GB SSD', 53624000, 56990000, 5, 1, N'Mã 53624', N'PSU 650W - Powered by ASUS 2026.', 6, 1203, GETDATE());
    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName = N'PCAPT0275 | MSI RTX 5070 Ti Red Dragon i5 14400F 16GB 1TB')
        INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
        VALUES (N'MSI', N'Máy tính bộ', N'PCAPT0275 | MSI RTX 5070 Ti Red Dragon i5 14400F 16GB 1TB', N'i5 14400F / RTX 5070 Ti 16GB', N'16GB DDR5', N'1TB SSD', 54176000, 57990000, 4, 1, N'Mã 54176', N'WC - Powered by MSI Q1 2026.', 7, 1204, GETDATE());
    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName = N'PCAPT0276 | MSI RTX 5080 Red Dragon Ultra 7 265KF 32GB 1TB')
        INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
        VALUES (N'MSI', N'Máy tính bộ', N'PCAPT0276 | MSI RTX 5080 Red Dragon Ultra 7 265KF 32GB 1TB', N'Ultra 7 265KF / RTX 5080 16GB', N'32GB RAM', N'1TB SSD', 54177000, 58990000, 3, 1, N'Mã 54177', N'Powered by MSI Q1 2026.', 8, 1205, GETDATE());
    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName = N'PCAPT0362 | ASUS TUF RTX 5060 Ti i5 14400F 16GB 500GB')
        INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
        VALUES (N'ASUS', N'Máy tính bộ', N'PCAPT0362 | ASUS TUF RTX 5060 Ti i5 14400F 16GB 500GB', N'i5 14400F / RTX 5060 Ti 8GB AI', N'16GB RAM', N'500GB SSD', 55271000, 57990000, 6, 1, N'Mã 55271', N'Powered by ASUS.', 5, 1206, GETDATE());
    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName = N'Mã 55452 | ASUS TUF RTX 5060 Ti i5 12400F 8GB 500GB')
        INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
        VALUES (N'ASUS', N'Máy tính bộ', N'Mã 55452 | ASUS TUF RTX 5060 Ti i5 12400F 8GB 500GB', N'i5 12400F / RTX 5060 Ti 8GB AI', N'8GB RAM', N'500GB SSD', 55452000, 57990000, 8, 1, N'Mã 55452', N'WF/BL - Powered by ASUS.', 4, 1207, GETDATE());
    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName = N'Mã 55453 | ASUS TUF RTX 5060 Ti i5 12400F 16GB 500GB')
        INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
        VALUES (N'ASUS', N'Máy tính bộ', N'Mã 55453 | ASUS TUF RTX 5060 Ti i5 12400F 16GB 500GB', N'i5 12400F / RTX 5060 Ti 8GB AI', N'16GB RAM', N'500GB SSD', 55453000, 58990000, 6, 1, N'Mã 55453', N'WF/BL - Powered by ASUS.', 6, 1208, GETDATE());
    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName = N'Mã 55454 | ASUS TUF RTX 5060 Ti Ultra 5 225F 16GB 500GB')
        INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
        VALUES (N'ASUS', N'Máy tính bộ', N'Mã 55454 | ASUS TUF RTX 5060 Ti Ultra 5 225F 16GB 500GB', N'Ultra 5 225F / RTX 5060 Ti 8GB AI', N'16GB RAM', N'500GB SSD', 55454000, 58990000, 5, 1, N'Mã 55454', N'Powered by ASUS.', 6, 1209, GETDATE());

    UPDATE Products SET IsFeatured = 1 WHERE ProductName IN (
        N'PCAPT0174 | ASUS ROG RTX 5090 Ultra 9 285K 96GB 1TB',
        N'PCAPT0250 | ASUS RTX 4060 Game Master i5 14400F 16GB 500GB',
        N'PCAPT0266 | ASUS RTX 5050 Game Master i5 14400F 16GB 500GB',
        N'PCAPT0275 | MSI RTX 5070 Ti Red Dragon i5 14400F 16GB 1TB',
        N'PCAPT0276 | MSI RTX 5080 Red Dragon Ultra 7 265KF 32GB 1TB',
        N'PCAPT0362 | ASUS TUF RTX 5060 Ti i5 14400F 16GB 500GB',
        N'Mã 55452 | ASUS TUF RTX 5060 Ti i5 12400F 8GB 500GB',
        N'Mã 55453 | ASUS TUF RTX 5060 Ti i5 12400F 16GB 500GB',
        N'Mã 55454 | ASUS TUF RTX 5060 Ti Ultra 5 225F 16GB 500GB'
    );
END
");

        await RunAsync(@"
IF OBJECT_ID('ProductImages','U') IS NOT NULL
BEGIN
    DELETE pi FROM ProductImages pi
    INNER JOIN Products p ON p.ProductId = pi.ProductId
    WHERE p.ProductName IN (
        N'PCAPT0174 | ASUS ROG RTX 5090 Ultra 9 285K 96GB 1TB',
        N'PCAPT0250 | ASUS RTX 4060 Game Master i5 14400F 16GB 500GB',
        N'PCAPT0266 | ASUS RTX 5050 Game Master i5 14400F 16GB 500GB',
        N'PCAPT0275 | MSI RTX 5070 Ti Red Dragon i5 14400F 16GB 1TB',
        N'PCAPT0276 | MSI RTX 5080 Red Dragon Ultra 7 265KF 32GB 1TB',
        N'PCAPT0362 | ASUS TUF RTX 5060 Ti i5 14400F 16GB 500GB',
        N'Mã 55452 | ASUS TUF RTX 5060 Ti i5 12400F 8GB 500GB',
        N'Mã 55453 | ASUS TUF RTX 5060 Ti i5 12400F 16GB 500GB',
        N'Mã 55454 | ASUS TUF RTX 5060 Ti Ultra 5 225F 16GB 500GB'
    );

    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-001.jpg', 1 FROM Products WHERE ProductName = N'PCAPT0174 | ASUS ROG RTX 5090 Ultra 9 285K 96GB 1TB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-002.jpg', 1 FROM Products WHERE ProductName = N'PCAPT0250 | ASUS RTX 4060 Game Master i5 14400F 16GB 500GB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-003.jpg', 1 FROM Products WHERE ProductName = N'PCAPT0266 | ASUS RTX 5050 Game Master i5 14400F 16GB 500GB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-004.jpg', 1 FROM Products WHERE ProductName = N'PCAPT0275 | MSI RTX 5070 Ti Red Dragon i5 14400F 16GB 1TB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-005.jpg', 1 FROM Products WHERE ProductName = N'PCAPT0276 | MSI RTX 5080 Red Dragon Ultra 7 265KF 32GB 1TB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-006.jpg', 1 FROM Products WHERE ProductName = N'PCAPT0362 | ASUS TUF RTX 5060 Ti i5 14400F 16GB 500GB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-007.jpg', 1 FROM Products WHERE ProductName = N'Mã 55452 | ASUS TUF RTX 5060 Ti i5 12400F 8GB 500GB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-008.jpg', 1 FROM Products WHERE ProductName = N'Mã 55453 | ASUS TUF RTX 5060 Ti i5 12400F 16GB 500GB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-009.jpg', 1 FROM Products WHERE ProductName = N'Mã 55454 | ASUS TUF RTX 5060 Ti Ultra 5 225F 16GB 500GB';
END
");


        await RunAsync(@"
IF OBJECT_ID('Products','U') IS NOT NULL
BEGIN
    UPDATE Products
    SET ProductName = N'PCAPT0174 | ASUS ROG RTX 5090 Ultra 9 285K 96GB 1TB',
        Brand = N'ASUS',
        CategoryName = N'Máy tính bộ',
        Cpu = N'Ultra 9 285K / RTX 5090 32GB AI',
        Ram = N'96GB DDR5',
        Ssd = N'1TB SSD',
        Price = 51296000,
        OldPrice = 54990000,
        StockQty = 3,
        IsFeatured = 1,
        BadgeText = N'Mã 51296',
        SortOrder = 1201
    WHERE ProductName = N'PC ASUS ROG Game Master RTX 5090 Ultra 9 285K 96GB 1TB';

    UPDATE Products
    SET ProductName = N'PCAPT0250 | ASUS RTX 4060 Game Master i5 14400F 16GB 500GB',
        Brand = N'ASUS',
        CategoryName = N'Máy tính bộ',
        Cpu = N'i5 14400F / RTX 4060 8GB AI',
        Ram = N'16GB RAM',
        Ssd = N'500GB SSD',
        Price = 53229000,
        OldPrice = 55990000,
        StockQty = 5,
        IsFeatured = 1,
        BadgeText = N'Mã 53229',
        SortOrder = 1202
    WHERE ProductName = N'PC ASUS Game Master RTX 4060 i5 14400F 16GB 500GB';

    UPDATE Products
    SET ProductName = N'PCAPT0266 | ASUS RTX 5050 Game Master i5 14400F 16GB 500GB',
        Brand = N'ASUS',
        CategoryName = N'Máy tính bộ',
        Cpu = N'i5 14400F / RTX 5050 8GB AI',
        Ram = N'16GB RAM',
        Ssd = N'500GB SSD',
        Price = 53624000,
        OldPrice = 56990000,
        StockQty = 5,
        IsFeatured = 1,
        BadgeText = N'Mã 53624',
        SortOrder = 1203
    WHERE ProductName = N'PC ASUS Game Master RTX 5050 i5 14400F 16GB 500GB';

    UPDATE Products
    SET ProductName = N'PCAPT0275 | MSI RTX 5070 Ti Red Dragon i5 14400F 16GB 1TB',
        Brand = N'MSI',
        CategoryName = N'Máy tính bộ',
        Cpu = N'i5 14400F / RTX 5070 Ti 16GB',
        Ram = N'16GB DDR5',
        Ssd = N'1TB SSD',
        Price = 54176000,
        OldPrice = 57990000,
        StockQty = 4,
        IsFeatured = 1,
        BadgeText = N'Mã 54176',
        SortOrder = 1204
    WHERE ProductName = N'PC MSI Red Dragon RTX 5070 Ti i5 14400F 16GB DDR5 1TB';

    UPDATE Products
    SET ProductName = N'PCAPT0276 | MSI RTX 5080 Red Dragon Ultra 7 265KF 32GB 1TB',
        Brand = N'MSI',
        CategoryName = N'Máy tính bộ',
        Cpu = N'Ultra 7 265KF / RTX 5080 16GB',
        Ram = N'32GB RAM',
        Ssd = N'1TB SSD',
        Price = 54177000,
        OldPrice = 58990000,
        StockQty = 3,
        IsFeatured = 1,
        BadgeText = N'Mã 54177',
        SortOrder = 1205
    WHERE ProductName = N'PC MSI Red Dragon RTX 5080 Ultra 7 265KF 32GB 1TB';

    UPDATE Products
    SET ProductName = N'PCAPT0362 | ASUS TUF RTX 5060 Ti i5 14400F 16GB 500GB',
        Brand = N'ASUS',
        CategoryName = N'Máy tính bộ',
        Cpu = N'i5 14400F / RTX 5060 Ti 8GB AI',
        Ram = N'16GB RAM',
        Ssd = N'500GB SSD',
        Price = 55271000,
        OldPrice = 57990000,
        StockQty = 6,
        IsFeatured = 1,
        BadgeText = N'Mã 55271',
        SortOrder = 1206
    WHERE ProductName = N'PC ASUS TUF Game Master RTX 5060 Ti i5 14400F 16GB 500GB';

    UPDATE Products
    SET ProductName = N'Mã 55452 | ASUS TUF RTX 5060 Ti i5 12400F 8GB 500GB',
        Brand = N'ASUS',
        CategoryName = N'Máy tính bộ',
        Cpu = N'i5 12400F / RTX 5060 Ti 8GB AI',
        Ram = N'8GB RAM',
        Ssd = N'500GB SSD',
        Price = 55452000,
        OldPrice = 57990000,
        StockQty = 8,
        IsFeatured = 1,
        BadgeText = N'Mã 55452',
        SortOrder = 1207
    WHERE ProductName = N'PC ASUS TUF Game Master RTX 5060 Ti i5 12400F 8GB 500GB';

    UPDATE Products
    SET ProductName = N'Mã 55453 | ASUS TUF RTX 5060 Ti i5 12400F 16GB 500GB',
        Brand = N'ASUS',
        CategoryName = N'Máy tính bộ',
        Cpu = N'i5 12400F / RTX 5060 Ti 8GB AI',
        Ram = N'16GB RAM',
        Ssd = N'500GB SSD',
        Price = 55453000,
        OldPrice = 58990000,
        StockQty = 6,
        IsFeatured = 1,
        BadgeText = N'Mã 55453',
        SortOrder = 1208
    WHERE ProductName = N'PC ASUS TUF Game Master RTX 5060 Ti i5 12400F 16GB 500GB';

    UPDATE Products
    SET ProductName = N'Mã 55454 | ASUS TUF RTX 5060 Ti Ultra 5 225F 16GB 500GB',
        Brand = N'ASUS',
        CategoryName = N'Máy tính bộ',
        Cpu = N'Ultra 5 225F / RTX 5060 Ti 8GB AI',
        Ram = N'16GB RAM',
        Ssd = N'500GB SSD',
        Price = 55454000,
        OldPrice = 58990000,
        StockQty = 5,
        IsFeatured = 1,
        BadgeText = N'Mã 55454',
        SortOrder = 1209
    WHERE ProductName = N'PC ASUS TUF Game Master RTX 5060 Ti Ultra 5 225F 16GB 500GB';
END
");

        await RunAsync(@"
IF OBJECT_ID('ProductImages','U') IS NOT NULL
BEGIN
    DELETE pi
    FROM ProductImages pi
    INNER JOIN Products p ON p.ProductId = pi.ProductId
    WHERE p.ProductName IN (
        N'PCAPT0174 | ASUS ROG RTX 5090 Ultra 9 285K 96GB 1TB',
        N'PCAPT0250 | ASUS RTX 4060 Game Master i5 14400F 16GB 500GB',
        N'PCAPT0266 | ASUS RTX 5050 Game Master i5 14400F 16GB 500GB',
        N'PCAPT0275 | MSI RTX 5070 Ti Red Dragon i5 14400F 16GB 1TB',
        N'PCAPT0276 | MSI RTX 5080 Red Dragon Ultra 7 265KF 32GB 1TB',
        N'PCAPT0362 | ASUS TUF RTX 5060 Ti i5 14400F 16GB 500GB',
        N'Mã 55452 | ASUS TUF RTX 5060 Ti i5 12400F 8GB 500GB',
        N'Mã 55453 | ASUS TUF RTX 5060 Ti i5 12400F 16GB 500GB',
        N'Mã 55454 | ASUS TUF RTX 5060 Ti Ultra 5 225F 16GB 500GB'
    );

    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-001.jpg', 1 FROM Products WHERE ProductName = N'PCAPT0174 | ASUS ROG RTX 5090 Ultra 9 285K 96GB 1TB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-002.jpg', 1 FROM Products WHERE ProductName = N'PCAPT0250 | ASUS RTX 4060 Game Master i5 14400F 16GB 500GB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-003.jpg', 1 FROM Products WHERE ProductName = N'PCAPT0266 | ASUS RTX 5050 Game Master i5 14400F 16GB 500GB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-004.jpg', 1 FROM Products WHERE ProductName = N'PCAPT0275 | MSI RTX 5070 Ti Red Dragon i5 14400F 16GB 1TB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-005.jpg', 1 FROM Products WHERE ProductName = N'PCAPT0276 | MSI RTX 5080 Red Dragon Ultra 7 265KF 32GB 1TB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-006.jpg', 1 FROM Products WHERE ProductName = N'PCAPT0362 | ASUS TUF RTX 5060 Ti i5 14400F 16GB 500GB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-007.jpg', 1 FROM Products WHERE ProductName = N'Mã 55452 | ASUS TUF RTX 5060 Ti i5 12400F 8GB 500GB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-008.jpg', 1 FROM Products WHERE ProductName = N'Mã 55453 | ASUS TUF RTX 5060 Ti i5 12400F 16GB 500GB';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/desktops/desktops-009.jpg', 1 FROM Products WHERE ProductName = N'Mã 55454 | ASUS TUF RTX 5060 Ti Ultra 5 225F 16GB 500GB';
END
");

        await SyncBrandProductImagesAsync();

        await RunAsync(@"
IF OBJECT_ID('Products','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName = N'Dell Precision 3590 Workstation')
        INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
        VALUES (N'Dell', N'Máy trạm', N'Dell Precision 3590 Workstation', N'Intel Core Ultra 7 165H', N'32GB RAM', N'1TB SSD', 42990000, 45990000, 6, 1, N'Workstation', N'Laptop máy trạm cho CAD, 3D, render và kỹ thuật.', 7, 1010, GETDATE());

    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName = N'HP ZBook Power G11')
        INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
        VALUES (N'HP', N'Máy trạm', N'HP ZBook Power G11', N'Intel Core Ultra 7 155H', N'32GB RAM', N'1TB SSD', 43990000, 46990000, 5, 1, N'Đồ họa', N'Máy trạm bền bỉ cho sáng tạo nội dung và công việc kỹ thuật.', 6, 1011, GETDATE());

    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName = N'Lenovo ThinkPad P16 Gen 2')
        INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
        VALUES (N'Lenovo', N'Máy trạm', N'Lenovo ThinkPad P16 Gen 2', N'Intel Core i9 13980HX', N'32GB RAM', N'1TB SSD', 48990000, 52990000, 4, 1, N'3D / CAD', N'Workstation hiệu năng cao cho CAD, BIM và đồ họa chuyên nghiệp.', 8, 1012, GETDATE());

    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName = N'ASUS ProArt Studiobook 16')
        INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
        VALUES (N'ASUS', N'Máy trạm', N'ASUS ProArt Studiobook 16', N'AMD Ryzen 9 7945HX', N'32GB RAM', N'1TB SSD', 45990000, 49990000, 4, 1, N'Sáng tạo', N'Lựa chọn mạnh cho edit video, thiết kế và AI creator.', 8, 1013, GETDATE());

    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName = N'MSI Creator Z17 HX Studio')
        INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
        VALUES (N'MSI', N'Máy trạm', N'MSI Creator Z17 HX Studio', N'Intel Core i9 13950HX', N'32GB RAM', N'1TB SSD', 46990000, 50990000, 3, 1, N'Studio', N'Laptop cho creator, 3D và xử lý hình ảnh chuyên sâu.', 8, 1014, GETDATE());

    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductName = N'Acer ConceptD 5 Pro')
        INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
        VALUES (N'Acer', N'Máy trạm', N'Acer ConceptD 5 Pro', N'Intel Core i7 13700H', N'16GB RAM', N'1TB SSD', 37990000, 40990000, 4, 1, N'Creator', N'Máy trạm cho dựng hình, thiết kế đồ họa và render ổn định.', 7, 1015, GETDATE());
END
");

        await RunAsync(@"
IF OBJECT_ID('ProductImages','U') IS NOT NULL
BEGIN
    DELETE pi
    FROM ProductImages pi
    INNER JOIN Products p ON p.ProductId = pi.ProductId
    WHERE p.ProductName IN (
        N'Dell Precision 3590 Workstation',
        N'HP ZBook Power G11',
        N'Lenovo ThinkPad P16 Gen 2',
        N'ASUS ProArt Studiobook 16',
        N'MSI Creator Z17 HX Studio',
        N'Acer ConceptD 5 Pro'
    );

    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/dell/dell-032.jpg', 1 FROM Products WHERE ProductName = N'Dell Precision 3590 Workstation';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/hp/hp-001.jpg', 1 FROM Products WHERE ProductName = N'HP ZBook Power G11';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/thinkbook/thinkbook-013.jpg', 1 FROM Products WHERE ProductName = N'Lenovo ThinkPad P16 Gen 2';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/asus/asus-001.jpg', 1 FROM Products WHERE ProductName = N'ASUS ProArt Studiobook 16';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/msi/msi-001.jpg', 1 FROM Products WHERE ProductName = N'MSI Creator Z17 HX Studio';
    INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) SELECT ProductId, N'/images/products/acer/acer-001.jpg', 1 FROM Products WHERE ProductName = N'Acer ConceptD 5 Pro';
END
");

        await RunAsync(@"
IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderCode IN (N'DH20260429101501', N'DH20260429112842'))
BEGIN
    INSERT INTO Orders(OrderCode, CustomerName, Phone, AddressLine, Note, PaymentMethod, IsPaid, OrderStatus, TotalAmount, CreatedAt)
    VALUES
    (N'DH20260429101501', N'Minh Anh', N'0912345678', N'25 Nguyễn Văn Trỗi, Phường 2, Đà Lạt', N'Giao trong giờ hành chính, gọi trước khi giao.', N'BankTransfer', 1, N'Đã thanh toán - đang xử lý', 42990000, GETDATE()),
    (N'DH20260429112842', N'Hoàng Phúc', N'0987654321', N'118 Lý Thường Kiệt, Quận Tân Bình, TP. Hồ Chí Minh', N'Cần xuất hóa đơn điện tử cho công ty.', N'PayLater', 0, N'Chờ xác nhận', 45990000, GETDATE())
END
");

        await RunAsync(@"
IF NOT EXISTS (SELECT 1 FROM OrderItems WHERE ProductName IN (N'Dell Precision 3590 Workstation', N'ASUS ProArt Studiobook 16'))
BEGIN
    INSERT INTO OrderItems(OrderId, ProductId, ProductName, UnitPrice, Quantity, LineTotal)
    SELECT o.OrderId, p.ProductId, p.ProductName, 42990000, 1, 42990000
    FROM Orders o
    INNER JOIN Products p ON p.ProductName = N'Dell Precision 3590 Workstation'
    WHERE o.OrderCode = N'DH20260429101501'
      AND NOT EXISTS (SELECT 1 FROM OrderItems oi WHERE oi.OrderId = o.OrderId AND oi.ProductId = p.ProductId);

    INSERT INTO OrderItems(OrderId, ProductId, ProductName, UnitPrice, Quantity, LineTotal)
    SELECT o.OrderId, p.ProductId, p.ProductName, 45990000, 1, 45990000
    FROM Orders o
    INNER JOIN Products p ON p.ProductName = N'ASUS ProArt Studiobook 16'
    WHERE o.OrderCode = N'DH20260429112842'
      AND NOT EXISTS (SELECT 1 FROM OrderItems oi WHERE oi.OrderId = o.OrderId AND oi.ProductId = p.ProductId);
END
");

    }

    private async Task SeedExtendedCatalogAsync()
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM Products", conn))
        {
            var existing = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            if (existing >= 220) return;
        }

        var plans = new[]
        {
            new { Brand = "HP", Category = "Máy tính bộ", Prefix = "HP ProDesk", Cpu = new[] { "Intel Core i5 9500T", "Intel Core i5 8500T", "Intel Core i7 9700T" }, Ram = new[] { "16GB", "32GB" }, Ssd = new[] { "256GB SSD", "512GB SSD", "1TB SSD" }, BasePrice = 6990000m, Count = 14 },
            new { Brand = "Dell", Category = "Máy tính bộ", Prefix = "Dell OptiPlex", Cpu = new[] { "Intel Core i5 8500T", "Intel Core i5 9500T", "Intel Core i7 9700" }, Ram = new[] { "16GB", "32GB" }, Ssd = new[] { "256GB SSD", "512GB SSD", "1TB SSD" }, BasePrice = 7490000m, Count = 12 },
            new { Brand = "Lenovo", Category = "Máy tính bộ", Prefix = "Lenovo ThinkCentre", Cpu = new[] { "Intel Core i5 9500T", "Intel Core i7 9700", "Intel Core i5 12400" }, Ram = new[] { "16GB", "32GB" }, Ssd = new[] { "256GB SSD", "512GB SSD", "1TB SSD" }, BasePrice = 7690000m, Count = 12 },
            new { Brand = "Acer", Category = "Mỏng nhẹ", Prefix = "Acer Aspire", Cpu = new[] { "Intel Core i5 1335U", "Intel Core i7 1355U", "AMD Ryzen 5 7530U", "AMD Ryzen 7 7730U" }, Ram = new[] { "8GB", "16GB", "24GB" }, Ssd = new[] { "512GB SSD", "1TB SSD" }, BasePrice = 14990000m, Count = 30 },
            new { Brand = "Acer", Category = "Gaming", Prefix = "Acer Nitro", Cpu = new[] { "Intel Core i5 13420H", "Intel Core i7 13620H", "AMD Ryzen 7 7840HS" }, Ram = new[] { "16GB", "24GB", "32GB" }, Ssd = new[] { "512GB SSD", "1TB SSD" }, BasePrice = 21990000m, Count = 18 },
            new { Brand = "ASUS", Category = "Gaming", Prefix = "ASUS TUF Gaming", Cpu = new[] { "Intel Core i5 12500H", "Intel Core i7 13620H", "AMD Ryzen 7 8845HS" }, Ram = new[] { "16GB", "24GB", "32GB" }, Ssd = new[] { "512GB SSD", "1TB SSD" }, BasePrice = 22990000m, Count = 28 },
            new { Brand = "ASUS", Category = "Văn phòng", Prefix = "ASUS Vivobook", Cpu = new[] { "Intel Core i5 1335U", "Intel Core 7 150U", "AMD Ryzen 5 7530U" }, Ram = new[] { "8GB", "16GB" }, Ssd = new[] { "512GB SSD", "1TB SSD" }, BasePrice = 15990000m, Count = 16 },
            new { Brand = "Dell", Category = "Văn phòng", Prefix = "Dell Inspiron", Cpu = new[] { "Intel Core i5 1335U", "Intel Core i7 1355U", "Intel Core Ultra 5 125U" }, Ram = new[] { "8GB", "16GB" }, Ssd = new[] { "512GB SSD", "1TB SSD" }, BasePrice = 16990000m, Count = 26 },
            new { Brand = "Dell", Category = "Cao cấp", Prefix = "Dell XPS", Cpu = new[] { "Intel Core Ultra 7 155H", "Intel Core i7 1360P" }, Ram = new[] { "16GB", "32GB" }, Ssd = new[] { "512GB SSD", "1TB SSD" }, BasePrice = 32990000m, Count = 16 },
            new { Brand = "HP", Category = "Mỏng nhẹ", Prefix = "HP Pavilion", Cpu = new[] { "Intel Core i5 1340P", "Intel Core i7 1360P", "AMD Ryzen 5 7530U" }, Ram = new[] { "8GB", "16GB" }, Ssd = new[] { "512GB SSD", "1TB SSD" }, BasePrice = 15990000m, Count = 24 },
            new { Brand = "HP", Category = "Văn phòng", Prefix = "HP ProBook", Cpu = new[] { "Intel Core i5 1335U", "Intel Core i7 1355U" }, Ram = new[] { "8GB", "16GB" }, Ssd = new[] { "512GB SSD", "1TB SSD" }, BasePrice = 17990000m, Count = 16 },
            new { Brand = "Lenovo", Category = "Văn phòng", Prefix = "Lenovo IdeaPad Slim", Cpu = new[] { "AMD Ryzen 5 7530U", "AMD Ryzen 7 7730U", "Intel Core i5 13420H" }, Ram = new[] { "8GB", "16GB" }, Ssd = new[] { "512GB SSD", "1TB SSD" }, BasePrice = 14990000m, Count = 26 },
            new { Brand = "Lenovo", Category = "Doanh nhân", Prefix = "Lenovo ThinkBook", Cpu = new[] { "Intel Core i5 1335U", "Intel Core Ultra 5 125H", "AMD Ryzen 7 7735HS" }, Ram = new[] { "16GB", "32GB" }, Ssd = new[] { "512GB SSD", "1TB SSD" }, BasePrice = 20990000m, Count = 18 },
            new { Brand = "Apple", Category = "Cao cấp", Prefix = "MacBook Air", Cpu = new[] { "Apple M2", "Apple M3" }, Ram = new[] { "8GB", "16GB", "24GB" }, Ssd = new[] { "256GB SSD", "512GB SSD", "1TB SSD" }, BasePrice = 24990000m, Count = 12 },
            new { Brand = "Apple", Category = "Cao cấp", Prefix = "MacBook Pro", Cpu = new[] { "Apple M3", "Apple M3 Pro" }, Ram = new[] { "16GB", "18GB", "36GB" }, Ssd = new[] { "512GB SSD", "1TB SSD" }, BasePrice = 39990000m, Count = 8 }
        };

        var badges = new[] { "Bán chạy", "Mới", "Hot", "Giá tốt", "Ưu đãi", "Đáng mua" };
        var rng = new Random(20260330);
        var sortOrder = 100;

        foreach (var plan in plans)
        {
            for (var i = 1; i <= plan.Count; i++)
            {
                var modelCode = plan.Prefix.Contains("MacBook")
                    ? $"{(plan.Prefix.Contains("Pro") ? "14" : "13")} {2024 + (i % 3)}"
                    : $"{(i + 10):000}";
                var productName = $"{plan.Prefix} {modelCode}";
                var cpu = plan.Cpu[(i - 1) % plan.Cpu.Length];
                var ram = plan.Ram[(i - 1) % plan.Ram.Length];
                var ssd = plan.Ssd[(i - 1) % plan.Ssd.Length];
                var price = plan.BasePrice + (i % 5) * 1000000m + rng.Next(0, 3) * 500000m;
                var oldPrice = price + (1000000m + (i % 4) * 500000m);
                var stock = 4 + (i * 3 % 27);
                var badge = badges[(i - 1) % badges.Length];
                var discount = Math.Max(3, Math.Min(15, (int)Math.Round((oldPrice - price) / oldPrice * 100)));
                var isFeatured = i <= 4 || i % 7 == 0;
                var description = BuildSampleDescription(productName, plan.Brand, cpu, ram, ssd);

                using var existsCmd = new SqlCommand("SELECT COUNT(*) FROM Products WHERE Brand=@Brand AND ProductName=@ProductName", conn);
                existsCmd.Parameters.AddWithValue("@Brand", plan.Brand);
                existsCmd.Parameters.AddWithValue("@ProductName", productName);
                var exists = Convert.ToInt32(await existsCmd.ExecuteScalarAsync()) > 0;
                if (exists) continue;

                using var insertCmd = new SqlCommand(@"INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder, CreatedAt)
VALUES(@Brand,@CategoryName,@ProductName,@Cpu,@Ram,@Ssd,@Price,@OldPrice,@StockQty,@IsFeatured,@BadgeText,@DescriptionText,@DiscountPercent,@SortOrder,GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
                insertCmd.Parameters.AddWithValue("@Brand", plan.Brand);
                insertCmd.Parameters.AddWithValue("@CategoryName", plan.Category);
                insertCmd.Parameters.AddWithValue("@ProductName", productName);
                insertCmd.Parameters.AddWithValue("@Cpu", cpu);
                insertCmd.Parameters.AddWithValue("@Ram", ram);
                insertCmd.Parameters.AddWithValue("@Ssd", ssd);
                insertCmd.Parameters.AddWithValue("@Price", price);
                insertCmd.Parameters.AddWithValue("@OldPrice", oldPrice);
                insertCmd.Parameters.AddWithValue("@StockQty", stock);
                insertCmd.Parameters.AddWithValue("@IsFeatured", isFeatured);
                insertCmd.Parameters.AddWithValue("@BadgeText", badge);
                insertCmd.Parameters.AddWithValue("@DescriptionText", description);
                insertCmd.Parameters.AddWithValue("@DiscountPercent", discount);
                insertCmd.Parameters.AddWithValue("@SortOrder", sortOrder++);
                var productId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                var images = GetBrandImageSet(plan.Brand, productName, productId).Take(3).ToList();
                for (var imgIndex = 0; imgIndex < images.Count; imgIndex++)
                {
                    using var imgCmd = new SqlCommand("INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) VALUES(@ProductId,@ImageUrl,@DisplayOrder)", conn);
                    imgCmd.Parameters.AddWithValue("@ProductId", productId);
                    imgCmd.Parameters.AddWithValue("@ImageUrl", images[imgIndex]);
                    imgCmd.Parameters.AddWithValue("@DisplayOrder", imgIndex + 1);
                    await imgCmd.ExecuteNonQueryAsync();
                }
            }
        }
    }


    public async Task<AdminUserSessionModel?> ValidateAdminLoginAsync(string username, string password)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"SELECT TOP 1 UserId, Username, FullName, IsSuperAdmin, CanViewOrders, CanUpdateOrders, CanCancelOrders, CanViewReviews, CanReplyReviews, CanDeleteReviews, CanManageInventory, CanDeleteInventory, CanImportInventory, CanManageWebsite, IsActive, PasswordHash FROM AdminUsers WHERE LTRIM(RTRIM(LOWER(Username)))=LTRIM(RTRIM(LOWER(@Username)))", conn);
        cmd.Parameters.AddWithValue("@Username", username?.Trim() ?? string.Empty);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        if (!reader.GetBoolean(14)) return null;
        var storedHash = reader[15]?.ToString() ?? string.Empty;
        var inputPassword = password?.Trim() ?? string.Empty;
        var inputHash = HashPassword(inputPassword);
        var legacySha256Hash = LegacySha256HashPassword(inputPassword);
        var acceptedByMd5 = string.Equals(storedHash, inputHash, StringComparison.OrdinalIgnoreCase);
        var acceptedByLegacySha256 = string.Equals(storedHash, legacySha256Hash, StringComparison.OrdinalIgnoreCase);
        if (!acceptedByMd5 && !acceptedByLegacySha256) return null;

        var userId = reader.GetInt32(0);
        if (acceptedByLegacySha256)
        {
            await reader.CloseAsync();
            using var updateCmd = new SqlCommand("UPDATE AdminUsers SET PasswordHash=@PasswordHash WHERE UserId=@UserId", conn);
            updateCmd.Parameters.AddWithValue("@UserId", userId);
            updateCmd.Parameters.AddWithValue("@PasswordHash", inputHash);
            await updateCmd.ExecuteNonQueryAsync();

            using var reloadCmd = new SqlCommand(@"SELECT TOP 1 UserId, Username, FullName, IsSuperAdmin, CanViewOrders, CanUpdateOrders, CanCancelOrders, CanViewReviews, CanReplyReviews, CanDeleteReviews, CanManageInventory, CanDeleteInventory, CanImportInventory, CanManageWebsite, IsActive, PasswordHash FROM AdminUsers WHERE UserId=@UserId", conn);
            reloadCmd.Parameters.AddWithValue("@UserId", userId);
            using var reloadReader = await reloadCmd.ExecuteReaderAsync();
            if (!await reloadReader.ReadAsync()) return null;

            return new AdminUserSessionModel
            {
                UserId = reloadReader.GetInt32(0),
                Username = reloadReader.GetString(1),
                FullName = reloadReader.GetString(2),
                IsSuperAdmin = reloadReader.GetBoolean(3),
                CanViewOrders = reloadReader.GetBoolean(4),
                CanUpdateOrders = reloadReader.GetBoolean(5),
                CanCancelOrders = reloadReader.GetBoolean(6),
                CanViewReviews = reloadReader.GetBoolean(7),
                CanReplyReviews = reloadReader.GetBoolean(8),
                CanDeleteReviews = reloadReader.GetBoolean(9),
                CanManageInventory = reloadReader.GetBoolean(10),
                CanDeleteInventory = reloadReader.GetBoolean(11),
                CanImportInventory = reloadReader.GetBoolean(12),
                CanManageWebsite = reloadReader.GetBoolean(13)
            };
        }

        return new AdminUserSessionModel
        {
            UserId = userId,
            Username = reader.GetString(1),
            FullName = reader.GetString(2),
            IsSuperAdmin = reader.GetBoolean(3),
            CanViewOrders = reader.GetBoolean(4),
            CanUpdateOrders = reader.GetBoolean(5),
            CanCancelOrders = reader.GetBoolean(6),
            CanViewReviews = reader.GetBoolean(7),
            CanReplyReviews = reader.GetBoolean(8),
            CanDeleteReviews = reader.GetBoolean(9),
            CanManageInventory = reader.GetBoolean(10),
            CanDeleteInventory = reader.GetBoolean(11),
            CanImportInventory = reader.GetBoolean(12),
            CanManageWebsite = reader.GetBoolean(13)
        };
    }

    public async Task<List<AdminUserItemViewModel>> GetAdminUsersAsync()
    {
        var result = new List<AdminUserItemViewModel>();
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"SELECT UserId, Username, FullName, IsSuperAdmin, CanViewOrders, CanUpdateOrders, CanCancelOrders, CanViewReviews, CanReplyReviews, CanDeleteReviews, CanManageInventory, CanDeleteInventory, CanImportInventory, CanManageWebsite, IsActive, CreatedAt FROM AdminUsers ORDER BY IsSuperAdmin DESC, Username", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new AdminUserItemViewModel
            {
                UserId = reader.GetInt32(0),
                Username = reader.GetString(1),
                FullName = reader.GetString(2),
                IsSuperAdmin = reader.GetBoolean(3),
                CanViewOrders = reader.GetBoolean(4),
                CanUpdateOrders = reader.GetBoolean(5),
                CanCancelOrders = reader.GetBoolean(6),
                CanViewReviews = reader.GetBoolean(7),
                CanReplyReviews = reader.GetBoolean(8),
                CanDeleteReviews = reader.GetBoolean(9),
                CanManageInventory = reader.GetBoolean(10),
                CanDeleteInventory = reader.GetBoolean(11),
                CanImportInventory = reader.GetBoolean(12),
                CanManageWebsite = reader.GetBoolean(13),
                IsActive = reader.GetBoolean(14),
                CreatedAt = reader.GetDateTime(15)
            });
        }
        return result;
    }

    public async Task<AdminUserFormViewModel?> GetAdminUserByIdAsync(int userId)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"SELECT TOP 1 UserId, Username, FullName, IsSuperAdmin, CanViewOrders, CanUpdateOrders, CanCancelOrders, CanViewReviews, CanReplyReviews, CanDeleteReviews, CanManageInventory, CanDeleteInventory, CanImportInventory, CanManageWebsite FROM AdminUsers WHERE UserId=@UserId", conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new AdminUserFormViewModel
        {
            UserId = reader.GetInt32(0),
            Username = reader.GetString(1),
            FullName = reader.GetString(2),
            IsSuperAdmin = reader.GetBoolean(3),
            CanViewOrders = reader.GetBoolean(4),
            CanUpdateOrders = reader.GetBoolean(5),
            CanCancelOrders = reader.GetBoolean(6),
            CanViewReviews = reader.GetBoolean(7),
            CanReplyReviews = reader.GetBoolean(8),
            CanDeleteReviews = reader.GetBoolean(9),
            CanManageInventory = reader.GetBoolean(10),
            CanDeleteInventory = reader.GetBoolean(11),
            CanImportInventory = reader.GetBoolean(12),
            CanManageWebsite = reader.GetBoolean(13)
        };
    }

    public async Task CreateAdminUserAsync(AdminUserFormViewModel model)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM AdminUsers WHERE LTRIM(RTRIM(LOWER(Username)))=LTRIM(RTRIM(LOWER(@Username)))", conn);
        checkCmd.Parameters.AddWithValue("@Username", model.Username.Trim());
        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
            throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

        using var cmd = new SqlCommand(@"INSERT INTO AdminUsers(
    Username, FullName, PasswordHash,
    IsSuperAdmin, CanViewOrders, CanUpdateOrders, CanCancelOrders,
    CanViewReviews, CanReplyReviews, CanDeleteReviews,
    CanManageInventory, CanDeleteInventory, CanImportInventory, CanManageWebsite,
    IsActive, CreatedAt
)
VALUES(
    @Username, @FullName, @PasswordHash,
    @IsSuperAdmin, @CanViewOrders, @CanUpdateOrders, @CanCancelOrders,
    @CanViewReviews, @CanReplyReviews, @CanDeleteReviews,
    @CanManageInventory, @CanDeleteInventory, @CanImportInventory, @CanManageWebsite,
    1, GETDATE()
)", conn);
        FillAdminUserCommand(cmd, model, includePassword: true);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAdminUserAsync(AdminUserFormViewModel model)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM AdminUsers WHERE LTRIM(RTRIM(LOWER(Username)))=LTRIM(RTRIM(LOWER(@Username))) AND UserId<>@UserId", conn);
        checkCmd.Parameters.AddWithValue("@Username", model.Username.Trim());
        checkCmd.Parameters.AddWithValue("@UserId", model.UserId);
        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
            throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

        using var cmd = new SqlCommand(@"UPDATE AdminUsers SET Username=@Username, FullName=@FullName, IsSuperAdmin=@IsSuperAdmin, CanViewOrders=@CanViewOrders, CanUpdateOrders=@CanUpdateOrders, CanCancelOrders=@CanCancelOrders, CanViewReviews=@CanViewReviews, CanReplyReviews=@CanReplyReviews, CanDeleteReviews=@CanDeleteReviews, CanManageInventory=@CanManageInventory, CanDeleteInventory=@CanDeleteInventory, CanImportInventory=@CanImportInventory, CanManageWebsite=@CanManageWebsite WHERE UserId=@UserId", conn);
        FillAdminUserCommand(cmd, model, includePassword: false);
        cmd.Parameters.AddWithValue("@UserId", model.UserId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ResetAdminPasswordAsync(int userId, string newPassword)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("UPDATE AdminUsers SET PasswordHash=@PasswordHash WHERE UserId=@UserId", conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@PasswordHash", HashPassword(newPassword.Trim()));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ToggleAdminUserStatusAsync(int userId)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"UPDATE AdminUsers SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END WHERE UserId=@UserId AND LTRIM(RTRIM(LOWER(Username)))<>N'admin'", conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        await cmd.ExecuteNonQueryAsync();
    }


    public async Task<WebsiteSettingsViewModel> GetWebsiteSettingsAsync()
    {
        if (_cache.TryGetValue(WebsiteSettingsCacheKey, out WebsiteSettingsViewModel? cached) && cached is not null)
            return cached;

        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"SELECT TOP 1 PromoText, StoreName, StoreTagline, LogoText, WebsiteLogoUrl, AdminLogoUrl, FaviconUrl, WebsiteLogoCropMode, AdminLogoCropMode, HeaderButtonText, HeaderButtonLink, HeroTitle, HeroSubtitle, HeroButtonText, HeroButtonLink, FeaturedBrandsTitle, DemandSectionTitle, FeaturedProductsTitle, FlashTitle, FlashBadgeText, FlashCountdownLabel, FlashCountdownEndsAt, FlashImageUrl, PrimaryColor, SecondaryColor, AccentColor, HeaderBackgroundColor, HeaderTextColor, StoreNameFontSize, StoreTaglineFontSize, PromoTextFontSize, HeroBackgroundColor, HeroTextColor, HeroTitleFontSize, HeroSubtitleFontSize, HeroButtonFontSize, PopupBackgroundColor, PopupTextColor, PopupTitleFontSize, PopupSubtitleFontSize, PopupButtonFontSize, SectionTitleColor, SectionSubtextColor, SectionTitleFontSize, FooterAbout, FooterSupportTitle, FooterSupportLine1, FooterSupportLine2, FooterSupportLine3, FooterBankTitle, FooterBankLine1, FooterBankLine2, FooterBankLine3, TopPhone1, TopPhone2, HeaderAddress, FooterCompanyHeading, FooterShowroomTitle, FooterShowroomAddress, FooterShowroomHotline, FooterWarrantyTitle, FooterWarrantyAddress, FooterWarrantyHotline, FooterWorkingHoursTitle, FooterWorkingHoursLine1, FooterWorkingHoursLine2, FooterShippingTitle, FooterShippingLine1, FooterShippingLine2, FooterShippingLine3, FooterShippingLine4, FooterQrLabel, FooterCopyright, CategoryBannerGamingUrl, CategoryBannerOfficeUrl, CategoryBannerPremiumUrl, CategoryBannerGraphicsUrl, CategoryBannerAccessoryUrl, CategoryBannerComponentUrl, FooterShippingImage1Url, FooterShippingImage2Url, FooterShippingImage3Url, FooterShippingImage4Url, FooterQrImageUrl, FooterInfoLink1Text, FooterInfoLink1Url, FooterInfoLink2Text, FooterInfoLink2Url, FooterInfoLink3Text, FooterInfoLink3Url, FooterInfoLink4Text, FooterInfoLink4Url, FooterInfoLink5Text, FooterInfoLink5Url, FooterInfoLink6Text, FooterInfoLink6Url, PopupTitle, PopupSubtitle, PopupButtonText, PopupButtonLink, PopupImageUrl, PopupEnabled, ShowHeroSection, ShowMegaMenu, ShowFlashBanner FROM WebsiteSettings ORDER BY SettingId", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return new WebsiteSettingsViewModel();
        var result = new WebsiteSettingsViewModel
        {
            PromoText = reader.GetString(0),
            StoreName = reader.GetString(1),
            StoreTagline = reader.GetString(2),
            LogoText = reader.GetString(3),
            WebsiteLogoUrl = NormalizeMediaUrl(reader.GetString(4)),
            AdminLogoUrl = NormalizeMediaUrl(reader.GetString(5)),
            FaviconUrl = NormalizeMediaUrl(reader.GetString(6)),
            WebsiteLogoCropMode = reader.GetString(7),
            AdminLogoCropMode = reader.GetString(8),
            HeaderButtonText = reader.GetString(9),
            HeaderButtonLink = reader.GetString(10),
            HeroTitle = reader.GetString(11),
            HeroSubtitle = reader.GetString(12),
            HeroButtonText = reader.GetString(13),
            HeroButtonLink = reader.GetString(14),
            FeaturedBrandsTitle = reader.GetString(15),
            DemandSectionTitle = reader.GetString(16),
            FeaturedProductsTitle = reader.GetString(17),
            FlashTitle = reader.GetString(18),
            FlashBadgeText = reader.GetString(19),
            FlashCountdownLabel = reader.GetString(20),
            FlashCountdownEndsAt = reader.GetString(21),
            FlashImageUrl = NormalizeMediaUrl(reader.GetString(22)),
            PrimaryColor = reader.GetString(23),
            SecondaryColor = reader.GetString(24),
            AccentColor = reader.GetString(25),
            HeaderBackgroundColor = reader.GetString(26),
            HeaderTextColor = reader.GetString(27),
            StoreNameFontSize = reader.GetInt32(28),
            StoreTaglineFontSize = reader.GetInt32(29),
            PromoTextFontSize = reader.GetInt32(30),
            HeroBackgroundColor = reader.GetString(31),
            HeroTextColor = reader.GetString(32),
            HeroTitleFontSize = reader.GetInt32(33),
            HeroSubtitleFontSize = reader.GetInt32(34),
            HeroButtonFontSize = reader.GetInt32(35),
            PopupBackgroundColor = reader.GetString(36),
            PopupTextColor = reader.GetString(37),
            PopupTitleFontSize = reader.GetInt32(38),
            PopupSubtitleFontSize = reader.GetInt32(39),
            PopupButtonFontSize = reader.GetInt32(40),
            SectionTitleColor = reader.GetString(41),
            SectionSubtextColor = reader.GetString(42),
            SectionTitleFontSize = reader.GetInt32(43),
            FooterAbout = reader.GetString(44),
            FooterSupportTitle = reader.GetString(45),
            FooterSupportLine1 = reader.GetString(46),
            FooterSupportLine2 = reader.GetString(47),
            FooterSupportLine3 = reader.GetString(48),
            FooterBankTitle = reader.GetString(49),
            FooterBankLine1 = reader.GetString(50),
            FooterBankLine2 = reader.GetString(51),
            FooterBankLine3 = reader.GetString(52),
            TopPhone1 = reader.GetString(53),
            TopPhone2 = reader.GetString(54),
            HeaderAddress = reader.GetString(55),
            FooterCompanyHeading = reader.GetString(56),
            FooterShowroomTitle = reader.GetString(57),
            FooterShowroomAddress = reader.GetString(58),
            FooterShowroomHotline = reader.GetString(59),
            FooterWarrantyTitle = reader.GetString(60),
            FooterWarrantyAddress = reader.GetString(61),
            FooterWarrantyHotline = reader.GetString(62),
            FooterWorkingHoursTitle = reader.GetString(63),
            FooterWorkingHoursLine1 = reader.GetString(64),
            FooterWorkingHoursLine2 = reader.GetString(65),
            FooterShippingTitle = reader.GetString(66),
            FooterShippingLine1 = reader.GetString(67),
            FooterShippingLine2 = reader.GetString(68),
            FooterShippingLine3 = reader.GetString(69),
            FooterShippingLine4 = reader.GetString(70),
            FooterQrLabel = reader.GetString(71),
            FooterCopyright = reader.GetString(72),
            CategoryBannerGamingUrl = NormalizeMediaUrl(reader.GetString(73)),
            CategoryBannerOfficeUrl = NormalizeMediaUrl(reader.GetString(74)),
            CategoryBannerPremiumUrl = NormalizeMediaUrl(reader.GetString(75)),
            CategoryBannerGraphicsUrl = NormalizeMediaUrl(reader.GetString(76)),
            CategoryBannerAccessoryUrl = NormalizeMediaUrl(reader.GetString(77)),
            CategoryBannerComponentUrl = NormalizeMediaUrl(reader.GetString(78)),
            FooterShippingImage1Url = NormalizeMediaUrl(reader.GetString(79)),
            FooterShippingImage2Url = NormalizeMediaUrl(reader.GetString(80)),
            FooterShippingImage3Url = NormalizeMediaUrl(reader.GetString(81)),
            FooterShippingImage4Url = NormalizeMediaUrl(reader.GetString(82)),
            FooterQrImageUrl = NormalizeMediaUrl(reader.GetString(83)),
            FooterInfoLink1Text = reader.GetString(84),
            FooterInfoLink1Url = reader.GetString(85),
            FooterInfoLink2Text = reader.GetString(86),
            FooterInfoLink2Url = reader.GetString(87),
            FooterInfoLink3Text = reader.GetString(88),
            FooterInfoLink3Url = reader.GetString(89),
            FooterInfoLink4Text = reader.GetString(90),
            FooterInfoLink4Url = reader.GetString(91),
            FooterInfoLink5Text = reader.GetString(92),
            FooterInfoLink5Url = reader.GetString(93),
            FooterInfoLink6Text = reader.GetString(94),
            FooterInfoLink6Url = reader.GetString(95),
            PopupTitle = reader.GetString(96),
            PopupSubtitle = reader.GetString(97),
            PopupButtonText = reader.GetString(98),
            PopupButtonLink = reader.GetString(99),
            PopupImageUrl = NormalizeMediaUrl(reader.GetString(100)),
            PopupEnabled = reader.GetBoolean(101),
            ShowHeroSection = reader.GetBoolean(102),
            ShowMegaMenu = reader.GetBoolean(103),
            ShowFlashBanner = reader.GetBoolean(104)
        };

        _cache.Set(WebsiteSettingsCacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<List<WebsiteSlideItemViewModel>> GetWebsiteSlidesAsync()
    {
        var result = new List<WebsiteSlideItemViewModel>();
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT SlideId, ISNULL(Title,N''), ISNULL(Subtitle,N''), ISNULL(ImageUrl,N''), ISNULL(LinkUrl,N'/'), ISNULL(ButtonText,N'Khám phá ngay'), ISNULL(SecondaryButtonText,N'Xem chi tiết'), ISNULL(SecondaryButtonLink,N'/Product/Catalog'), ISNULL(KickerText,N'ƯU ĐÃI NỔI BẬT'), ISNULL(ThemePrimaryColor,N'#cf2027'), ISNULL(ThemeAccentColor,N'#7c3aed'), ISNULL(TitleFontSize,54), ISNULL(SubtitleFontSize,18), ISNULL(KickerFontSize,13), ISNULL(ButtonFontSize,17), ISNULL(ChipFontSize,13), ISNULL(PanelOpacityPercent,88), ISNULL(PanelWidthPercent,42), ISNULL(PanelPadding,30), ISNULL(PanelRadius,28), DisplayOrder FROM BannerSlides ORDER BY DisplayOrder, SlideId", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new WebsiteSlideItemViewModel
            {
                SlideId = reader.GetInt32(0), Title = reader.GetString(1), Subtitle = reader.GetString(2), ImageUrl = reader.GetString(3), LinkUrl = reader.GetString(4), ButtonText = reader.GetString(5), SecondaryButtonText = reader.GetString(6), SecondaryButtonLink = reader.GetString(7), KickerText = reader.GetString(8), ThemePrimaryColor = reader.GetString(9), ThemeAccentColor = reader.GetString(10), TitleFontSize = reader.GetInt32(11), SubtitleFontSize = reader.GetInt32(12), KickerFontSize = reader.GetInt32(13), ButtonFontSize = reader.GetInt32(14), ChipFontSize = reader.GetInt32(15), PanelOpacityPercent = reader.GetInt32(16), PanelWidthPercent = reader.GetInt32(17), PanelPadding = reader.GetInt32(18), PanelRadius = reader.GetInt32(19), DisplayOrder = reader.GetInt32(20)
            });
        }
        while (result.Count < 4)
            result.Add(new WebsiteSlideItemViewModel { DisplayOrder = result.Count + 1, LinkUrl = "/", SecondaryButtonLink = "/Product/Catalog", PanelOpacityPercent = 88, PanelWidthPercent = 42, PanelPadding = 30, PanelRadius = 28 });
        return result.OrderBy(x => x.DisplayOrder).ToList();
    }

    public async Task SaveWebsitePresentationAsync(AdminWebsitePageViewModel model)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = new SqlCommand(@"UPDATE WebsiteSettings SET PromoText=@PromoText, StoreName=@StoreName, StoreTagline=@StoreTagline, LogoText=@LogoText, WebsiteLogoUrl=@WebsiteLogoUrl, AdminLogoUrl=@AdminLogoUrl, FaviconUrl=@FaviconUrl, WebsiteLogoCropMode=@WebsiteLogoCropMode, AdminLogoCropMode=@AdminLogoCropMode, HeaderButtonText=@HeaderButtonText, HeaderButtonLink=@HeaderButtonLink, HeroTitle=@HeroTitle, HeroSubtitle=@HeroSubtitle, HeroButtonText=@HeroButtonText, HeroButtonLink=@HeroButtonLink, FeaturedBrandsTitle=@FeaturedBrandsTitle, DemandSectionTitle=@DemandSectionTitle, FeaturedProductsTitle=@FeaturedProductsTitle, FlashTitle=@FlashTitle, FlashBadgeText=@FlashBadgeText, FlashCountdownLabel=@FlashCountdownLabel, FlashCountdownEndsAt=@FlashCountdownEndsAt, FlashImageUrl=@FlashImageUrl, PrimaryColor=@PrimaryColor, SecondaryColor=@SecondaryColor, AccentColor=@AccentColor, HeaderBackgroundColor=@HeaderBackgroundColor, HeaderTextColor=@HeaderTextColor, StoreNameFontSize=@StoreNameFontSize, StoreTaglineFontSize=@StoreTaglineFontSize, PromoTextFontSize=@PromoTextFontSize, HeroBackgroundColor=@HeroBackgroundColor, HeroTextColor=@HeroTextColor, HeroTitleFontSize=@HeroTitleFontSize, HeroSubtitleFontSize=@HeroSubtitleFontSize, HeroButtonFontSize=@HeroButtonFontSize, PopupBackgroundColor=@PopupBackgroundColor, PopupTextColor=@PopupTextColor, PopupTitleFontSize=@PopupTitleFontSize, PopupSubtitleFontSize=@PopupSubtitleFontSize, PopupButtonFontSize=@PopupButtonFontSize, SectionTitleColor=@SectionTitleColor, SectionSubtextColor=@SectionSubtextColor, SectionTitleFontSize=@SectionTitleFontSize, FooterAbout=@FooterAbout, FooterSupportTitle=@FooterSupportTitle, FooterSupportLine1=@FooterSupportLine1, FooterSupportLine2=@FooterSupportLine2, FooterSupportLine3=@FooterSupportLine3, FooterBankTitle=@FooterBankTitle, FooterBankLine1=@FooterBankLine1, FooterBankLine2=@FooterBankLine2, FooterBankLine3=@FooterBankLine3, TopPhone1=@TopPhone1, TopPhone2=@TopPhone2, HeaderAddress=@HeaderAddress, FooterCompanyHeading=@FooterCompanyHeading, FooterShowroomTitle=@FooterShowroomTitle, FooterShowroomAddress=@FooterShowroomAddress, FooterShowroomHotline=@FooterShowroomHotline, FooterWarrantyTitle=@FooterWarrantyTitle, FooterWarrantyAddress=@FooterWarrantyAddress, FooterWarrantyHotline=@FooterWarrantyHotline, FooterWorkingHoursTitle=@FooterWorkingHoursTitle, FooterWorkingHoursLine1=@FooterWorkingHoursLine1, FooterWorkingHoursLine2=@FooterWorkingHoursLine2, FooterShippingTitle=@FooterShippingTitle, FooterShippingLine1=@FooterShippingLine1, FooterShippingLine2=@FooterShippingLine2, FooterShippingLine3=@FooterShippingLine3, FooterShippingLine4=@FooterShippingLine4, FooterQrLabel=@FooterQrLabel, FooterCopyright=@FooterCopyright, CategoryBannerGamingUrl=@CategoryBannerGamingUrl, CategoryBannerOfficeUrl=@CategoryBannerOfficeUrl, CategoryBannerPremiumUrl=@CategoryBannerPremiumUrl, CategoryBannerGraphicsUrl=@CategoryBannerGraphicsUrl, CategoryBannerAccessoryUrl=@CategoryBannerAccessoryUrl, CategoryBannerComponentUrl=@CategoryBannerComponentUrl, FooterShippingImage1Url=@FooterShippingImage1Url, FooterShippingImage2Url=@FooterShippingImage2Url, FooterShippingImage3Url=@FooterShippingImage3Url, FooterShippingImage4Url=@FooterShippingImage4Url, FooterQrImageUrl=@FooterQrImageUrl, FooterInfoLink1Text=@FooterInfoLink1Text, FooterInfoLink1Url=@FooterInfoLink1Url, FooterInfoLink2Text=@FooterInfoLink2Text, FooterInfoLink2Url=@FooterInfoLink2Url, FooterInfoLink3Text=@FooterInfoLink3Text, FooterInfoLink3Url=@FooterInfoLink3Url, FooterInfoLink4Text=@FooterInfoLink4Text, FooterInfoLink4Url=@FooterInfoLink4Url, FooterInfoLink5Text=@FooterInfoLink5Text, FooterInfoLink5Url=@FooterInfoLink5Url, FooterInfoLink6Text=@FooterInfoLink6Text, FooterInfoLink6Url=@FooterInfoLink6Url, PopupTitle=@PopupTitle, PopupSubtitle=@PopupSubtitle, PopupButtonText=@PopupButtonText, PopupButtonLink=@PopupButtonLink, PopupImageUrl=@PopupImageUrl, PopupEnabled=@PopupEnabled, ShowHeroSection=@ShowHeroSection, ShowMegaMenu=@ShowMegaMenu, ShowFlashBanner=@ShowFlashBanner", conn, tx))
            {
                var s = model.Settings ?? new WebsiteSettingsViewModel();
                cmd.Parameters.AddWithValue("@PromoText", s.PromoText ?? string.Empty); cmd.Parameters.AddWithValue("@StoreName", s.StoreName ?? string.Empty); cmd.Parameters.AddWithValue("@StoreTagline", s.StoreTagline ?? string.Empty);
                cmd.Parameters.AddWithValue("@LogoText", s.LogoText ?? string.Empty); cmd.Parameters.AddWithValue("@WebsiteLogoUrl", s.WebsiteLogoUrl ?? string.Empty); cmd.Parameters.AddWithValue("@AdminLogoUrl", s.AdminLogoUrl ?? string.Empty); cmd.Parameters.AddWithValue("@FaviconUrl", s.FaviconUrl ?? string.Empty); cmd.Parameters.AddWithValue("@WebsiteLogoCropMode", string.IsNullOrWhiteSpace(s.WebsiteLogoCropMode) ? "square" : s.WebsiteLogoCropMode); cmd.Parameters.AddWithValue("@AdminLogoCropMode", string.IsNullOrWhiteSpace(s.AdminLogoCropMode) ? "square" : s.AdminLogoCropMode); cmd.Parameters.AddWithValue("@HeaderButtonText", s.HeaderButtonText ?? string.Empty); cmd.Parameters.AddWithValue("@HeaderButtonLink", s.HeaderButtonLink ?? string.Empty);
                cmd.Parameters.AddWithValue("@HeroTitle", s.HeroTitle ?? string.Empty); cmd.Parameters.AddWithValue("@HeroSubtitle", s.HeroSubtitle ?? string.Empty); cmd.Parameters.AddWithValue("@HeroButtonText", s.HeroButtonText ?? string.Empty); cmd.Parameters.AddWithValue("@HeroButtonLink", s.HeroButtonLink ?? string.Empty);
                cmd.Parameters.AddWithValue("@FeaturedBrandsTitle", s.FeaturedBrandsTitle ?? string.Empty); cmd.Parameters.AddWithValue("@DemandSectionTitle", s.DemandSectionTitle ?? string.Empty); cmd.Parameters.AddWithValue("@FeaturedProductsTitle", s.FeaturedProductsTitle ?? string.Empty);
                cmd.Parameters.AddWithValue("@FlashTitle", s.FlashTitle ?? string.Empty); cmd.Parameters.AddWithValue("@FlashBadgeText", s.FlashBadgeText ?? "FLASH SALE"); cmd.Parameters.AddWithValue("@FlashCountdownLabel", s.FlashCountdownLabel ?? "Kết thúc trong:"); cmd.Parameters.AddWithValue("@FlashCountdownEndsAt", s.FlashCountdownEndsAt ?? "2030-12-31T23:59:59"); cmd.Parameters.AddWithValue("@FlashImageUrl", s.FlashImageUrl ?? string.Empty); cmd.Parameters.AddWithValue("@PrimaryColor", s.PrimaryColor ?? "#cf2027");
                cmd.Parameters.AddWithValue("@SecondaryColor", s.SecondaryColor ?? "#1d4ed8"); cmd.Parameters.AddWithValue("@AccentColor", s.AccentColor ?? "#7c3aed"); cmd.Parameters.AddWithValue("@HeaderBackgroundColor", s.HeaderBackgroundColor ?? "#ffffff"); cmd.Parameters.AddWithValue("@HeaderTextColor", s.HeaderTextColor ?? "#0f172a"); cmd.Parameters.AddWithValue("@StoreNameFontSize", s.StoreNameFontSize <= 0 ? 18 : s.StoreNameFontSize); cmd.Parameters.AddWithValue("@StoreTaglineFontSize", s.StoreTaglineFontSize <= 0 ? 14 : s.StoreTaglineFontSize); cmd.Parameters.AddWithValue("@PromoTextFontSize", s.PromoTextFontSize <= 0 ? 14 : s.PromoTextFontSize); cmd.Parameters.AddWithValue("@HeroBackgroundColor", s.HeroBackgroundColor ?? "#ffffff"); cmd.Parameters.AddWithValue("@HeroTextColor", s.HeroTextColor ?? "#0f172a"); cmd.Parameters.AddWithValue("@HeroTitleFontSize", s.HeroTitleFontSize <= 0 ? 42 : s.HeroTitleFontSize); cmd.Parameters.AddWithValue("@HeroSubtitleFontSize", s.HeroSubtitleFontSize <= 0 ? 18 : s.HeroSubtitleFontSize); cmd.Parameters.AddWithValue("@HeroButtonFontSize", s.HeroButtonFontSize <= 0 ? 18 : s.HeroButtonFontSize); cmd.Parameters.AddWithValue("@PopupBackgroundColor", s.PopupBackgroundColor ?? "#ffffff"); cmd.Parameters.AddWithValue("@PopupTextColor", s.PopupTextColor ?? "#0f172a"); cmd.Parameters.AddWithValue("@PopupTitleFontSize", s.PopupTitleFontSize <= 0 ? 38 : s.PopupTitleFontSize); cmd.Parameters.AddWithValue("@PopupSubtitleFontSize", s.PopupSubtitleFontSize <= 0 ? 16 : s.PopupSubtitleFontSize); cmd.Parameters.AddWithValue("@PopupButtonFontSize", s.PopupButtonFontSize <= 0 ? 17 : s.PopupButtonFontSize); cmd.Parameters.AddWithValue("@SectionTitleColor", s.SectionTitleColor ?? "#0f172a"); cmd.Parameters.AddWithValue("@SectionSubtextColor", s.SectionSubtextColor ?? "#475569"); cmd.Parameters.AddWithValue("@SectionTitleFontSize", s.SectionTitleFontSize <= 0 ? 22 : s.SectionTitleFontSize); cmd.Parameters.AddWithValue("@FooterAbout", s.FooterAbout ?? string.Empty);
                cmd.Parameters.AddWithValue("@FooterSupportTitle", s.FooterSupportTitle ?? string.Empty); cmd.Parameters.AddWithValue("@FooterSupportLine1", s.FooterSupportLine1 ?? string.Empty); cmd.Parameters.AddWithValue("@FooterSupportLine2", s.FooterSupportLine2 ?? string.Empty); cmd.Parameters.AddWithValue("@FooterSupportLine3", s.FooterSupportLine3 ?? string.Empty);
                cmd.Parameters.AddWithValue("@FooterBankTitle", s.FooterBankTitle ?? string.Empty); cmd.Parameters.AddWithValue("@FooterBankLine1", s.FooterBankLine1 ?? string.Empty); cmd.Parameters.AddWithValue("@FooterBankLine2", s.FooterBankLine2 ?? string.Empty); cmd.Parameters.AddWithValue("@FooterBankLine3", s.FooterBankLine3 ?? string.Empty);
                cmd.Parameters.AddWithValue("@TopPhone1", s.TopPhone1 ?? string.Empty); cmd.Parameters.AddWithValue("@TopPhone2", s.TopPhone2 ?? string.Empty); cmd.Parameters.AddWithValue("@HeaderAddress", s.HeaderAddress ?? string.Empty); cmd.Parameters.AddWithValue("@FooterCompanyHeading", s.FooterCompanyHeading ?? string.Empty); cmd.Parameters.AddWithValue("@FooterShowroomTitle", s.FooterShowroomTitle ?? string.Empty); cmd.Parameters.AddWithValue("@FooterShowroomAddress", s.FooterShowroomAddress ?? string.Empty); cmd.Parameters.AddWithValue("@FooterShowroomHotline", s.FooterShowroomHotline ?? string.Empty); cmd.Parameters.AddWithValue("@FooterWarrantyTitle", s.FooterWarrantyTitle ?? string.Empty); cmd.Parameters.AddWithValue("@FooterWarrantyAddress", s.FooterWarrantyAddress ?? string.Empty); cmd.Parameters.AddWithValue("@FooterWarrantyHotline", s.FooterWarrantyHotline ?? string.Empty); cmd.Parameters.AddWithValue("@FooterWorkingHoursTitle", s.FooterWorkingHoursTitle ?? string.Empty); cmd.Parameters.AddWithValue("@FooterWorkingHoursLine1", s.FooterWorkingHoursLine1 ?? string.Empty); cmd.Parameters.AddWithValue("@FooterWorkingHoursLine2", s.FooterWorkingHoursLine2 ?? string.Empty); cmd.Parameters.AddWithValue("@FooterShippingTitle", s.FooterShippingTitle ?? string.Empty); cmd.Parameters.AddWithValue("@FooterShippingLine1", s.FooterShippingLine1 ?? string.Empty); cmd.Parameters.AddWithValue("@FooterShippingLine2", s.FooterShippingLine2 ?? string.Empty); cmd.Parameters.AddWithValue("@FooterShippingLine3", s.FooterShippingLine3 ?? string.Empty); cmd.Parameters.AddWithValue("@FooterShippingLine4", s.FooterShippingLine4 ?? string.Empty); cmd.Parameters.AddWithValue("@FooterQrLabel", s.FooterQrLabel ?? string.Empty); cmd.Parameters.AddWithValue("@FooterCopyright", s.FooterCopyright ?? string.Empty);
                cmd.Parameters.AddWithValue("@CategoryBannerGamingUrl", s.CategoryBannerGamingUrl ?? string.Empty); cmd.Parameters.AddWithValue("@CategoryBannerOfficeUrl", s.CategoryBannerOfficeUrl ?? string.Empty); cmd.Parameters.AddWithValue("@CategoryBannerPremiumUrl", s.CategoryBannerPremiumUrl ?? string.Empty); cmd.Parameters.AddWithValue("@CategoryBannerGraphicsUrl", s.CategoryBannerGraphicsUrl ?? string.Empty); cmd.Parameters.AddWithValue("@CategoryBannerAccessoryUrl", s.CategoryBannerAccessoryUrl ?? string.Empty); cmd.Parameters.AddWithValue("@CategoryBannerComponentUrl", s.CategoryBannerComponentUrl ?? string.Empty); cmd.Parameters.AddWithValue("@FooterShippingImage1Url", s.FooterShippingImage1Url ?? string.Empty); cmd.Parameters.AddWithValue("@FooterShippingImage2Url", s.FooterShippingImage2Url ?? string.Empty); cmd.Parameters.AddWithValue("@FooterShippingImage3Url", s.FooterShippingImage3Url ?? string.Empty); cmd.Parameters.AddWithValue("@FooterShippingImage4Url", s.FooterShippingImage4Url ?? string.Empty); cmd.Parameters.AddWithValue("@FooterQrImageUrl", s.FooterQrImageUrl ?? string.Empty); cmd.Parameters.AddWithValue("@FooterInfoLink1Text", s.FooterInfoLink1Text ?? string.Empty); cmd.Parameters.AddWithValue("@FooterInfoLink1Url", s.FooterInfoLink1Url ?? string.Empty); cmd.Parameters.AddWithValue("@FooterInfoLink2Text", s.FooterInfoLink2Text ?? string.Empty); cmd.Parameters.AddWithValue("@FooterInfoLink2Url", s.FooterInfoLink2Url ?? string.Empty); cmd.Parameters.AddWithValue("@FooterInfoLink3Text", s.FooterInfoLink3Text ?? string.Empty); cmd.Parameters.AddWithValue("@FooterInfoLink3Url", s.FooterInfoLink3Url ?? string.Empty); cmd.Parameters.AddWithValue("@FooterInfoLink4Text", s.FooterInfoLink4Text ?? string.Empty); cmd.Parameters.AddWithValue("@FooterInfoLink4Url", s.FooterInfoLink4Url ?? string.Empty); cmd.Parameters.AddWithValue("@FooterInfoLink5Text", s.FooterInfoLink5Text ?? string.Empty); cmd.Parameters.AddWithValue("@FooterInfoLink5Url", s.FooterInfoLink5Url ?? string.Empty); cmd.Parameters.AddWithValue("@FooterInfoLink6Text", s.FooterInfoLink6Text ?? string.Empty); cmd.Parameters.AddWithValue("@FooterInfoLink6Url", s.FooterInfoLink6Url ?? string.Empty);
                cmd.Parameters.AddWithValue("@PopupTitle", s.PopupTitle ?? string.Empty); cmd.Parameters.AddWithValue("@PopupSubtitle", s.PopupSubtitle ?? string.Empty); cmd.Parameters.AddWithValue("@PopupButtonText", s.PopupButtonText ?? string.Empty); cmd.Parameters.AddWithValue("@PopupButtonLink", s.PopupButtonLink ?? "/Product/Catalog"); cmd.Parameters.AddWithValue("@PopupImageUrl", s.PopupImageUrl ?? string.Empty); cmd.Parameters.AddWithValue("@PopupEnabled", s.PopupEnabled); cmd.Parameters.AddWithValue("@ShowHeroSection", s.ShowHeroSection); cmd.Parameters.AddWithValue("@ShowMegaMenu", s.ShowMegaMenu); cmd.Parameters.AddWithValue("@ShowFlashBanner", s.ShowFlashBanner);
                await cmd.ExecuteNonQueryAsync();
            }
            foreach (var slide in (model.Slides ?? new List<WebsiteSlideItemViewModel>()).OrderBy(x => x.DisplayOrder))
            {
                if (slide.SlideId > 0)
                {
                    using var updateCmd = new SqlCommand(@"UPDATE BannerSlides SET Title=@Title, Subtitle=@Subtitle, ImageUrl=@ImageUrl, LinkUrl=@LinkUrl, ButtonText=@ButtonText, SecondaryButtonText=@SecondaryButtonText, SecondaryButtonLink=@SecondaryButtonLink, KickerText=@KickerText, ThemePrimaryColor=@ThemePrimaryColor, ThemeAccentColor=@ThemeAccentColor, TitleFontSize=@TitleFontSize, SubtitleFontSize=@SubtitleFontSize, KickerFontSize=@KickerFontSize, ButtonFontSize=@ButtonFontSize, ChipFontSize=@ChipFontSize, PanelOpacityPercent=@PanelOpacityPercent, PanelWidthPercent=@PanelWidthPercent, PanelPadding=@PanelPadding, PanelRadius=@PanelRadius, DisplayOrder=@DisplayOrder WHERE SlideId=@SlideId", conn, tx);
                    updateCmd.Parameters.AddWithValue("@SlideId", slide.SlideId); updateCmd.Parameters.AddWithValue("@Title", slide.Title ?? string.Empty); updateCmd.Parameters.AddWithValue("@Subtitle", slide.Subtitle ?? string.Empty);
                    updateCmd.Parameters.AddWithValue("@ImageUrl", slide.ImageUrl ?? string.Empty); updateCmd.Parameters.AddWithValue("@LinkUrl", string.IsNullOrWhiteSpace(slide.LinkUrl) ? "/" : slide.LinkUrl); updateCmd.Parameters.AddWithValue("@ButtonText", slide.ButtonText ?? "Khám phá ngay"); updateCmd.Parameters.AddWithValue("@SecondaryButtonText", slide.SecondaryButtonText ?? "Xem chi tiết"); updateCmd.Parameters.AddWithValue("@SecondaryButtonLink", string.IsNullOrWhiteSpace(slide.SecondaryButtonLink) ? "/Product/Catalog" : slide.SecondaryButtonLink); updateCmd.Parameters.AddWithValue("@KickerText", slide.KickerText ?? "ƯU ĐÃI NỔI BẬT"); updateCmd.Parameters.AddWithValue("@ThemePrimaryColor", slide.ThemePrimaryColor ?? "#cf2027"); updateCmd.Parameters.AddWithValue("@ThemeAccentColor", slide.ThemeAccentColor ?? "#7c3aed"); updateCmd.Parameters.AddWithValue("@TitleFontSize", slide.TitleFontSize <= 0 ? 54 : slide.TitleFontSize); updateCmd.Parameters.AddWithValue("@SubtitleFontSize", slide.SubtitleFontSize <= 0 ? 18 : slide.SubtitleFontSize); updateCmd.Parameters.AddWithValue("@KickerFontSize", slide.KickerFontSize <= 0 ? 13 : slide.KickerFontSize); updateCmd.Parameters.AddWithValue("@ButtonFontSize", slide.ButtonFontSize <= 0 ? 17 : slide.ButtonFontSize); updateCmd.Parameters.AddWithValue("@ChipFontSize", slide.ChipFontSize <= 0 ? 13 : slide.ChipFontSize); updateCmd.Parameters.AddWithValue("@PanelOpacityPercent", slide.PanelOpacityPercent <= 0 ? 88 : slide.PanelOpacityPercent); updateCmd.Parameters.AddWithValue("@PanelWidthPercent", slide.PanelWidthPercent <= 0 ? 42 : slide.PanelWidthPercent); updateCmd.Parameters.AddWithValue("@PanelPadding", slide.PanelPadding <= 0 ? 30 : slide.PanelPadding); updateCmd.Parameters.AddWithValue("@PanelRadius", slide.PanelRadius <= 0 ? 28 : slide.PanelRadius); updateCmd.Parameters.AddWithValue("@DisplayOrder", slide.DisplayOrder);
                    await updateCmd.ExecuteNonQueryAsync();
                }
                else if (!string.IsNullOrWhiteSpace(slide.ImageUrl) || !string.IsNullOrWhiteSpace(slide.Title) || !string.IsNullOrWhiteSpace(slide.Subtitle))
                {
                    using var insertCmd = new SqlCommand(@"INSERT INTO BannerSlides(Title, Subtitle, ImageUrl, LinkUrl, ButtonText, SecondaryButtonText, SecondaryButtonLink, KickerText, ThemePrimaryColor, ThemeAccentColor, TitleFontSize, SubtitleFontSize, KickerFontSize, ButtonFontSize, ChipFontSize, PanelOpacityPercent, PanelWidthPercent, PanelPadding, PanelRadius, DisplayOrder) VALUES(@Title,@Subtitle,@ImageUrl,@LinkUrl,@ButtonText,@SecondaryButtonText,@SecondaryButtonLink,@KickerText,@ThemePrimaryColor,@ThemeAccentColor,@TitleFontSize,@SubtitleFontSize,@KickerFontSize,@ButtonFontSize,@ChipFontSize,@PanelOpacityPercent,@PanelWidthPercent,@PanelPadding,@PanelRadius,@DisplayOrder)", conn, tx);
                    insertCmd.Parameters.AddWithValue("@Title", slide.Title ?? string.Empty); insertCmd.Parameters.AddWithValue("@Subtitle", slide.Subtitle ?? string.Empty); insertCmd.Parameters.AddWithValue("@ImageUrl", slide.ImageUrl ?? string.Empty);
                    insertCmd.Parameters.AddWithValue("@LinkUrl", string.IsNullOrWhiteSpace(slide.LinkUrl) ? "/" : slide.LinkUrl); insertCmd.Parameters.AddWithValue("@ButtonText", slide.ButtonText ?? "Khám phá ngay"); insertCmd.Parameters.AddWithValue("@SecondaryButtonText", slide.SecondaryButtonText ?? "Xem chi tiết"); insertCmd.Parameters.AddWithValue("@SecondaryButtonLink", string.IsNullOrWhiteSpace(slide.SecondaryButtonLink) ? "/Product/Catalog" : slide.SecondaryButtonLink); insertCmd.Parameters.AddWithValue("@KickerText", slide.KickerText ?? "ƯU ĐÃI NỔI BẬT"); insertCmd.Parameters.AddWithValue("@ThemePrimaryColor", slide.ThemePrimaryColor ?? "#cf2027"); insertCmd.Parameters.AddWithValue("@ThemeAccentColor", slide.ThemeAccentColor ?? "#7c3aed"); insertCmd.Parameters.AddWithValue("@TitleFontSize", slide.TitleFontSize <= 0 ? 54 : slide.TitleFontSize); insertCmd.Parameters.AddWithValue("@SubtitleFontSize", slide.SubtitleFontSize <= 0 ? 18 : slide.SubtitleFontSize); insertCmd.Parameters.AddWithValue("@KickerFontSize", slide.KickerFontSize <= 0 ? 13 : slide.KickerFontSize); insertCmd.Parameters.AddWithValue("@ButtonFontSize", slide.ButtonFontSize <= 0 ? 17 : slide.ButtonFontSize); insertCmd.Parameters.AddWithValue("@ChipFontSize", slide.ChipFontSize <= 0 ? 13 : slide.ChipFontSize); insertCmd.Parameters.AddWithValue("@PanelOpacityPercent", slide.PanelOpacityPercent <= 0 ? 88 : slide.PanelOpacityPercent); insertCmd.Parameters.AddWithValue("@PanelWidthPercent", slide.PanelWidthPercent <= 0 ? 42 : slide.PanelWidthPercent); insertCmd.Parameters.AddWithValue("@PanelPadding", slide.PanelPadding <= 0 ? 30 : slide.PanelPadding); insertCmd.Parameters.AddWithValue("@PanelRadius", slide.PanelRadius <= 0 ? 28 : slide.PanelRadius); insertCmd.Parameters.AddWithValue("@DisplayOrder", slide.DisplayOrder);
                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
            tx.Commit();
            _cache.Remove(WebsiteSettingsCacheKey);
            _cache.Remove(TopBrandsCacheKey);
            _cache.Remove(TopCategoriesCacheKey);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static void FillAdminUserCommand(SqlCommand cmd, AdminUserFormViewModel model, bool includePassword)
    {
        var isSuper = model.IsSuperAdmin;
        var canViewOrders = isSuper || model.CanViewOrders || model.CanUpdateOrders || model.CanCancelOrders;
        var canUpdateOrders = isSuper || model.CanUpdateOrders;
        var canCancelOrders = isSuper || model.CanCancelOrders;
        var canViewReviews = isSuper || model.CanViewReviews || model.CanReplyReviews || model.CanDeleteReviews;
        var canReplyReviews = isSuper || model.CanReplyReviews;
        var canDeleteReviews = isSuper || model.CanDeleteReviews;
        var canManageInventory = isSuper || model.CanManageInventory || model.CanDeleteInventory;
        var canDeleteInventory = isSuper || model.CanDeleteInventory;
        var canImportInventory = isSuper || model.CanImportInventory;
        var canManageWebsite = isSuper || model.CanManageWebsite;

        cmd.Parameters.AddWithValue("@Username", model.Username.Trim());
        cmd.Parameters.AddWithValue("@FullName", model.FullName.Trim());
        cmd.Parameters.AddWithValue("@IsSuperAdmin", isSuper);
        cmd.Parameters.AddWithValue("@CanViewOrders", canViewOrders);
        cmd.Parameters.AddWithValue("@CanUpdateOrders", canUpdateOrders);
        cmd.Parameters.AddWithValue("@CanCancelOrders", canCancelOrders);
        cmd.Parameters.AddWithValue("@CanViewReviews", canViewReviews);
        cmd.Parameters.AddWithValue("@CanReplyReviews", canReplyReviews);
        cmd.Parameters.AddWithValue("@CanDeleteReviews", canDeleteReviews);
        cmd.Parameters.AddWithValue("@CanManageInventory", canManageInventory);
        cmd.Parameters.AddWithValue("@CanDeleteInventory", canDeleteInventory);
        cmd.Parameters.AddWithValue("@CanImportInventory", canImportInventory);
        cmd.Parameters.AddWithValue("@CanManageWebsite", canManageWebsite);
        if (includePassword)
            cmd.Parameters.AddWithValue("@PasswordHash", HashPassword(model.Password));
    }

    private static string HashPassword(string password)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string LegacySha256HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
        return Convert.ToHexString(hash);
    }

    public async Task<HomePageViewModel> GetHomePageAsync()
    {
        var vm = new HomePageViewModel();
        vm.Settings = await GetWebsiteSettingsAsync();
        vm.Slides = await GetSlidesAsync();
        vm.FeaturedBrands = await GetDistinctAsync("SELECT DISTINCT TOP 10 Brand FROM Products ORDER BY Brand");
        vm.DemandGroups = await GetDistinctAsync("SELECT DISTINCT TOP 10 CategoryName FROM Products ORDER BY CategoryName");
        vm.FeaturedProducts = await GetProductsAsync("SELECT TOP 8 * FROM Products ORDER BY IsFeatured DESC, SortOrder, ProductId");
        vm.FlashSaleProducts = await GetProductsAsync("SELECT TOP 8 * FROM Products ORDER BY DiscountPercent DESC, ProductId DESC");
        return vm;
    }

    public async Task<List<BannerSlide>> GetSlidesAsync()
    {
        var result = new List<BannerSlide>();
        using var conn = CreateConnection();
        await conn.OpenAsync();
        try
        {
            using var cmd = new SqlCommand("SELECT SlideId, Title, Subtitle, ImageUrl, LinkUrl, ISNULL(ButtonText,N'Khám phá ngay'), ISNULL(SecondaryButtonText,N'Xem chi tiết'), ISNULL(SecondaryButtonLink,N'/Product/Catalog'), ISNULL(KickerText,N'ƯU ĐÃI NỔI BẬT'), ISNULL(ThemePrimaryColor,N'#cf2027'), ISNULL(ThemeAccentColor,N'#7c3aed'), ISNULL(TitleFontSize,54), ISNULL(SubtitleFontSize,18), ISNULL(KickerFontSize,13), ISNULL(ButtonFontSize,17), ISNULL(ChipFontSize,13), ISNULL(PanelOpacityPercent,88), ISNULL(PanelWidthPercent,42), ISNULL(PanelPadding,30), ISNULL(PanelRadius,28), DisplayOrder FROM BannerSlides ORDER BY DisplayOrder", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new BannerSlide
                {
                    Id = reader.GetInt32(0),
                    Title = reader[1]?.ToString() ?? string.Empty,
                    Subtitle = reader[2]?.ToString() ?? string.Empty,
                    ImageUrl = NormalizeMediaUrl(reader[3]?.ToString()),
                    LinkUrl = reader[4]?.ToString() ?? string.Empty,
                    ButtonText = reader[5]?.ToString() ?? "Khám phá ngay",
                    SecondaryButtonText = reader[6]?.ToString() ?? "Xem chi tiết",
                    SecondaryButtonLink = reader[7]?.ToString() ?? "/Product/Catalog",
                    KickerText = reader[8]?.ToString() ?? "ƯU ĐÃI NỔI BẬT",
                    ThemePrimaryColor = reader[9]?.ToString() ?? "#cf2027",
                    ThemeAccentColor = reader[10]?.ToString() ?? "#7c3aed",
                    TitleFontSize = Convert.ToInt32(reader[11]),
                    SubtitleFontSize = Convert.ToInt32(reader[12]),
                    KickerFontSize = Convert.ToInt32(reader[13]),
                    ButtonFontSize = Convert.ToInt32(reader[14]),
                    ChipFontSize = Convert.ToInt32(reader[15]),
                    PanelOpacityPercent = Convert.ToInt32(reader[16]),
                    PanelWidthPercent = Convert.ToInt32(reader[17]),
                    PanelPadding = Convert.ToInt32(reader[18]),
                    PanelRadius = Convert.ToInt32(reader[19]),
                    DisplayOrder = reader.GetInt32(20)
                });
            }
        }
        catch (SqlException ex) when (ex.Number == 208 || ex.Number == 207)
        {
            return new List<BannerSlide>();
        }
        return result;
    }

    public async Task<CatalogPageViewModel> SearchProductsAsync(string? keyword, string? brand, string? category, string? cpu, string? ram, string? ssd, bool official, bool fast, bool installment, int page, int pageSize)
    {
        page = page <= 0 ? 1 : page;
        var normalizedCategoryFilter = (category ?? "").Trim();
var workstationAliases = new[] { "Gaming - Đồ họa","Gaming-Đồ họa","Gaming Đồ họa" };
var desktopAliases = new[] { "Máy tính bộ","Máy tính bàn" };
var desktopSampleNames = new[]{
"PCAPT0174 | ASUS ROG RTX 5090 Ultra 9 285K 96GB 1TB",
"PCAPT0250 | ASUS RTX 4060 Game Master i5 14400F 16GB 500GB",
"PCAPT0266 | ASUS RTX 5050 Game Master i5 14400F 16GB 500GB",
"PCAPT0275 | MSI RTX 5070 Ti Red Dragon i5 14400F 16GB 1TB",
"PCAPT0276 | MSI RTX 5080 Red Dragon Ultra 7 265KF 32GB 1TB",
"PCAPT0362 | ASUS TUF RTX 5060 Ti i5 14400F 16GB 500GB",
"Mã 55452 | ASUS TUF RTX 5060 Ti i5 12400F 8GB 500GB",
"Mã 55453 | ASUS TUF RTX 5060 Ti i5 12400F 16GB 500GB",
"Mã 55454 | ASUS TUF RTX 5060 Ti Ultra 5 225F 16GB 500GB"
};
var isDesktopOnlyMode = desktopAliases.Any(x=>x.Equals(normalizedCategoryFilter,StringComparison.OrdinalIgnoreCase));
if(workstationAliases.Any(x=>x.Equals(normalizedCategoryFilter,StringComparison.OrdinalIgnoreCase))){
    category="Máy trạm";
}

var vm = new CatalogPageViewModel
        {
            Keyword = keyword,
            Brand = brand,
            Category = category,
            Cpu = cpu,
            Ram = ram,
            Ssd = ssd,
            Official = official,
            FastDelivery = fast,
            Installment = installment,
            Page = page,
            PageSize = pageSize,
            Brands = await GetDistinctAsync("SELECT DISTINCT Brand FROM Products ORDER BY Brand"),
            Categories = await GetDistinctAsync("SELECT DISTINCT CategoryName FROM Products ORDER BY CategoryName"),
            Cpus = await GetDistinctAsync("SELECT DISTINCT Cpu FROM Products WHERE Cpu IS NOT NULL AND LTRIM(RTRIM(Cpu)) <> '' ORDER BY Cpu"),
            Rams = await GetDistinctAsync("SELECT DISTINCT Ram FROM Products WHERE Ram IS NOT NULL AND LTRIM(RTRIM(Ram)) <> '' ORDER BY Ram"),
            Ssds = await GetDistinctAsync("SELECT DISTINCT Ssd FROM Products WHERE Ssd IS NOT NULL AND LTRIM(RTRIM(Ssd)) <> '' ORDER BY Ssd")
        };

        var filters = new List<string> { "1=1" };
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var countCmd = conn.CreateCommand();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            filters.Add("(ProductName LIKE @keyword OR Brand LIKE @keyword OR Cpu LIKE @keyword)");
            countCmd.Parameters.AddWithValue("@keyword", $"%{keyword}%");
        }
        if (!string.IsNullOrWhiteSpace(brand))
        {
            filters.Add("Brand = @brand");
            countCmd.Parameters.AddWithValue("@brand", brand);
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            if(isDesktopOnlyMode){
    filters.Add("(CategoryName = N'Máy tính bộ' OR CategoryName = N'Máy tính bàn')");
}else{
    filters.Add("CategoryName = @category");
}
            countCmd.Parameters.AddWithValue("@category", category);
        }
        if (!string.IsNullOrWhiteSpace(cpu))
        {
            filters.Add("Cpu = @cpu");
            countCmd.Parameters.AddWithValue("@cpu", cpu);
        }
        if (!string.IsNullOrWhiteSpace(ram))
        {
            filters.Add("Ram = @ram");
            countCmd.Parameters.AddWithValue("@ram", ram);
        }
        if (!string.IsNullOrWhiteSpace(ssd))
        {
            filters.Add("Ssd = @ssd");
            countCmd.Parameters.AddWithValue("@ssd", ssd);
        }
        if (official)
        {
            filters.Add("1 = 1");
        }
        if (fast)
        {
            filters.Add("StockQty > 0");
        }
        if (installment)
        {
            filters.Add("Price >= 15000000");
        }
        if(isDesktopOnlyMode){
    var desktopNameParams=new List<string>();
    for(int i=0;i<desktopSampleNames.Length;i++){
        var p="@d"+i;
        desktopNameParams.Add(p);
        countCmd.Parameters.AddWithValue(p,desktopSampleNames[i]);
    }
    filters.Add($"ProductName IN ({string.Join(",",desktopNameParams)})");
}
var where = string.Join(" AND ", filters);
        countCmd.CommandText = $"SELECT COUNT(*) FROM Products WHERE {where}";
        vm.TotalItems = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        using var cmd = conn.CreateCommand();
        foreach (SqlParameter p in countCmd.Parameters)
            cmd.Parameters.AddWithValue(p.ParameterName, p.Value);
        cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@fetch", pageSize);
        cmd.CommandText = $@"
            SELECT * FROM Products
            WHERE {where}
            ORDER BY IsFeatured DESC, SortOrder, ProductId
            OFFSET @offset ROWS FETCH NEXT @fetch ROWS ONLY";
        vm.Products = await ReadProductsAsync(cmd);
        return vm;
    }

    public async Task<ProductDetailsViewModel?> GetProductByIdAsync(int id, int? star = null)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT * FROM Products WHERE ProductId = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        var product = new ProductDetailsViewModel
        {
            Id = reader.GetInt32(reader.GetOrdinal("ProductId")),
            Brand = reader["Brand"].ToString() ?? string.Empty,
            Category = reader["CategoryName"].ToString() ?? string.Empty,
            Name = reader["ProductName"].ToString() ?? string.Empty,
            Cpu = reader["Cpu"].ToString() ?? string.Empty,
            Ram = reader["Ram"].ToString() ?? string.Empty,
            Ssd = reader["Ssd"].ToString() ?? string.Empty,
            Price = Convert.ToDecimal(reader["Price"]),
            OldPrice = Convert.ToDecimal(reader["OldPrice"]),
            Stock = Convert.ToInt32(reader["StockQty"]),
            BadgeText = reader["BadgeText"].ToString() ?? string.Empty,
            Description = string.IsNullOrWhiteSpace(reader["DescriptionText"].ToString()) ? BuildSampleDescription(reader["ProductName"].ToString() ?? string.Empty, reader["Brand"].ToString() ?? string.Empty, reader["Cpu"].ToString() ?? string.Empty, reader["Ram"].ToString() ?? string.Empty, reader["Ssd"].ToString() ?? string.Empty) : reader["DescriptionText"].ToString() ?? string.Empty,
            Features = new List<string>
            {
                "Bảo hành chính hãng 12 tháng",
                "Hỗ trợ cài đặt, vệ sinh máy miễn phí",
                "Giao hàng nhanh toàn quốc",
                "Đổi trả 7 ngày nếu lỗi kỹ thuật"
            }
        };
        await reader.CloseAsync();
        product.Images = await GetProductImagesAsync(product.Id);
        product.Reviews = await GetProductReviewsAsync(product.Id, star);
        var allReviews = await GetProductReviewsAsync(product.Id, null);
        product.TotalReviews = allReviews.Count;
        product.AverageRating = product.TotalReviews == 0 ? 5.0 : Math.Round(allReviews.Average(x => x.Rating), 1);
        product.FilterStar = star;
        product.RatingSummary = Enumerable.Range(1, 5).Reverse().ToDictionary(s => s, s => allReviews.Count(r => r.Rating == s));
        return product;
    }






public async Task<List<ProductReviewViewModel>> GetProductReviewsAsync(int productId, int? star = null)
{
    var result = new List<ProductReviewViewModel>();
    using var conn = CreateConnection();
    await conn.OpenAsync();
    using var cmd = new SqlCommand("SELECT r.ReviewId, r.ProductId, p.ProductName, r.ReviewerName, r.Rating, r.CommentText, ISNULL(r.ImageUrl,''), ISNULL(r.ReplyText,''), r.ReplyCreatedAt, r.CreatedAt FROM ProductReviews r INNER JOIN Products p ON p.ProductId=r.ProductId WHERE r.ProductId=@ProductId" + (star.HasValue ? " AND r.Rating=@Star" : "") + " ORDER BY r.ReviewId DESC", conn);
    cmd.Parameters.AddWithValue("@ProductId", productId);
    if (star.HasValue) cmd.Parameters.AddWithValue("@Star", star.Value);
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        result.Add(new ProductReviewViewModel
        {
            ReviewId = reader.GetInt32(0),
            ProductId = reader.GetInt32(1),
            ProductName = reader.GetString(2),
            ReviewerName = reader.GetString(3),
            Rating = reader.GetInt32(4),
            CommentText = reader.GetString(5),
            ImageUrl = NormalizeMediaUrl(reader.GetString(6)),
            ReplyText = reader.GetString(7),
            ReplyCreatedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
            CreatedAt = reader.GetDateTime(9)
        });
    }
    return result;
}

public async Task AddProductReviewAsync(int productId, string reviewerName, int rating, string commentText, string imageUrl)
{
    using var conn = CreateConnection();
    await conn.OpenAsync();
    using var cmd = new SqlCommand(@"
        INSERT INTO ProductReviews(ProductId, ReviewerName, Rating, CommentText, ImageUrl, ReplyText, ReplyCreatedAt, CreatedAt)
        VALUES(@ProductId,@ReviewerName,@Rating,@CommentText,@ImageUrl,N'',NULL,GETDATE())", conn);
    cmd.Parameters.AddWithValue("@ProductId", productId);
    cmd.Parameters.AddWithValue("@ReviewerName", string.IsNullOrWhiteSpace(reviewerName) ? "Khách hàng" : reviewerName.Trim());
    cmd.Parameters.AddWithValue("@Rating", rating <= 0 ? 5 : rating);
    cmd.Parameters.AddWithValue("@CommentText", commentText ?? string.Empty);
    cmd.Parameters.AddWithValue("@ImageUrl", imageUrl ?? string.Empty);
    await cmd.ExecuteNonQueryAsync();
}

public async Task<List<AdminReviewItemViewModel>> GetAdminReviewsAsync(int? productId = null)
{
    var result = new List<AdminReviewItemViewModel>();
    using var conn = CreateConnection();
    await conn.OpenAsync();
    using var cmd = new SqlCommand("SELECT r.ReviewId, r.ProductId, p.ProductName, r.ReviewerName, r.Rating, r.CommentText, ISNULL(r.ImageUrl,''), ISNULL(r.ReplyText,''), r.ReplyCreatedAt, r.CreatedAt FROM ProductReviews r INNER JOIN Products p ON p.ProductId=r.ProductId" + (productId.HasValue ? " WHERE r.ProductId=@ProductId" : "") + " ORDER BY r.ReviewId DESC", conn);
    if (productId.HasValue) cmd.Parameters.AddWithValue("@ProductId", productId.Value);
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        result.Add(new AdminReviewItemViewModel
        {
            ReviewId = reader.GetInt32(0),
            ProductId = reader.GetInt32(1),
            ProductName = reader.GetString(2),
            ReviewerName = reader.GetString(3),
            Rating = reader.GetInt32(4),
            CommentText = reader.GetString(5),
            ImageUrl = NormalizeMediaUrl(reader.GetString(6)),
            ReplyText = reader.GetString(7),
            ReplyCreatedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
            CreatedAt = reader.GetDateTime(9)
        });
    }
    return result;
}

public async Task ReplyProductReviewAsync(int reviewId, string replyText)
{
    using var conn = CreateConnection();
    await conn.OpenAsync();
    using var cmd = new SqlCommand("UPDATE ProductReviews SET ReplyText=@ReplyText, ReplyCreatedAt=GETDATE() WHERE ReviewId=@ReviewId", conn);
    cmd.Parameters.AddWithValue("@ReplyText", replyText ?? string.Empty);
    cmd.Parameters.AddWithValue("@ReviewId", reviewId);
    await cmd.ExecuteNonQueryAsync();
}

public async Task DeleteProductReviewAsync(int reviewId)
{
    using var conn = CreateConnection();
    await conn.OpenAsync();
    using var cmd = new SqlCommand("DELETE FROM ProductReviews WHERE ReviewId=@ReviewId", conn);
    cmd.Parameters.AddWithValue("@ReviewId", reviewId);
    await cmd.ExecuteNonQueryAsync();
}
    public async Task<List<string>> GetProductImagesAsync(int productId)
    {
        var result = new List<string>();
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using (var cmd = new SqlCommand("SELECT ImageUrl FROM ProductImages WHERE ProductId=@id ORDER BY DisplayOrder", conn))
        {
            cmd.Parameters.AddWithValue("@id", productId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var url = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                url = NormalizeLegacyProductImageUrl(url);
                if (string.IsNullOrWhiteSpace(url)) continue;
                result.Add(url);
            }
        }

        if (result.Count > 0)
        {
            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        using var fallbackCmd = new SqlCommand("SELECT TOP 1 Brand, ProductName, CategoryName FROM Products WHERE ProductId=@id", conn);
        fallbackCmd.Parameters.AddWithValue("@id", productId);
        using var fallbackReader = await fallbackCmd.ExecuteReaderAsync();
        if (await fallbackReader.ReadAsync())
        {
            var brand = fallbackReader.IsDBNull(0) ? string.Empty : fallbackReader.GetString(0);
            var productName = fallbackReader.IsDBNull(1) ? string.Empty : fallbackReader.GetString(1);
            var categoryName = fallbackReader.IsDBNull(2) ? string.Empty : fallbackReader.GetString(2);
            return GetBrandImageSet(brand, productName, productId, categoryName)
                .Select(NormalizeLegacyProductImageUrl)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return GetBrandImageSet("Acer", "Acer", 1, string.Empty);
    }

    public async Task<string> CreateOrderAsync(CheckoutViewModel form, List<CartItem> items)
    {
        var orderCode = $"DH{DateTime.Now:yyyyMMddHHmmss}";
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            var total = items.Sum(x => x.LineTotal);
            var isPaid = string.Equals(form.PaymentMethod, "BankTransfer", StringComparison.OrdinalIgnoreCase);
            var orderStatus = isPaid ? "Đã thanh toán - chờ giao" : "Chờ thanh toán";
            using var cmd = new SqlCommand(@"
                INSERT INTO Orders(OrderCode, CustomerName, Phone, AddressLine, Note, PaymentMethod, IsPaid, OrderStatus, TotalAmount, CreatedAt)
                OUTPUT INSERTED.OrderId
                VALUES(@OrderCode,@CustomerName,@Phone,@AddressLine,@Note,@PaymentMethod,@IsPaid,@OrderStatus,@TotalAmount,GETDATE())", conn, tx);
            cmd.Parameters.AddWithValue("@OrderCode", orderCode);
            cmd.Parameters.AddWithValue("@CustomerName", form.CustomerName);
            cmd.Parameters.AddWithValue("@Phone", form.Phone);
            cmd.Parameters.AddWithValue("@AddressLine", form.Address);
            cmd.Parameters.AddWithValue("@Note", form.Note ?? string.Empty);
            cmd.Parameters.AddWithValue("@PaymentMethod", form.PaymentMethod);
            cmd.Parameters.AddWithValue("@IsPaid", isPaid);
            cmd.Parameters.AddWithValue("@OrderStatus", orderStatus);
            cmd.Parameters.AddWithValue("@TotalAmount", total);
            var orderId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            foreach (var item in items)
            {
                using var itemCmd = new SqlCommand(@"
                    INSERT INTO OrderItems(OrderId, ProductId, ProductName, UnitPrice, Quantity, LineTotal)
                    VALUES(@OrderId,@ProductId,@ProductName,@UnitPrice,@Quantity,@LineTotal)", conn, tx);
                itemCmd.Parameters.AddWithValue("@OrderId", orderId);
                itemCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                itemCmd.Parameters.AddWithValue("@ProductName", item.ProductName);
                itemCmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                itemCmd.Parameters.AddWithValue("@LineTotal", item.LineTotal);
                await itemCmd.ExecuteNonQueryAsync();

                if (isPaid)
                {
                    using var stockCmd = new SqlCommand("UPDATE Products SET StockQty = CASE WHEN StockQty >= @qty THEN StockQty - @qty ELSE 0 END WHERE ProductId=@ProductId", conn, tx);
                    stockCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    stockCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    await stockCmd.ExecuteNonQueryAsync();
                }
            }
            tx.Commit();
            return orderCode;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<List<string>> GetDistinctAsync(string sql)
    {
        var result = new List<string>();
        using var conn = CreateConnection();
        await conn.OpenAsync();
        try
        {
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(reader.GetString(0));
        }
        catch (SqlException ex) when (ex.Number == 208 || ex.Number == 207)
        {
            return new List<string>();
        }
        return result;
    }

    private async Task<List<ProductCardViewModel>> GetProductsAsync(string sql)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(sql, conn);
        return await ReadProductsAsync(cmd);
    }

    private async Task<List<ProductCardViewModel>> ReadProductsAsync(SqlCommand cmd)
    {
        var result = new List<ProductCardViewModel>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new ProductCardViewModel
            {
                Id = Convert.ToInt32(reader["ProductId"]),
                Brand = reader["Brand"].ToString() ?? string.Empty,
                Category = reader["CategoryName"].ToString() ?? string.Empty,
                Name = reader["ProductName"].ToString() ?? string.Empty,
                Cpu = reader["Cpu"].ToString() ?? string.Empty,
                Ram = reader["Ram"].ToString() ?? string.Empty,
                Ssd = reader["Ssd"].ToString() ?? string.Empty,
                Price = Convert.ToDecimal(reader["Price"]),
                OldPrice = Convert.ToDecimal(reader["OldPrice"]),
                Stock = Convert.ToInt32(reader["StockQty"]),
                BadgeText = reader["BadgeText"].ToString() ?? string.Empty,
                IsOfficial = true,
                IsFastDelivery = Convert.ToInt32(reader["StockQty"]) > 0,
                IsInstallment = Convert.ToDecimal(reader["Price"]) >= 15000000m,
                OfficialText = "Hàng chính hãng",
                FastDeliveryText = Convert.ToInt32(reader["StockQty"]) > 10 ? "Giao nhanh 2H nội thành" : "Giao nhanh toàn quốc",
                InstallmentText = Convert.ToDecimal(reader["Price"]) >= 15000000m ? "Trả góp 0% qua thẻ" : "Hỗ trợ trả góp"
            });
        }
        await reader.CloseAsync();

        foreach (var item in result)
        {
            item.Images = await GetProductImagesAsync(item.Id);
        }
        return result;
    }

    public async Task<List<InventoryItemViewModel>> GetInventoryAsync(string? keyword = null, string? brand = null, string? category = null)
    {
        var result = new List<InventoryItemViewModel>();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        var filters = new List<string> { "1=1" };
        using var cmd = conn.CreateCommand();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            filters.Add("(p.ProductName LIKE @keyword OR p.Brand LIKE @keyword OR p.CategoryName LIKE @keyword)");
            cmd.Parameters.AddWithValue("@keyword", $"%{keyword.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(brand))
        {
            filters.Add("p.Brand = @brand");
            cmd.Parameters.AddWithValue("@brand", brand.Trim());
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            filters.Add("p.CategoryName = @category");
            cmd.Parameters.AddWithValue("@category", category.Trim());
        }

        var where = string.Join(" AND ", filters);
        cmd.CommandText = $@"
            SELECT p.ProductId, p.Brand, p.CategoryName, p.ProductName, p.Cpu, p.Ram, p.Ssd, p.Price, p.StockQty, p.IsFeatured,
                   ISNULL((SELECT TOP 1 ImageUrl FROM ProductImages pi WHERE pi.ProductId = p.ProductId ORDER BY DisplayOrder), '') AS Thumbnail
            FROM Products p
            WHERE {where}
            ORDER BY p.Brand, p.CategoryName, p.ProductName";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var productId = reader.GetInt32(0);
            var brandValue = reader.GetString(1);
            var categoryValue = reader.GetString(2);
            var productNameValue = reader.GetString(3);
            var thumbnail = NormalizeLegacyProductImageUrl(reader.GetString(10));
            if (string.IsNullOrWhiteSpace(thumbnail) || string.Equals(thumbnail, "/images/products/acer/acer-001.jpg", StringComparison.OrdinalIgnoreCase))
            {
                thumbnail = GetBrandImageSet(brandValue, productNameValue, productId, categoryValue)
                    .Select(NormalizeLegacyProductImageUrl)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                    ?? "/images/products/acer/acer-001.jpg";
            }

            result.Add(new InventoryItemViewModel
            {
                ProductId = productId,
                Brand = brandValue,
                CategoryName = categoryValue,
                ProductName = productNameValue,
                Cpu = reader.GetString(4),
                Ram = reader.GetString(5),
                Ssd = reader.GetString(6),
                Price = reader.GetDecimal(7),
                StockQty = reader.GetInt32(8),
                IsFeatured = reader.GetBoolean(9),
                Thumbnail = thumbnail
            });
        }
        return result;
    }

    public async Task<InventoryFormViewModel?> GetInventoryFormByIdAsync(int productId)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            SELECT p.ProductId, p.Brand, p.CategoryName, p.ProductName, p.Cpu, p.Ram, p.Ssd, p.Price, p.OldPrice,
                   p.StockQty, p.IsFeatured, p.BadgeText, p.DescriptionText, p.DiscountPercent, p.SortOrder
            FROM Products p WHERE p.ProductId=@ProductId", conn);
        cmd.Parameters.AddWithValue("@ProductId", productId);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        var vm = new InventoryFormViewModel
        {
            ProductId = reader.GetInt32(0),
            Brand = reader.GetString(1),
            CategoryName = reader.GetString(2),
            ProductName = reader.GetString(3),
            Cpu = reader.GetString(4),
            Ram = reader.GetString(5),
            Ssd = reader.GetString(6),
            Price = reader.GetDecimal(7),
            OldPrice = reader.GetDecimal(8),
            StockQty = reader.GetInt32(9),
            IsFeatured = reader.GetBoolean(10),
            BadgeText = reader.GetString(11),
            DescriptionText = reader.GetString(12),
            DiscountPercent = reader.GetInt32(13),
            SortOrder = reader.GetInt32(14)
        };
        await reader.CloseAsync();

        var images = await GetProductImagesAsync(productId);
        vm.ExistingImages = images;
        vm.ImageGalleryText = string.Join(Environment.NewLine, images);
        return vm;
    }

    public async Task<int> CreateProductAsync(InventoryFormViewModel model)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd = new SqlCommand(@"
                INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder)
                OUTPUT INSERTED.ProductId
                VALUES(@Brand,@CategoryName,@ProductName,@Cpu,@Ram,@Ssd,@Price,@OldPrice,@StockQty,@IsFeatured,@BadgeText,@DescriptionText,@DiscountPercent,@SortOrder)", conn, tx);
            cmd.Parameters.AddWithValue("@Brand", model.Brand);
            cmd.Parameters.AddWithValue("@CategoryName", model.CategoryName);
            cmd.Parameters.AddWithValue("@ProductName", model.ProductName);
            cmd.Parameters.AddWithValue("@Cpu", model.Cpu);
            cmd.Parameters.AddWithValue("@Ram", model.Ram);
            cmd.Parameters.AddWithValue("@Ssd", model.Ssd);
            cmd.Parameters.AddWithValue("@Price", model.Price);
            cmd.Parameters.AddWithValue("@OldPrice", model.OldPrice);
            cmd.Parameters.AddWithValue("@StockQty", model.StockQty);
            cmd.Parameters.AddWithValue("@IsFeatured", model.IsFeatured);
            cmd.Parameters.AddWithValue("@BadgeText", model.BadgeText);
            cmd.Parameters.AddWithValue("@DescriptionText", model.DescriptionText ?? string.Empty);
            cmd.Parameters.AddWithValue("@DiscountPercent", model.DiscountPercent);
            cmd.Parameters.AddWithValue("@SortOrder", model.SortOrder);
            var productId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            await SaveProductImagesAsync(conn, tx, productId, model);
            tx.Commit();
            return productId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task UpdateProductAsync(InventoryFormViewModel model)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd = new SqlCommand(@"
                UPDATE Products SET
                    Brand=@Brand, CategoryName=@CategoryName, ProductName=@ProductName, Cpu=@Cpu, Ram=@Ram, Ssd=@Ssd,
                    Price=@Price, OldPrice=@OldPrice, StockQty=@StockQty, IsFeatured=@IsFeatured, BadgeText=@BadgeText,
                    DescriptionText=@DescriptionText, DiscountPercent=@DiscountPercent, SortOrder=@SortOrder
                WHERE ProductId=@ProductId", conn, tx);
            cmd.Parameters.AddWithValue("@ProductId", model.ProductId);
            cmd.Parameters.AddWithValue("@Brand", model.Brand);
            cmd.Parameters.AddWithValue("@CategoryName", model.CategoryName);
            cmd.Parameters.AddWithValue("@ProductName", model.ProductName);
            cmd.Parameters.AddWithValue("@Cpu", model.Cpu);
            cmd.Parameters.AddWithValue("@Ram", model.Ram);
            cmd.Parameters.AddWithValue("@Ssd", model.Ssd);
            cmd.Parameters.AddWithValue("@Price", model.Price);
            cmd.Parameters.AddWithValue("@OldPrice", model.OldPrice);
            cmd.Parameters.AddWithValue("@StockQty", model.StockQty);
            cmd.Parameters.AddWithValue("@IsFeatured", model.IsFeatured);
            cmd.Parameters.AddWithValue("@BadgeText", model.BadgeText);
            cmd.Parameters.AddWithValue("@DescriptionText", model.DescriptionText ?? string.Empty);
            cmd.Parameters.AddWithValue("@DiscountPercent", model.DiscountPercent);
            cmd.Parameters.AddWithValue("@SortOrder", model.SortOrder);
            await cmd.ExecuteNonQueryAsync();

            using var delCmd = new SqlCommand("DELETE FROM ProductImages WHERE ProductId=@ProductId", conn, tx);
            delCmd.Parameters.AddWithValue("@ProductId", model.ProductId);
            await delCmd.ExecuteNonQueryAsync();
            await SaveProductImagesAsync(conn, tx, model.ProductId, model);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task UpdateStockAsync(int productId, int stockQty)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("UPDATE Products SET StockQty=@StockQty WHERE ProductId=@ProductId", conn);
        cmd.Parameters.AddWithValue("@ProductId", productId);
        cmd.Parameters.AddWithValue("@StockQty", stockQty);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteProductAsync(int productId)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            using var delImg = new SqlCommand("DELETE FROM ProductImages WHERE ProductId=@ProductId", conn, tx);
            delImg.Parameters.AddWithValue("@ProductId", productId);
            await delImg.ExecuteNonQueryAsync();

            using var delProduct = new SqlCommand("DELETE FROM Products WHERE ProductId=@ProductId", conn, tx);
            delProduct.Parameters.AddWithValue("@ProductId", productId);
            await delProduct.ExecuteNonQueryAsync();

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    
public async Task<List<AdminOrderViewModel>> GetAdminOrdersAsync(string status)
{
    var result = new List<AdminOrderViewModel>();
    using var conn = CreateConnection();
    await conn.OpenAsync();
    using var cmd = new SqlCommand();
    cmd.Connection = conn;
    var where = status switch
    {
        "paid" => " WHERE IsPaid=1",
        "unpaid" => " WHERE IsPaid=0",
        "delivered" => " WHERE OrderStatus IN (N'Đã giao hàng', N'Đã giao hàng và thanh toán')",
        "cancelled" => " WHERE OrderStatus = N'Đã hủy'",
        _ => string.Empty
    };
    cmd.CommandText = "SELECT OrderId, OrderCode, CreatedAt, CustomerName, Phone, AddressLine, Note, PaymentMethod, IsPaid, TotalAmount, OrderStatus FROM Orders" + where + " ORDER BY OrderId DESC";
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        result.Add(new AdminOrderViewModel
        {
            OrderId = reader.GetInt32(0),
            OrderCode = reader.GetString(1),
            CreatedAt = reader.GetDateTime(2),
            CustomerName = reader.GetString(3),
            Phone = reader.GetString(4),
            AddressLine = reader.GetString(5),
            Note = reader.GetString(6),
            PaymentMethod = reader.GetString(7),
            IsPaid = reader.GetBoolean(8),
            TotalAmount = reader.GetDecimal(9),
            OrderStatus = reader.GetString(10)
        });
        var created = result[^1];
        created.CanCancel = !string.Equals(created.OrderStatus, "Đã hủy", StringComparison.OrdinalIgnoreCase);
        created.CanMarkDelivered = !string.Equals(created.OrderStatus, "Đã hủy", StringComparison.OrdinalIgnoreCase)
                                  && !string.Equals(created.OrderStatus, "Đã giao hàng", StringComparison.OrdinalIgnoreCase)
                                  && !string.Equals(created.OrderStatus, "Đã giao hàng và thanh toán", StringComparison.OrdinalIgnoreCase);
    }
    await reader.CloseAsync();

    foreach (var order in result)
    {
        using var itemCmd = new SqlCommand("SELECT ProductId, ProductName, UnitPrice, Quantity, LineTotal FROM OrderItems WHERE OrderId=@OrderId", conn);
        itemCmd.Parameters.AddWithValue("@OrderId", order.OrderId);
        using var itemReader = await itemCmd.ExecuteReaderAsync();
        while (await itemReader.ReadAsync())
        {
            order.Items.Add(new CartItem
            {
                ProductId = itemReader.GetInt32(0),
                ProductName = itemReader.GetString(1),
                UnitPrice = itemReader.GetDecimal(2),
                Quantity = itemReader.GetInt32(3)
            });
        }
        await itemReader.CloseAsync();
    }
    return result;
}



    public async Task MarkOrderDeliveredAsync(int orderId)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            bool isPaid = false;
            string paymentMethod = string.Empty;
            string currentStatus = string.Empty;

            using (var getCmd = new SqlCommand("SELECT IsPaid, PaymentMethod, OrderStatus FROM Orders WHERE OrderId=@OrderId", conn, tx))
            {
                getCmd.Parameters.AddWithValue("@OrderId", orderId);
                using var reader = await getCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return;
                isPaid = reader.GetBoolean(0);
                paymentMethod = reader.GetString(1);
                currentStatus = reader.GetString(2);
            }

            if (string.Equals(currentStatus, "Đã hủy", StringComparison.OrdinalIgnoreCase)) { tx.Rollback(); return; }

            if (!isPaid)
            {
                using var itemCmd = new SqlCommand("SELECT ProductId, Quantity FROM OrderItems WHERE OrderId=@OrderId", conn, tx);
                itemCmd.Parameters.AddWithValue("@OrderId", orderId);
                using var itemReader = await itemCmd.ExecuteReaderAsync();
                var items = new List<(int productId, int qty)>();
                while (await itemReader.ReadAsync())
                    items.Add((itemReader.GetInt32(0), itemReader.GetInt32(1)));
                await itemReader.CloseAsync();

                foreach (var item in items)
                {
                    using var stockCmd = new SqlCommand("UPDATE Products SET StockQty = CASE WHEN StockQty >= @qty THEN StockQty - @qty ELSE 0 END WHERE ProductId=@ProductId", conn, tx);
                    stockCmd.Parameters.AddWithValue("@qty", item.qty);
                    stockCmd.Parameters.AddWithValue("@ProductId", item.productId);
                    await stockCmd.ExecuteNonQueryAsync();
                }

                using var updateCmd = new SqlCommand("UPDATE Orders SET IsPaid=1, PaymentMethod=@PaymentMethod, OrderStatus=N'Đã giao hàng và thanh toán' WHERE OrderId=@OrderId", conn, tx);
                updateCmd.Parameters.AddWithValue("@OrderId", orderId);
                updateCmd.Parameters.AddWithValue("@PaymentMethod", string.IsNullOrWhiteSpace(paymentMethod) ? "PayLater" : paymentMethod);
                await updateCmd.ExecuteNonQueryAsync();
            }
            else
            {
                using var updateCmd = new SqlCommand("UPDATE Orders SET OrderStatus=N'Đã giao hàng' WHERE OrderId=@OrderId", conn, tx);
                updateCmd.Parameters.AddWithValue("@OrderId", orderId);
                await updateCmd.ExecuteNonQueryAsync();
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task CancelOrderAsync(int orderId)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            bool isPaid = false;
            string currentStatus = string.Empty;

            using (var getCmd = new SqlCommand("SELECT IsPaid, OrderStatus FROM Orders WHERE OrderId=@OrderId", conn, tx))
            {
                getCmd.Parameters.AddWithValue("@OrderId", orderId);
                using var reader = await getCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return;
                isPaid = reader.GetBoolean(0);
                currentStatus = reader.GetString(1);
            }

            if (string.Equals(currentStatus, "Đã hủy", StringComparison.OrdinalIgnoreCase)) { tx.Rollback(); return; }

            var shouldReturnStock = isPaid || string.Equals(currentStatus, "Đã giao hàng", StringComparison.OrdinalIgnoreCase) || string.Equals(currentStatus, "Đã giao hàng và thanh toán", StringComparison.OrdinalIgnoreCase);

            if (shouldReturnStock)
            {
                using var itemCmd = new SqlCommand("SELECT ProductId, Quantity FROM OrderItems WHERE OrderId=@OrderId", conn, tx);
                itemCmd.Parameters.AddWithValue("@OrderId", orderId);
                using var itemReader = await itemCmd.ExecuteReaderAsync();
                var items = new List<(int productId, int qty)>();
                while (await itemReader.ReadAsync())
                    items.Add((itemReader.GetInt32(0), itemReader.GetInt32(1)));
                await itemReader.CloseAsync();

                foreach (var item in items)
                {
                    using var stockCmd = new SqlCommand("UPDATE Products SET StockQty = StockQty + @qty WHERE ProductId=@ProductId", conn, tx);
                    stockCmd.Parameters.AddWithValue("@qty", item.qty);
                    stockCmd.Parameters.AddWithValue("@ProductId", item.productId);
                    await stockCmd.ExecuteNonQueryAsync();
                }
            }

            using var updateCmd = new SqlCommand("UPDATE Orders SET OrderStatus=N'Đã hủy' WHERE OrderId=@OrderId", conn, tx);
            updateCmd.Parameters.AddWithValue("@OrderId", orderId);
            await updateCmd.ExecuteNonQueryAsync();

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }


    public async Task<AdminOrderViewModel?> GetAdminOrderDetailAsync(int orderId)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT OrderId, OrderCode, CreatedAt, CustomerName, Phone, AddressLine, Note, PaymentMethod, IsPaid, TotalAmount, OrderStatus FROM Orders WHERE OrderId=@OrderId", conn);
        cmd.Parameters.AddWithValue("@OrderId", orderId);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        var order = new AdminOrderViewModel
        {
            OrderId = reader.GetInt32(0),
            OrderCode = reader.GetString(1),
            CreatedAt = reader.GetDateTime(2),
            CustomerName = reader.GetString(3),
            Phone = reader.GetString(4),
            AddressLine = reader.GetString(5),
            Note = reader.GetString(6),
            PaymentMethod = reader.GetString(7),
            IsPaid = reader.GetBoolean(8),
            TotalAmount = reader.GetDecimal(9),
            OrderStatus = reader.GetString(10)
        };
        order.CanCancel = !string.Equals(order.OrderStatus, "Đã hủy", StringComparison.OrdinalIgnoreCase);
        order.CanMarkDelivered = !string.Equals(order.OrderStatus, "Đã hủy", StringComparison.OrdinalIgnoreCase)
                                 && !string.Equals(order.OrderStatus, "Đã giao hàng", StringComparison.OrdinalIgnoreCase)
                                 && !string.Equals(order.OrderStatus, "Đã giao hàng và thanh toán", StringComparison.OrdinalIgnoreCase);
        await reader.CloseAsync();

        using var itemCmd = new SqlCommand("SELECT ProductId, ProductName, UnitPrice, Quantity FROM OrderItems WHERE OrderId=@OrderId", conn);
        itemCmd.Parameters.AddWithValue("@OrderId", orderId);
        using var itemReader = await itemCmd.ExecuteReaderAsync();
        while (await itemReader.ReadAsync())
        {
            order.Items.Add(new CartItem
            {
                ProductId = itemReader.GetInt32(0),
                ProductName = itemReader.GetString(1),
                UnitPrice = itemReader.GetDecimal(2),
                Quantity = itemReader.GetInt32(3)
            });
        }
        return order;
    }

    private static async Task SaveProductImagesAsync(SqlConnection conn, SqlTransaction tx, int productId, InventoryFormViewModel model)
    {
        var images = (model.ExistingImages ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (images.Count == 0)
        {
            images = GetBrandImageSet(model.Brand, model.ProductName, productId).Take(3).ToList();
        }

        for (var index = 0; index < images.Count; index++)
        {
            using var imgCmd = new SqlCommand("INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) VALUES(@ProductId,@ImageUrl,@DisplayOrder)", conn, tx);
            imgCmd.Parameters.AddWithValue("@ProductId", productId);
            imgCmd.Parameters.AddWithValue("@ImageUrl", images[index]);
            imgCmd.Parameters.AddWithValue("@DisplayOrder", index + 1);
            await imgCmd.ExecuteNonQueryAsync();
        }
    }

    public async Task<(int products, int totalStock, int lowStock, int orders)> GetAdminSummaryAsync()
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            SELECT
                (SELECT COUNT(*) FROM Products),
                (SELECT ISNULL(SUM(StockQty),0) FROM Products),
                (SELECT COUNT(*) FROM Products WHERE StockQty <= 5),
                (SELECT COUNT(*) FROM Orders)", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3));
    }

    private static string BuildSampleDescription(string name, string brand, string cpu, string ram, string ssd)
    {
        return $@"<p><strong>{name}</strong> là mẫu laptop nổi bật của <strong>{brand}</strong>, phù hợp cho học tập, văn phòng và giải trí hằng ngày.</p>
<h3>Hiệu năng nổi bật</h3>
<ul>
<li>CPU: {cpu}</li>
<li>RAM: {ram}</li>
<li>SSD: {ssd}</li>
<li>Thiết kế hiện đại, dễ mang theo</li>
</ul>
<p>Sản phẩm mang lại trải nghiệm mượt mà cho các tác vụ văn phòng, học trực tuyến, thiết kế cơ bản và giải trí đa phương tiện. Bàn phím êm, màn hình sắc nét cùng thời lượng pin ổn định giúp bạn yên tâm sử dụng mỗi ngày.</p>
<p><img src='/images/banners/slide2.svg' alt='Mô tả sản phẩm' /></p>
<p><strong>Phù hợp với:</strong> sinh viên, nhân viên văn phòng, người dùng cần một chiếc laptop cân bằng giữa hiệu năng và tính di động.</p>";
    }


    public async Task<List<string>> GetTopBrandsForMenuAsync(int top = 8)
    {
        var key = $"{TopBrandsCacheKey}:{top}";
        if (_cache.TryGetValue(key, out List<string>? cached) && cached is not null)
            return cached;

        var sql = $"SELECT DISTINCT TOP {top} Brand FROM Products WHERE ISNULL(LTRIM(RTRIM(Brand)),N'') <> N'' ORDER BY Brand";
        var result = await GetDistinctAsync(sql);
        _cache.Set(key, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<List<string>> GetTopCategoriesForMenuAsync(int top = 8)
    {
        var key = $"{TopCategoriesCacheKey}:{top}";
        if (_cache.TryGetValue(key, out List<string>? cached) && cached is not null)
            return cached;

        var sql = $"SELECT DISTINCT TOP {top} CategoryName FROM Products WHERE ISNULL(LTRIM(RTRIM(CategoryName)),N'') <> N'' ORDER BY CategoryName";
        var result = await GetDistinctAsync(sql);
        _cache.Set(key, result, TimeSpan.FromMinutes(5));
        return result;
    }

public async Task<AdminCatalogViewModel> GetAdminCatalogAsync(string? brand, string? category, string? keyword)
{
    var vm = new AdminCatalogViewModel
    {
        Brand = brand,
        Category = category,
        Keyword = keyword,
        Brands = await GetDistinctAsync("SELECT DISTINCT Brand FROM Products ORDER BY Brand"),
        Categories = await GetDistinctAsync("SELECT DISTINCT CategoryName FROM Products ORDER BY CategoryName"),
        Items = await GetInventoryAsync()
    };

    if (!string.IsNullOrWhiteSpace(brand))
        vm.Items = vm.Items.Where(x => x.Brand == brand).ToList();
    if (!string.IsNullOrWhiteSpace(category))
        vm.Items = vm.Items.Where(x => x.CategoryName == category).ToList();
    if (!string.IsNullOrWhiteSpace(keyword))
        vm.Items = vm.Items.Where(x =>
            x.ProductName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            x.Brand.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            x.CategoryName.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

    return vm;
}

public async Task<int> ImportProductsAsync(List<InventoryImportRow> rows)
{
    int imported = 0;
    using var conn = CreateConnection();
    await conn.OpenAsync();
    using var tx = conn.BeginTransaction();
    try
    {
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Brand) || string.IsNullOrWhiteSpace(row.CategoryName) || string.IsNullOrWhiteSpace(row.ProductName))
                continue;

            using var cmd = new SqlCommand(@"
                INSERT INTO Products(Brand, CategoryName, ProductName, Cpu, Ram, Ssd, Price, OldPrice, StockQty, IsFeatured, BadgeText, DescriptionText, DiscountPercent, SortOrder)
                OUTPUT INSERTED.ProductId
                VALUES(@Brand,@CategoryName,@ProductName,@Cpu,@Ram,@Ssd,@Price,@OldPrice,@StockQty,@IsFeatured,@BadgeText,@DescriptionText,@DiscountPercent,@SortOrder)", conn, tx);
            cmd.Parameters.AddWithValue("@Brand", row.Brand);
            cmd.Parameters.AddWithValue("@CategoryName", row.CategoryName);
            cmd.Parameters.AddWithValue("@ProductName", row.ProductName);
            cmd.Parameters.AddWithValue("@Cpu", row.Cpu);
            cmd.Parameters.AddWithValue("@Ram", row.Ram);
            cmd.Parameters.AddWithValue("@Ssd", row.Ssd);
            cmd.Parameters.AddWithValue("@Price", row.Price);
            cmd.Parameters.AddWithValue("@OldPrice", row.OldPrice <= 0 ? row.Price : row.OldPrice);
            cmd.Parameters.AddWithValue("@StockQty", row.StockQty);
            cmd.Parameters.AddWithValue("@IsFeatured", row.IsFeatured);
            cmd.Parameters.AddWithValue("@BadgeText", row.BadgeText);
            cmd.Parameters.AddWithValue("@DescriptionText", row.DescriptionText ?? string.Empty);
            cmd.Parameters.AddWithValue("@DiscountPercent", row.DiscountPercent);
            cmd.Parameters.AddWithValue("@SortOrder", row.SortOrder);
            var productId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            var images = new[] { row.Image1, row.Image2, row.Image3 }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (images.Count == 0)
            {
                images = GetBrandImageSet(row.Brand, row.ProductName, productId).Take(3).ToList();
            }

            for (var index = 0; index < images.Count; index++)
            {
                using var imgCmd = new SqlCommand("INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) VALUES(@ProductId,@ImageUrl,@DisplayOrder)", conn, tx);
                imgCmd.Parameters.AddWithValue("@ProductId", productId);
                imgCmd.Parameters.AddWithValue("@ImageUrl", images[index]);
                imgCmd.Parameters.AddWithValue("@DisplayOrder", index + 1);
                await imgCmd.ExecuteNonQueryAsync();
            }

            imported++;
        }

        tx.Commit();
        return imported;
    }
    catch
    {
        tx.Rollback();
        throw;
    }
}

public async Task<List<PolicyPageViewModel>> GetPolicyPagesAsync()
{
    var items = new List<PolicyPageViewModel>();
    using var conn = CreateConnection();
    await conn.OpenAsync();
    using var cmd = new SqlCommand(@"SELECT PolicyId, PolicyKey, MenuLabel, Title, Summary, ContentHtml, SeoTitle, MetaDescription, DisplayOrder FROM PolicyPages ORDER BY DisplayOrder, PolicyId", conn);
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        items.Add(new PolicyPageViewModel
        {
            PolicyId = reader.GetInt32(0),
            PolicyKey = reader.GetString(1),
            MenuLabel = reader.GetString(2),
            Title = reader.GetString(3),
            Summary = reader.GetString(4),
            ContentHtml = reader.GetString(5),
            SeoTitle = reader.GetString(6),
            MetaDescription = reader.GetString(7),
            DisplayOrder = reader.GetInt32(8)
        });
    }
    return items;
}

public async Task<List<PolicyPageViewModel>> GetAboutFooterPoliciesAsync()
{
    var policies = await GetPolicyPagesAsync();
    var keys = new[] { "lich-su", "dao-duc-va-chinh-truc", "cam-ket-cua-chung-toi", "dao-tao-nhan-su", "huong-dan-mua-hang", "chinh-sach-ban-hang" };
    return policies
        .Where(x => keys.Contains(x.PolicyKey, StringComparer.OrdinalIgnoreCase))
        .OrderBy(x => x.DisplayOrder)
        .ThenBy(x => x.PolicyId)
        .ToList();
}

public async Task<PolicyPageViewModel?> GetPolicyByKeyAsync(string key)
{
    using var conn = CreateConnection();
    await conn.OpenAsync();
    using var cmd = new SqlCommand(@"SELECT TOP 1 PolicyId, PolicyKey, MenuLabel, Title, Summary, ContentHtml, SeoTitle, MetaDescription, DisplayOrder FROM PolicyPages WHERE PolicyKey=@PolicyKey", conn);
    cmd.Parameters.AddWithValue("@PolicyKey", key);
    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) return null;
    return new PolicyPageViewModel
    {
        PolicyId = reader.GetInt32(0),
        PolicyKey = reader.GetString(1),
        MenuLabel = reader.GetString(2),
        Title = reader.GetString(3),
        Summary = reader.GetString(4),
        ContentHtml = reader.GetString(5),
        SeoTitle = reader.GetString(6),
        MetaDescription = reader.GetString(7),
        DisplayOrder = reader.GetInt32(8)
    };
}

public async Task SavePolicyPagesAsync(List<PolicyPageViewModel> policies)
{
    using var conn = CreateConnection();
    await conn.OpenAsync();
    using var tx = conn.BeginTransaction();
    try
    {
        foreach (var item in policies.OrderBy(x => x.DisplayOrder).ThenBy(x => x.PolicyId))
        {
            using var cmd = new SqlCommand(@"
UPDATE PolicyPages
SET MenuLabel=@MenuLabel,
    Title=@Title,
    Summary=@Summary,
    ContentHtml=@ContentHtml,
    SeoTitle=@SeoTitle,
    MetaDescription=@MetaDescription,
    DisplayOrder=@DisplayOrder
WHERE PolicyId=@PolicyId", conn, tx);
            cmd.Parameters.AddWithValue("@PolicyId", item.PolicyId);
            cmd.Parameters.AddWithValue("@MenuLabel", item.MenuLabel ?? string.Empty);
            cmd.Parameters.AddWithValue("@Title", item.Title ?? string.Empty);
            cmd.Parameters.AddWithValue("@Summary", item.Summary ?? string.Empty);
            cmd.Parameters.AddWithValue("@ContentHtml", item.ContentHtml ?? string.Empty);
            cmd.Parameters.AddWithValue("@SeoTitle", item.SeoTitle ?? string.Empty);
            cmd.Parameters.AddWithValue("@MetaDescription", item.MetaDescription ?? string.Empty);
            cmd.Parameters.AddWithValue("@DisplayOrder", item.DisplayOrder);
            await cmd.ExecuteNonQueryAsync();
        }
        tx.Commit();
    }
    catch
    {
        tx.Rollback();
        throw;
    }
}


public async Task<List<TechNewsPostViewModel>> GetTechNewsPostsAsync(int take = 50)
{
    var items = new List<TechNewsPostViewModel>();
    using var conn = CreateConnection();
    await conn.OpenAsync();
    using var cmd = new SqlCommand(@"SELECT TOP (@Take) PostId, Slug, CategoryName, Title, Summary, ContentHtml, SeoTitle, MetaDescription, ThumbnailUrl, PublishedAtText, DisplayOrder, IsPublished FROM TechNewsPosts WHERE IsPublished=1 ORDER BY DisplayOrder, PostId", conn);
    cmd.Parameters.AddWithValue("@Take", take);
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var slugValue = reader.GetString(1);
        items.Add(new TechNewsPostViewModel
        {
            PostId = reader.GetInt32(0),
            Slug = slugValue,
            CategoryName = reader.GetString(2),
            Title = reader.GetString(3),
            Summary = reader.GetString(4),
            ContentHtml = reader.GetString(5),
            SeoTitle = reader.GetString(6),
            MetaDescription = reader.GetString(7),
            ThumbnailUrl = NormalizeTechNewsThumbnailUrl(slugValue, reader.GetString(8)),
            PublishedAtText = reader.GetString(9),
            DisplayOrder = reader.GetInt32(10),
            IsPublished = reader.GetBoolean(11)
        });
    }
    return items;
}

public async Task<TechNewsPostViewModel?> GetTechNewsPostBySlugAsync(string slug)
{
    using var conn = CreateConnection();
    await conn.OpenAsync();
    using var cmd = new SqlCommand(@"SELECT TOP 1 PostId, Slug, CategoryName, Title, Summary, ContentHtml, SeoTitle, MetaDescription, ThumbnailUrl, PublishedAtText, DisplayOrder, IsPublished FROM TechNewsPosts WHERE Slug=@Slug AND IsPublished=1", conn);
    cmd.Parameters.AddWithValue("@Slug", slug);
    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) return null;
    var slugValue = reader.GetString(1);
    return new TechNewsPostViewModel
    {
        PostId = reader.GetInt32(0),
        Slug = slugValue,
        CategoryName = reader.GetString(2),
        Title = reader.GetString(3),
        Summary = reader.GetString(4),
        ContentHtml = reader.GetString(5),
        SeoTitle = reader.GetString(6),
        MetaDescription = reader.GetString(7),
        ThumbnailUrl = NormalizeTechNewsThumbnailUrl(slugValue, reader.GetString(8)),
        PublishedAtText = reader.GetString(9),
        DisplayOrder = reader.GetInt32(10),
        IsPublished = reader.GetBoolean(11)
    };
}

public async Task SaveTechNewsPostsAsync(List<TechNewsPostViewModel> posts)
{
    using var conn = CreateConnection();
    await conn.OpenAsync();
    using var tx = conn.BeginTransaction();
    try
    {
        foreach (var item in posts.OrderBy(x => x.DisplayOrder).ThenBy(x => x.PostId))
        {
            using var cmd = new SqlCommand(@"
UPDATE TechNewsPosts
SET CategoryName=@CategoryName,
    Title=@Title,
    Summary=@Summary,
    ContentHtml=@ContentHtml,
    SeoTitle=@SeoTitle,
    MetaDescription=@MetaDescription,
    ThumbnailUrl=@ThumbnailUrl,
    PublishedAtText=@PublishedAtText,
    DisplayOrder=@DisplayOrder,
    IsPublished=@IsPublished
WHERE PostId=@PostId", conn, tx);
            cmd.Parameters.AddWithValue("@PostId", item.PostId);
            cmd.Parameters.AddWithValue("@CategoryName", item.CategoryName ?? string.Empty);
            cmd.Parameters.AddWithValue("@Title", item.Title ?? string.Empty);
            cmd.Parameters.AddWithValue("@Summary", item.Summary ?? string.Empty);
            cmd.Parameters.AddWithValue("@ContentHtml", item.ContentHtml ?? string.Empty);
            cmd.Parameters.AddWithValue("@SeoTitle", item.SeoTitle ?? string.Empty);
            cmd.Parameters.AddWithValue("@MetaDescription", item.MetaDescription ?? string.Empty);
            cmd.Parameters.AddWithValue("@ThumbnailUrl", item.ThumbnailUrl ?? string.Empty);
            cmd.Parameters.AddWithValue("@PublishedAtText", item.PublishedAtText ?? string.Empty);
            cmd.Parameters.AddWithValue("@DisplayOrder", item.DisplayOrder);
            cmd.Parameters.AddWithValue("@IsPublished", item.IsPublished);
            await cmd.ExecuteNonQueryAsync();
        }
        tx.Commit();
    }
    catch
    {
        tx.Rollback();
        throw;
    }
}



    public async Task SyncBrandProductImagesAsync()
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            var products = new List<(int ProductId, string Brand, string ProductName, string CategoryName)>();
            using (var cmd = new SqlCommand("SELECT ProductId, Brand, ProductName, CategoryName FROM Products ORDER BY ProductId", conn, tx))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    products.Add((reader.GetInt32(0), reader.IsDBNull(1) ? string.Empty : reader.GetString(1), reader.IsDBNull(2) ? string.Empty : reader.GetString(2), reader.IsDBNull(3) ? string.Empty : reader.GetString(3)));
                }
            }

            foreach (var product in products)
            {
                var images = GetBrandImageSet(product.Brand, product.ProductName, product.ProductId, product.CategoryName).Take(3).ToList();
                if (images.Count == 0)
                {
                    continue;
                }

                using (var delCmd = new SqlCommand("DELETE FROM ProductImages WHERE ProductId=@ProductId", conn, tx))
                {
                    delCmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                    await delCmd.ExecuteNonQueryAsync();
                }

                for (var index = 0; index < images.Count; index++)
                {
                    using var imgCmd = new SqlCommand("INSERT INTO ProductImages(ProductId, ImageUrl, DisplayOrder) VALUES(@ProductId,@ImageUrl,@DisplayOrder)", conn, tx);
                    imgCmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                    imgCmd.Parameters.AddWithValue("@ImageUrl", images[index]);
                    imgCmd.Parameters.AddWithValue("@DisplayOrder", index + 1);
                    await imgCmd.ExecuteNonQueryAsync();
                }
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static List<string> GetBrandImageSet(string? brand, string? productName, int productId, string? categoryName = null)
    {
        var key = ResolveImageBrandKey(brand, productName, categoryName);
        if (!BrandImageCatalog.TryGetValue(key, out var pool) || pool.Count == 0)
        {
            return GetBrandImageSet("Acer", "Acer", 1, string.Empty);
        }

        var start = Math.Abs(productId) % pool.Count;
        var result = new List<string>();
        for (var i = 0; i < Math.Min(3, pool.Count); i++)
        {
            result.Add(pool[(start + i) % pool.Count]);
        }
        return result;
    }

    private static string ResolveImageBrandKey(string? brand, string? productName, string? categoryName = null)
    {
        var brandValue = (brand ?? string.Empty).Trim().ToLowerInvariant();
        var nameValue = (productName ?? string.Empty).Trim().ToLowerInvariant();
        var categoryValue = (categoryName ?? string.Empty).Trim().ToLowerInvariant();

        if (categoryValue.Contains("phụ kiện")) return "accessory";
        if (categoryValue.Contains("linh kiện")) return "component";
        if (categoryValue.Contains("máy tính bàn") || nameValue.Contains("prodesk") || nameValue.Contains("desktop") || nameValue.Contains("optiplex")) return "desktops";

        if (brandValue.Contains("acer")) return "acer";
        if (brandValue.Contains("asus")) return "asus";
        if (brandValue.Contains("dell")) return "dell";
        if (brandValue.Contains("hp")) return "hp";
        if (brandValue.Contains("msi")) return "msi";
        if (brandValue.Contains("apple") || brandValue.Contains("macbook")) return "macbook";
        if (brandValue.Contains("thinkbook") || nameValue.Contains("thinkbook") || nameValue.Contains("thinkpad")) return "thinkbook";
        if (brandValue.Contains("lenovo"))
        {
            if (nameValue.Contains("thinkbook") || nameValue.Contains("thinkpad")) return "thinkbook";
            return "lenovo";
        }
        return brandValue;
    }

    private static readonly Dictionary<string, List<string>> BrandImageCatalog = new(StringComparer.OrdinalIgnoreCase)
    {
        ["acer"] = BuildSequentialImageList("/images/products/acer", "acer", 50),
        ["asus"] = BuildSequentialImageList("/images/products/asus", "asus", 50),
        ["dell"] = BuildSequentialImageList("/images/products/dell", "dell", 39),
        ["hp"] = BuildSequentialImageList("/images/products/hp", "hp", 50),
        ["msi"] = BuildSequentialImageList("/images/products/msi", "msi", 4),
        ["lenovo"] = BuildSequentialImageList("/images/products/lenovo", "lenovo", 34),
        ["thinkbook"] = BuildSequentialImageList("/images/products/thinkbook", "thinkbook", 19),
        ["macbook"] = BuildSequentialImageList("/images/products/macbook", "macbook", 29),
        ["desktops"] = BuildSequentialImageList("/images/products/desktops", "desktops", 9),
        ["accessory"] = BuildImageList("/uploads/products", new[]
        {
            "ban-phim-co-gaming-ek87-ea47cc97.png",
            "chuot-logitech-g102-gen-2-9e309308.jpg",
            "de-tan-nhiet-laptop-x6b-6-fan-48e05f77.jpg",
            "ugreen-usb-c-hub-6-in-1-9-7876c2ce.jpg",
            "s#U1ea1c-zin-laptop-dell-65w-48c68ee2.jpg",
            "tai-nghe-ch#U1ee5p-tai-h3-3103e9fd.jpg"
        }),
        ["component"] = BuildImageList("/uploads/products", new[]
        {
            "ssd-m-2-nvme-512gb-548d4158.jpg",
            "ram-pc-ddr4-8gb-2666mhz-cb3da6c1.jpg",
            "ram-ddr4-laptop-16gb-samsung-3200mhz-3550965f.jpg",
            "pin-laptop-hp-probook-ed2e5c3d.jpg"
        })
    };

    private static List<string> BuildSequentialImageList(string basePath, string prefix, int count, string extension = ".jpg")
        => Enumerable.Range(1, count).Select(i => $"{basePath}/{prefix}-{i:000}{extension}".Replace("//", "/")).ToList();

    private static List<string> BuildImageList(string basePath, IEnumerable<string> fileNames)
        => fileNames.Select(file => $"{basePath}/{file}".Replace("//", "/")).ToList();


    private static string NormalizeTechNewsThumbnailUrl(string? slug, string? url)
    {
        var normalized = NormalizeMediaUrl(url);
        var slugValue = (slug ?? string.Empty).Trim();

        if (slugValue.Equals("top-5-laptop-do-hoa-3d-cao-cap", StringComparison.OrdinalIgnoreCase) &&
            normalized.Equals("/images/banners/campaign-gaming.jpg", StringComparison.OrdinalIgnoreCase))
        {
            return "/images/products/asus/asus-001.jpg";
        }

        return normalized;
    }

    private static string NormalizeLegacyProductImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;

        var trimmed = NormalizeMediaUrl(url);
        if (string.IsNullOrWhiteSpace(trimmed))
            return string.Empty;

        if (!trimmed.StartsWith("/images/products/", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        var relative = trimmed["/images/products/".Length..];
        var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return trimmed;

        var folder = parts[0].Trim().ToLowerInvariant();
        var fileName = parts[^1].Trim();
        if (string.IsNullOrWhiteSpace(fileName))
            return trimmed;

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
        var fileWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(fileWithoutExt))
            return trimmed;

        var expectedPrefix = folder + "-";
        if (fileWithoutExt.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var suffix = fileWithoutExt[expectedPrefix.Length..];
            if (suffix.Length == 3 && suffix.All(char.IsDigit))
                return $"/images/products/{folder}/{folder}-{suffix}{ext}";
            if (int.TryParse(suffix, out var parsedStandard))
                return $"/images/products/{folder}/{folder}-{parsedStandard:000}{ext}";
        }

        var digits = new string(fileWithoutExt.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out var parsedNumber))
        {
            return $"/images/products/{folder}/{folder}-{parsedNumber:000}{ext}";
        }

        return trimmed;
    }

    private static string NormalizeMediaUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        var trimmed = url.Trim().Replace('\\', '/');

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var wwwrootIndex = trimmed.IndexOf("/wwwroot/", StringComparison.OrdinalIgnoreCase);
        if (wwwrootIndex >= 0)
        {
            trimmed = trimmed[(wwwrootIndex + "/wwwroot".Length)..];
        }

        var uploadsIndex = trimmed.IndexOf("/uploads/", StringComparison.OrdinalIgnoreCase);
        if (uploadsIndex >= 0)
        {
            trimmed = trimmed[uploadsIndex..];
        }
        else if (trimmed.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = "/" + trimmed;
        }
        else if (trimmed.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = "/" + trimmed;
        }

        if (LegacyTechNewsImageMap.TryGetValue(trimmed, out var mappedUrl))
        {
            trimmed = mappedUrl;
        }

        return trimmed.StartsWith("/", StringComparison.Ordinal) ? trimmed : "/" + trimmed;
    }

    private static readonly Dictionary<string, string> LegacyTechNewsImageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/images/accessories/accessory-keyboard.svg"] = "/uploads/products/ban-phim-co-gaming-ek87-ea47cc97.png",
        ["/images/accessories/component-ram.svg"] = "/uploads/products/ram-ddr4-laptop-16gb-samsung-3200mhz-3550965f.jpg"
    };



    public async Task<AdminDashboardViewModel> GetAdminDashboardAsync()
    {
        var model = new AdminDashboardViewModel();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using (var cmd = new SqlCommand(@"
            SELECT
                (SELECT COUNT(*) FROM Products),
                (SELECT ISNULL(SUM(StockQty),0) FROM Products),
                (SELECT COUNT(*) FROM Products WHERE StockQty <= 5),
                (SELECT COUNT(*) FROM Orders),
                (SELECT COUNT(*) FROM Orders WHERE IsPaid = 1),
                (SELECT COUNT(*) FROM Orders WHERE IsPaid = 0),
                (SELECT COUNT(*) FROM Orders WHERE OrderStatus IN (N'Đã giao hàng', N'Đã giao hàng và thanh toán')),
                (SELECT COUNT(*) FROM Orders WHERE OrderStatus = N'Đã hủy'),
                (SELECT ISNULL(SUM(TotalAmount),0) FROM Orders WHERE OrderStatus <> N'Đã hủy'),
                (SELECT ISNULL(AVG(CAST(TotalAmount AS DECIMAL(18,2))),0) FROM Orders WHERE OrderStatus <> N'Đã hủy'),
                (SELECT ISNULL(SUM(CAST(Price AS DECIMAL(18,2)) * StockQty),0) FROM Products)", conn))
        {
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                model.TotalProducts = reader.GetInt32(0);
                model.TotalStock = reader.GetInt32(1);
                model.LowStockProducts = reader.GetInt32(2);
                model.TotalOrders = reader.GetInt32(3);
                model.PaidOrders = reader.GetInt32(4);
                model.UnpaidOrders = reader.GetInt32(5);
                model.DeliveredOrders = reader.GetInt32(6);
                model.CancelledOrders = reader.GetInt32(7);
                model.TotalRevenue = reader.GetDecimal(8);
                model.AverageOrderValue = reader.GetDecimal(9);
                model.InventoryValue = reader.GetDecimal(10);
            }
        }

        using (var cmd = new SqlCommand(@"
            SELECT TOP 7 CONVERT(date, CreatedAt) AS DayValue, ISNULL(SUM(TotalAmount),0) AS Revenue
            FROM Orders
            WHERE OrderStatus <> N'Đã hủy'
            GROUP BY CONVERT(date, CreatedAt)
            ORDER BY DayValue DESC", conn))
        {
            using var reader = await cmd.ExecuteReaderAsync();
            var rows = new List<DashboardTrendPoint>();
            while (await reader.ReadAsync())
            {
                var day = reader.GetDateTime(0);
                rows.Add(new DashboardTrendPoint
                {
                    Label = day.ToString("dd/MM"),
                    Value = reader.GetDecimal(1)
                });
            }
            rows.Reverse();
            model.RevenueByDay = rows;
        }

        using (var cmd = new SqlCommand(@"
            SELECT OrderStatus, COUNT(*) AS TotalCount
            FROM Orders
            GROUP BY OrderStatus
            ORDER BY TotalCount DESC", conn))
        {
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var value = reader.GetInt32(1);
                model.OrdersByStatus.Add(new DashboardBreakdownItem
                {
                    Label = reader.IsDBNull(0) ? "Chưa rõ trạng thái" : reader.GetString(0),
                    Value = value,
                    DisplayValue = value.ToString("N0")
                });
            }
        }

        using (var cmd = new SqlCommand(@"
            SELECT TOP 6 ISNULL(NULLIF(LTRIM(RTRIM(CategoryName)),N''),N'Khác') AS CategoryName, ISNULL(SUM(StockQty),0) AS TotalStock
            FROM Products
            GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(CategoryName)),N''),N'Khác')
            ORDER BY TotalStock DESC, CategoryName", conn))
        {
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var value = reader.GetInt32(1);
                model.StockByCategory.Add(new DashboardBreakdownItem
                {
                    Label = reader.GetString(0),
                    Value = value,
                    DisplayValue = value.ToString("N0") + " sp"
                });
            }
        }

        using (var cmd = new SqlCommand(@"
            SELECT TOP 6 ISNULL(NULLIF(LTRIM(RTRIM(Brand)),N''),N'Khác') AS BrandName, COUNT(*) AS TotalProducts
            FROM Products
            GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(Brand)),N''),N'Khác')
            ORDER BY TotalProducts DESC, BrandName", conn))
        {
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var value = reader.GetInt32(1);
                model.ProductsByBrand.Add(new DashboardBreakdownItem
                {
                    Label = reader.GetString(0),
                    Value = value,
                    DisplayValue = value.ToString("N0") + " model"
                });
            }
        }

        using (var cmd = new SqlCommand(@"
            SELECT TOP 8 OrderId, OrderCode, CustomerName, CreatedAt, TotalAmount, OrderStatus
            FROM Orders
            ORDER BY CreatedAt DESC, OrderId DESC", conn))
        {
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                model.RecentOrders.Add(new DashboardRecentOrderItem
                {
                    OrderId = reader.GetInt32(0),
                    OrderCode = reader.GetString(1),
                    CustomerName = reader.GetString(2),
                    CreatedAt = reader.GetDateTime(3),
                    TotalAmount = reader.GetDecimal(4),
                    OrderStatus = reader.GetString(5)
                });
            }
        }

        using (var cmd = new SqlCommand(@"
            SELECT TOP 8 ProductId, ProductName, ISNULL(CategoryName,N'Khác') AS CategoryName, StockQty
            FROM Products
            WHERE StockQty <= 5
            ORDER BY StockQty ASC, ProductId DESC", conn))
        {
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                model.LowStockItems.Add(new DashboardLowStockItem
                {
                    ProductId = reader.GetInt32(0),
                    ProductName = reader.GetString(1),
                    CategoryName = reader.GetString(2),
                    StockQty = reader.GetInt32(3)
                });
            }
        }

        return model;
    }

}

