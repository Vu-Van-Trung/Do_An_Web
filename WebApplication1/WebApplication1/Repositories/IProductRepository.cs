using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetWithDetailsAsync(int id);
    Task<PagedResult<Product>> GetFilteredAsync(ProductFilter filter);
    Task<IEnumerable<string>> GetSpecValuesAsync(string key);
}
