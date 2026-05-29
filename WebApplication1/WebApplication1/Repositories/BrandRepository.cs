using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Repositories;

public class BrandRepository : Repository<Brand>, IBrandRepository
{
    public BrandRepository(ApplicationDbContext context) : base(context)
    {
    }
}
