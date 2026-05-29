using WebApplication1.Models;

namespace WebApplication1.ViewModels;

public class CatalogViewModel
{
    public PagedResult<Product> Products { get; set; } = new();
    public ProductFilter Filter { get; set; } = new();
    public IEnumerable<Category> Categories { get; set; } = Enumerable.Empty<Category>();
    public IEnumerable<Brand> Brands { get; set; } = Enumerable.Empty<Brand>();
    public IEnumerable<string> Connections { get; set; } = Enumerable.Empty<string>();
    public IEnumerable<string> SwitchTypes { get; set; } = Enumerable.Empty<string>();
    public IEnumerable<string> DpiOptions { get; set; } = Enumerable.Empty<string>();
}
