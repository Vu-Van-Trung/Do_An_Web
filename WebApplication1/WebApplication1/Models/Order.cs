namespace WebApplication1.Models;

public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string PaymentMethod { get; set; } = "COD";
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.COD;
    public string? VnPayTransactionId { get; set; }

    public string? DiscountCode { get; set; }
    public decimal DiscountAmount { get; set; }

    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
