namespace WebApplication1.ViewModels;

public class ProductFilter
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Connection { get; set; }
    public string? SwitchType { get; set; }
    public string? Dpi { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
