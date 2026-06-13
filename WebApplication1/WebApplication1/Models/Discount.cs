namespace WebApplication1.Models;

public enum DiscountType { Percent, Fixed }

public class Discount
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; } = DiscountType.Percent;
    public decimal Value { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int? MaxUsage { get; set; }
    public int UsedCount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
