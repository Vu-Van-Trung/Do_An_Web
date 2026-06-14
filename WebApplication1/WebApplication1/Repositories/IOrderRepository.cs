using WebApplication1.Models;

namespace WebApplication1.Repositories;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetWithItemsAsync(int id);
    Task<IEnumerable<Order>> GetByUserAsync(string userId);
    Task<IEnumerable<Order>> GetAllWithItemsAsync();
    Task<decimal> GetTotalRevenueAsync();
    Task<int> GetOrderCountAsync();
    Task<bool> UserHasOrdersAsync(string userId);
}
