namespace websitebanlaptop.Models;

public class CatalogQueryModel
{
    public string? Keyword { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Cpu { get; set; }
    public string? Ram { get; set; }
    public string? Ssd { get; set; }
    public bool Official { get; set; }
    public bool Fast { get; set; }
    public bool Installment { get; set; }
    public int Page { get; set; } = 1;
    public bool Ajax { get; set; }
}
