using Microsoft.AspNetCore.Http;

namespace websitebanlaptop.Models;

public class WebsiteSettingsViewModel
{
    public string PromoText { get; set; } = "Miễn phí giao hàng nội thành cho đơn từ 15 triệu • Hotline: 1900 1234";
    public string StoreName { get; set; } = "Laptop Store Premium";
    public string StoreTagline { get; set; } = "Laptop • Gaming • Văn phòng • Doanh nhân";
    public string LogoText { get; set; } = "LS";
    public string WebsiteLogoUrl { get; set; } = "";
    public string AdminLogoUrl { get; set; } = "";
    public string FaviconUrl { get; set; } = "";
    public string WebsiteLogoCropMode { get; set; } = "square";
    public string AdminLogoCropMode { get; set; } = "square";
    public string HeaderButtonText { get; set; } = "Xem sản phẩm";
    public string HeaderButtonLink { get; set; } = "/Product/Catalog";
    public string HeroTitle { get; set; } = "Giao diện bán laptop hiện đại, chuyên nghiệp và dễ quản trị";
    public string HeroSubtitle { get; set; } = "Tối ưu trải nghiệm mua hàng với banner đẹp, danh mục rõ ràng và quản trị nội dung trực tiếp từ admin.";
    public string HeroButtonText { get; set; } = "Khám phá ngay";
    public string HeroButtonLink { get; set; } = "/Product/Catalog";
    public string FeaturedBrandsTitle { get; set; } = "Thương hiệu nổi bật";
    public string DemandSectionTitle { get; set; } = "Nhu cầu sử dụng";
    public string FeaturedProductsTitle { get; set; } = "Sản phẩm nổi bật";
    public string FlashTitle { get; set; } = "Laptop giá chạm đáy. Đừng bỏ lỡ!";
    public string FlashBadgeText { get; set; } = "FLASH SALE";
    public string FlashCountdownLabel { get; set; } = "Kết thúc trong:";
    public string FlashCountdownEndsAt { get; set; } = "2030-12-31T23:59:59";
    public string FlashImageUrl { get; set; } = "/images/banners/slide1.svg";
    public string PrimaryColor { get; set; } = "#cf2027";
    public string SecondaryColor { get; set; } = "#1d4ed8";
    public string AccentColor { get; set; } = "#7c3aed";

    public string HeaderBackgroundColor { get; set; } = "#ffffff";
    public string HeaderTextColor { get; set; } = "#0f172a";
    public int StoreNameFontSize { get; set; } = 18;
    public int StoreTaglineFontSize { get; set; } = 14;
    public int PromoTextFontSize { get; set; } = 14;

    public string HeroBackgroundColor { get; set; } = "#ffffff";
    public string HeroTextColor { get; set; } = "#0f172a";
    public int HeroTitleFontSize { get; set; } = 42;
    public int HeroSubtitleFontSize { get; set; } = 18;
    public int HeroButtonFontSize { get; set; } = 18;

    public string PopupBackgroundColor { get; set; } = "#ffffff";
    public string PopupTextColor { get; set; } = "#0f172a";
    public int PopupTitleFontSize { get; set; } = 38;
    public int PopupSubtitleFontSize { get; set; } = 16;
    public int PopupButtonFontSize { get; set; } = 17;

    public string SectionTitleColor { get; set; } = "#0f172a";
    public string SectionSubtextColor { get; set; } = "#475569";
    public int SectionTitleFontSize { get; set; } = 22;
    public string FooterAbout { get; set; } = "Website chuyên đề xây dựng website bán laptop với giỏ hàng, thanh toán giả lập và quản lý đơn hàng.";
    public string FooterSupportTitle { get; set; } = "Hỗ trợ";
    public string FooterSupportLine1 { get; set; } = "Giao hàng toàn quốc";
    public string FooterSupportLine2 { get; set; } = "Đổi trả dễ dàng";
    public string FooterSupportLine3 { get; set; } = "Bảo hành chính hãng";
    public string FooterBankTitle { get; set; } = "Thông tin chuyển khoản";
    public string FooterBankLine1 { get; set; } = "VCB - Vietcombank";
    public string FooterBankLine2 { get; set; } = "STK: 190012340001";
    public string FooterBankLine3 { get; set; } = "Chủ TK: LAPTOP STORE PREMIUM";

