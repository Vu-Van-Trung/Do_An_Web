using WebApplication1.Models;

namespace WebApplication1.Repositories;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IEnumerable<Category>> GetRootCategoriesAsync();
    Task<Category?> GetBySlugAsync(string slug);
}
