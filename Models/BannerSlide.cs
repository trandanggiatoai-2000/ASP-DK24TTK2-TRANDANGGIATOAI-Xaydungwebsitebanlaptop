namespace websitebanlaptop.Models;

public class BannerSlide
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public string ButtonText { get; set; } = "Khám phá ngay";
    public string SecondaryButtonText { get; set; } = "Xem chi tiết";
    public string SecondaryButtonLink { get; set; } = "/Product/Catalog";
    public string KickerText { get; set; } = "ƯU ĐÃI NỔI BẬT";
    public string ThemePrimaryColor { get; set; } = "#cf2027";
    public string ThemeAccentColor { get; set; } = "#7c3aed";
    public int TitleFontSize { get; set; } = 54;
    public int SubtitleFontSize { get; set; } = 18;
    public int KickerFontSize { get; set; } = 13;
    public int ButtonFontSize { get; set; } = 17;
    public int ChipFontSize { get; set; } = 13;
    public int PanelOpacityPercent { get; set; } = 88;
    public int PanelWidthPercent { get; set; } = 42;
    public int PanelPadding { get; set; } = 30;
    public int PanelRadius { get; set; } = 28;
    public int DisplayOrder { get; set; }
}

