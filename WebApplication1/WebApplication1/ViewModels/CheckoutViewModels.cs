using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;

namespace WebApplication1.ViewModels;

public class ShippingViewModel
{
    [Required, Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required, Phone]
    public string Phone { get; set; } = string.Empty;

    [Required, Display(Name = "Shipping Address")]
    public string ShippingAddress { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

public class PaymentViewModel
{
    public string PaymentMethod { get; set; } = "COD";
    public string? CardNumber { get; set; }
    public string? CardName { get; set; }
}

public class CheckoutViewModel
{
    public ShippingViewModel Shipping { get; set; } = new();
    public PaymentViewModel Payment { get; set; } = new();
    public CartViewModel Cart { get; set; } = new();
    public int Step { get; set; } = 1;
}

public class OrderConfirmationViewModel
{
    public Order Order { get; set; } = null!;
}

public class AdminDashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public int OrderCount { get; set; }
    public int CustomerCount { get; set; }
    public int ProductCount { get; set; }
    public IEnumerable<Product> LowStockProducts { get; set; } = Enumerable.Empty<Product>();
}

public class ProductFormViewModel
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required, Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int Stock { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int BrandId { get; set; }

    public string? ImageUrl { get; set; }
    public IFormFile? ImageFile { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SpecInput> Specifications { get; set; } = new();
}

public class SpecInput
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
