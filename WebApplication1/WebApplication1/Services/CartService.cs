using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Services;

public class CartService : ICartService
{
    private const string SessionCartKey = "CartSessionId";
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext Http => _httpContextAccessor.HttpContext!;
    private string? UserId => Http.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    private string SessionId
    {
        get
        {
            var session = Http.Session;
            var id = session.GetString(SessionCartKey);
            if (!string.IsNullOrEmpty(id))
                return id;
            id = Guid.NewGuid().ToString();
            session.SetString(SessionCartKey, id);
            return id;
        }
    }

    private IQueryable<CartItem> CartQuery()
    {
        if (UserId != null)
            return _context.CartItems.Where(c => c.UserId == UserId);
        return _context.CartItems.Where(c => c.SessionId == SessionId);
    }

    public async Task<CartViewModel> GetCartAsync()
    {
        var items = await CartQuery()
            .Include(c => c.Product)
            .ThenInclude(p => p.Brand)
            .Include(c => c.Product.Category)
            .ToListAsync();

        return new CartViewModel
        {
            Items = items.Select(c => new CartLineViewModel
            {
                CartItemId = c.Id,
                ProductId = c.ProductId,
                Name = c.Product.Name,
                ImageUrl = c.Product.ImageUrl,
                UnitPrice = c.Product.Price,
                Quantity = c.Quantity,
                Stock = c.Product.Stock,
                ShippingClass = c.Product.ShippingClass
            }).ToList()
        };
    }

    public async Task<CartViewModel> GetSelectedCartAsync(IEnumerable<int> cartItemIds)
    {
        var idSet = cartItemIds.ToHashSet();
        var items = await CartQuery()
            .Include(c => c.Product).ThenInclude(p => p.Brand)
            .Include(c => c.Product.Category)
            .Where(c => idSet.Contains(c.Id))
            .ToListAsync();

        return new CartViewModel
        {
            Items = items.Select(c => new CartLineViewModel
            {
                CartItemId = c.Id,
                ProductId  = c.ProductId,
                Name       = c.Product.Name,
                ImageUrl   = c.Product.ImageUrl,
                UnitPrice  = c.Product.Price,
                Quantity   = c.Quantity,
                Stock      = c.Product.Stock,
                ShippingClass = c.Product.ShippingClass
            }).ToList()
        };
    }

    public async Task<int> GetItemCountAsync() =>
        await CartQuery().SumAsync(c => c.Quantity);

    public async Task AddToCartAsync(int productId, int quantity = 1)
    {
        var product = await _context.Products.FindAsync(productId)
            ?? throw new InvalidOperationException("Product not found.");
        if (product.Stock < quantity)
            throw new InvalidOperationException("Insufficient stock.");

        var existing = await CartQuery().FirstOrDefaultAsync(c => c.ProductId == productId);
        if (existing != null)
        {
            existing.Quantity = Math.Min(existing.Quantity + quantity, product.Stock);
        }
        else
        {
            _context.CartItems.Add(new CartItem
            {
                ProductId = productId,
                Quantity = quantity,
                UserId = UserId,
                SessionId = UserId == null ? SessionId : null
            });
        }
        await _context.SaveChangesAsync();
    }

    public async Task UpdateQuantityAsync(int cartItemId, int quantity)
    {
        var item = await CartQuery().Include(c => c.Product).FirstOrDefaultAsync(c => c.Id == cartItemId);
        if (item == null) return;
        if (quantity <= 0)
        {
            _context.CartItems.Remove(item);
        }
        else
        {
            item.Quantity = Math.Min(quantity, item.Product.Stock);
        }
        await _context.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(int cartItemId)
    {
        var item = await CartQuery().FirstOrDefaultAsync(c => c.Id == cartItemId);
        if (item == null) return;
        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();
    }

    public async Task MergeSessionCartAsync(string userId)
    {
        var sessionId = Http.Session.GetString(SessionCartKey);
        if (string.IsNullOrEmpty(sessionId)) return;

        var sessionItems = await _context.CartItems
            .Where(c => c.SessionId == sessionId)
            .ToListAsync();

        foreach (var sessionItem in sessionItems)
        {
            var userItem = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == sessionItem.ProductId);

            if (userItem != null)
            {
                userItem.Quantity = Math.Min(
                    userItem.Quantity + sessionItem.Quantity,
                    userItem.Product.Stock);
                _context.CartItems.Remove(sessionItem);
            }
            else
            {
                sessionItem.UserId = userId;
                sessionItem.SessionId = null;
            }
        }

        Http.Session.Remove(SessionCartKey);
        await _context.SaveChangesAsync();
    }

    public async Task ClearCartAsync()
    {
        var items = await CartQuery().ToListAsync();
        _context.CartItems.RemoveRange(items);
        await _context.SaveChangesAsync();
    }
}
