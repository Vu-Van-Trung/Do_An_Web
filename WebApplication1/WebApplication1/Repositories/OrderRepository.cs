using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Repositories;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetWithItemsAsync(int id) =>
        await Context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IEnumerable<Order>> GetByUserAsync(string userId) =>
        await Context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

    public async Task<IEnumerable<Order>> GetAllWithItemsAsync() =>
        await Context.Orders
            .Include(o => o.Items)
            .Include(o => o.User)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

    public async Task<decimal> GetTotalRevenueAsync() =>
        await Context.Orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SumAsync(o => o.Total);

    public async Task<int> GetOrderCountAsync() => await Context.Orders.CountAsync();

    public async Task<bool> UserHasOrdersAsync(string userId) =>
        await Context.Orders.AnyAsync(o => o.UserId == userId);
}
