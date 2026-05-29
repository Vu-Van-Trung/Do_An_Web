using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Repositories;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context) : base(context)
    {
    }
}
