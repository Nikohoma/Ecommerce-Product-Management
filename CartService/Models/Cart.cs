namespace CartService.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public string UserId { get; set; }

        public List<CartItem> Items { get; set; }  // related to cartItem, need to use Include in linq
        public decimal Total => Items?.Sum(i => i.Quantity * i.PriceSnapshot) ?? 0;
    }
}