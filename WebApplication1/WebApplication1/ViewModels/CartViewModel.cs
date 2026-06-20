namespace WebApplication1.ViewModels;

public class CartLineViewModel
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int Stock { get; set; }
    public bool IsBulky { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class CartViewModel
{
    public List<CartLineViewModel> Items { get; set; } = new();
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public int ItemCount => Items.Sum(i => i.Quantity);
}
