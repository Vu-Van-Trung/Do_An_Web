using WebApplication1.ViewModels;

namespace WebApplication1.Services;

public interface ICartService
{
    Task<CartViewModel> GetCartAsync();
    Task<CartViewModel> GetSelectedCartAsync(IEnumerable<int> cartItemIds);
    Task<int> GetItemCountAsync();
    Task AddToCartAsync(int productId, int quantity = 1);
    Task UpdateQuantityAsync(int cartItemId, int quantity);
    Task RemoveItemAsync(int cartItemId);
    Task MergeSessionCartAsync(string userId);
    Task ClearCartAsync();
}
