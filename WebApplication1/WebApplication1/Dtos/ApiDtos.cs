using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Dtos;

public record PagedResult<T>(
    IEnumerable<T> Data,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

// ── Products ──────────────────────────────────────────────────────────────────
public record ProductListDto(
    int Id, string Name, decimal Price, int Stock, bool IsActive,
    string? ImageUrl, string Category, string Brand,
    double? AverageRating, int ReviewCount);

public record ProductDetailDto(
    int Id, string Name, string? Description, decimal Price, int Stock, bool IsActive,
    string? ImageUrl, List<string> SecondaryImages,
    string Category, int CategoryId, string Brand, int BrandId,
    string ShippingClass, double? AverageRating, int ReviewCount,
    List<SpecDto> Specs, DateTime CreatedAt);

public record SpecDto(string Key, string Value);

// ── Reviews ───────────────────────────────────────────────────────────────────
public record ReviewDto(int Id, string UserName, int Rating, string? Comment, DateTime CreatedAt);
public record AddReviewRequest([Range(1, 5)] int Rating, string? Comment);

// ── Categories ────────────────────────────────────────────────────────────────
public record CategoryDto(
    int Id, string Name, string? Description, string? Slug, string? Icon,
    int? ParentCategoryId, List<CategoryDto>? SubCategories = null);

// ── Brands ────────────────────────────────────────────────────────────────────
public record BrandDto(int Id, string Name, string? LogoUrl, int ProductCount);

// ── Cart ──────────────────────────────────────────────────────────────────────
public record CartResponseDto(List<CartItemDto> Items, decimal Subtotal, int ItemCount);
public record CartItemDto(
    int CartItemId, int ProductId, string Name, string? ImageUrl,
    decimal UnitPrice, int Quantity, int Stock, decimal LineTotal, string ShippingClass);
public record AddToCartRequest(int ProductId, int Quantity = 1);
public record UpdateCartItemRequest([Range(0, 9999)] int Quantity);

// ── Orders ────────────────────────────────────────────────────────────────────
public record OrderListDto(
    int Id, DateTime OrderDate, string Status, string PaymentMethod, string PaymentStatus,
    decimal Subtotal, decimal ShippingFee, decimal DiscountAmount, decimal Total, int ItemCount);

public record OrderDetailDto(
    int Id, DateTime OrderDate, string Status, string PaymentMethod, string PaymentStatus,
    string FullName, string Phone, string ShippingAddress, string? Notes,
    decimal Subtotal, decimal ShippingFee, decimal DiscountAmount, string? DiscountCode,
    decimal Total, List<OrderItemDto> Items);

public record OrderItemDto(
    int ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

public record PlaceOrderApiRequest(
    [Required] string FullName,
    [Required, Phone] string Phone,
    [Required] string ShippingAddress,
    string? Notes,
    int ProvinceCode,
    string ShippingService,
    [Required] string PaymentMethod,
    string? DiscountCode = null,
    int[]? SelectedCartItemIds = null);

// ── Auth ──────────────────────────────────────────────────────────────────────
public record LoginRequest(
    [Required] string Email,
    [Required] string Password,
    bool RememberMe = false);

public record RegisterRequest(
    [Required] string FullName,
    [Required, EmailAddress] string Email,
    [Required, Phone] string PhoneNumber,
    [Required, MinLength(6)] string Password);

public record AuthUserDto(
    string Id, string FullName, string Email, string? PhoneNumber,
    string? Address, IList<string> Roles);

// ── Wishlist ──────────────────────────────────────────────────────────────────
public record WishlistItemDto(
    int ProductId, string Name, decimal Price, string? ImageUrl, bool InStock, DateTime AddedAt);

// ── Shipping ──────────────────────────────────────────────────────────────────
public record ShippingOptionDto(
    string Service, string ServiceName, decimal Fee, string Eta, bool IsContactOnly);

// ── Discounts ─────────────────────────────────────────────────────────────────
public record ValidateDiscountRequest([Required] string Code, decimal OrderSubtotal);
public record DiscountResultDto(
    bool Success, string? Message, decimal DiscountAmount,
    bool IsFreeShipping, string? Code, string? Name);
