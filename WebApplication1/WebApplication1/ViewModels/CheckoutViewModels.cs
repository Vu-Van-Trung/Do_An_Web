using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;
using Microsoft.AspNetCore.Identity;


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
    public IEnumerable<Order> RecentOrders { get; set; } = Enumerable.Empty<Order>();
    public List<MonthlyRevenue> MonthlyRevenues { get; set; } = new();
    public int PendingOrderCount { get; set; }
}

public class MonthlyRevenue
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class DiscountFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã giảm giá")]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên chương trình")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public PromotionType PromotionType { get; set; } = PromotionType.Coupon;

    [Required]
    public DiscountType DiscountType { get; set; } = DiscountType.Percent;

    [Range(0, double.MaxValue, ErrorMessage = "Giá trị phải >= 0")]
    public decimal Value { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MaxDiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MinOrderAmount { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxUsage { get; set; }

    [Required]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required]
    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);

    public bool IsActive { get; set; } = true;
    public int? ApplicableCategoryId { get; set; }
}

public class UserListViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsLocked { get; set; }
    public string Role { get; set; } = string.Empty;
    public int OrderCount { get; set; }
}

public class UserDetailViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public string Role { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public IEnumerable<Order> Orders { get; set; } = Enumerable.Empty<Order>();
}

public class ReportViewModel
{
    public List<MonthlyRevenue> MonthlyRevenues { get; set; } = new();
    public IEnumerable<TopProduct> TopProducts { get; set; } = Enumerable.Empty<TopProduct>();
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int NewCustomersThisMonth { get; set; }
}

public class TopProduct
{
    public string Name { get; set; } = string.Empty;
    public int TotalSold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class ApplyDiscountRequest
{
    public string? Code { get; set; }
}

public class RoleManagementViewModel
{
    public Dictionary<string, IList<ApplicationUser>> UsersByRole { get; set; } = new();
    public List<PermissionRow> Permissions { get; set; } = new();
}

public class PermissionRow
{
    public PermissionRow(string name, bool admin, bool manager, bool staff, bool customer)
    {
        Name = name;
        Admin = admin;
        Manager = manager;
        Staff = staff;
        Customer = customer;
    }
    public string Name { get; set; }
    public bool Admin { get; set; }
    public bool Manager { get; set; }
    public bool Staff { get; set; }
    public bool Customer { get; set; }
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
