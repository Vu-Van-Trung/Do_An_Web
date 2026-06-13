using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Repositories;

public class DiscountRepository : Repository<Discount>, IDiscountRepository
{
    public DiscountRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Discount?> GetByCodeAsync(string code) =>
        await Context.Discounts
            .FirstOrDefaultAsync(d => d.Code.ToUpper() == code.ToUpper());

    public async Task<Discount?> GetActiveByCodeAsync(string code, decimal orderAmount)
    {
        var now = DateTime.UtcNow;
        return await Context.Discounts.FirstOrDefaultAsync(d =>
            d.Code.ToUpper() == code.ToUpper() &&
            d.IsActive &&
            d.StartDate <= now &&
            d.EndDate >= now &&
            d.MinOrderAmount <= orderAmount &&
            (d.MaxUsage == null || d.UsedCount < d.MaxUsage));
    }
}