    public string TopPhone1 { get; set; } = "0903 344 188";
    public string TopPhone2 { get; set; } = "0909 344 188";
    public string HeaderAddress { get; set; } = "617 Đường 3 Tháng 2, P.8, Quận 10, HCM";
    public string FooterCompanyHeading { get; set; } = "CỬA HÀNG";
    public string FooterShowroomTitle { get; set; } = "Showroom bán hàng";
    public string FooterShowroomAddress { get; set; } = "Địa chỉ: 617 Đường 3 tháng 2, Phường 8, Quận 10, TP. Hồ Chí Minh";
    public string FooterShowroomHotline { get; set; } = "Hotline: 0903 344 188 - 0909 344 188";
    public string FooterWarrantyTitle { get; set; } = "Trung tâm bảo hành";
    public string FooterWarrantyAddress { get; set; } = "Địa chỉ: 530 Đường 3 tháng 2, Phường 14, Quận 10, TP. Hồ Chí Minh";
    public string FooterWarrantyHotline { get; set; } = "Hỗ trợ kỹ thuật: 0909 054 758";
    public string FooterWorkingHoursTitle { get; set; } = "Thời gian làm việc";
    public string FooterWorkingHoursLine1 { get; set; } = "Showroom: Thứ 2 – Chủ Nhật (8:00 – 21:00)";
    public string FooterWorkingHoursLine2 { get; set; } = "TT Bảo hành: Thứ 2 – Thứ 7 (8:00 – 17:00)";
    public string FooterShippingTitle { get; set; } = "DỊCH VỤ GIAO HÀNG";
    public string FooterShippingLine1 { get; set; } = "VIETNAM POST";
    public string FooterShippingLine2 { get; set; } = "GHN";
    public string FooterShippingLine3 { get; set; } = "Viettel Post";
    public string FooterShippingLine4 { get; set; } = "AhaMove";
    public string FooterQrLabel { get; set; } = "QR THANH TOÁN";
    public string FooterQrImageUrl { get; set; } = "";
    public string FooterCopyright { get; set; } = "© Copyright Laptop Store - Chuyên Đề Xây Dựng Website Bán Laptop.";

    public string FooterInfoLink1Text { get; set; } = "Đánh giá Laptop Hp Probook 650 G4";
    public string FooterInfoLink1Url { get; set; } = "/TechNews/Details?slug=danh-gia-hp-probook-650-g4-van-phong-man-hinh-to";
    public string FooterInfoLink2Text { get; set; } = "Laptop xách tay là gì?";
    public string FooterInfoLink2Url { get; set; } = "/TechNews/Details?slug=laptop-xach-tay-la-gi-nen-mua-khong";
    public string FooterInfoLink3Text { get; set; } = "Mẹo dùng laptop bền và mượt";
    public string FooterInfoLink3Url { get; set; } = "/TechNews/Details?slug=thu-thuat-meo-su-dung-laptop-ben-va-muot";
    public string FooterInfoLink4Text { get; set; } = "Top game phù hợp laptop gaming";
    public string FooterInfoLink4Url { get; set; } = "/TechNews/Details?slug=top-game-phu-hop-laptop-gaming";
    public string FooterInfoLink5Text { get; set; } = "Hỏi đáp khi mua laptop xách tay";
    public string FooterInfoLink5Url { get; set; } = "/TechNews/Details?slug=hoi-dap-khi-mua-laptop-xach-tay";
    public string FooterInfoLink6Text { get; set; } = "Chính sách bảo hành và hỗ trợ";
    public string FooterInfoLink6Url { get; set; } = "/support/warranty";

    public string CategoryBannerGamingUrl { get; set; } = "/images/banners/campaign-gaming.jpg";
    public string CategoryBannerOfficeUrl { get; set; } = "/images/banners/campaign-office.jpg";
    public string CategoryBannerPremiumUrl { get; set; } = "/images/banners/campaign-weekend.jpg";
    public string CategoryBannerGraphicsUrl { get; set; } = "/images/banners/campaign-macbook.jpg";
    public string CategoryBannerAccessoryUrl { get; set; } = "/images/accessories/accessory-keyboard.svg";
    public string CategoryBannerComponentUrl { get; set; } = "/images/accessories/component-ram.svg";

    public string FooterShippingImage1Url { get; set; } = "";
    public string FooterShippingImage2Url { get; set; } = "";
    public string FooterShippingImage3Url { get; set; } = "";
    public string FooterShippingImage4Url { get; set; } = "";

    public string PopupTitle { get; set; } = "Siêu ưu đãi cuối tuần";
    public string PopupSubtitle { get; set; } = "Giảm sâu cho laptop văn phòng, gaming và phụ kiện khi đặt online hôm nay.";
    public string PopupButtonText { get; set; } = "Xem ưu đãi";
    public string PopupButtonLink { get; set; } = "/Product/Catalog";
    public string PopupImageUrl { get; set; } = "/images/banners/campaign-weekend.jpg";
    public bool PopupEnabled { get; set; } = true;
    public bool ShowHeroSection { get; set; } = true;
    public bool ShowMegaMenu { get; set; } = true;
    public bool ShowFlashBanner { get; set; } = true;

    public IFormFile? WebsiteLogoFile { get; set; }
    public IFormFile? AdminLogoFile { get; set; }
    public IFormFile? FaviconFile { get; set; }
    public IFormFile? PopupImageFile { get; set; }
    public IFormFile? FlashImageFile { get; set; }

    public IFormFile? CategoryBannerGamingFile { get; set; }
    public IFormFile? CategoryBannerOfficeFile { get; set; }
    public IFormFile? CategoryBannerPremiumFile { get; set; }
    public IFormFile? CategoryBannerGraphicsFile { get; set; }
    public IFormFile? CategoryBannerAccessoryFile { get; set; }
    public IFormFile? CategoryBannerComponentFile { get; set; }

    public IFormFile? FooterShippingImage1File { get; set; }
    public IFormFile? FooterShippingImage2File { get; set; }
    public IFormFile? FooterShippingImage3File { get; set; }
    public IFormFile? FooterShippingImage4File { get; set; }
    public IFormFile? FooterQrImageFile { get; set; }
}

