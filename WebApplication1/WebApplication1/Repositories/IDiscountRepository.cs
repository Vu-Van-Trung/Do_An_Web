using WebApplication1.Models;

namespace WebApplication1.Repositories;

public interface IDiscountRepository : IRepository<Discount>
{
    Task<Discount?> GetByCodeAsync(string code);
    Task<Discount?> GetActiveByCodeAsync(string code, decimal orderAmount);
}
