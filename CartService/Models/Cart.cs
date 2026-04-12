namespace CartService.Models
{
    public enum CartStatus
    {
        Active,        // user is adding/removing items
        CheckedOut,    // user placed order — cart is locked
        Abandoned,     // user inactive for X hours/days
        Expired        // system cleaned up after abandonment
    }
    public class Cart
    {
        public int Id { get; set; }
        public string UserId { get; set; }

        public CartStatus Status { get; set; } = CartStatus.Active;  

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;   
        public DateTime? LastActivityAt { get; set; }                
        public DateTime? CheckedOutAt { get; set; }                   

        public List<CartItem> Items { get; set; } = new();

        // computed — never stored in DB
        public decimal Total => Items?.Sum(i => i.Quantity * i.PriceSnapshot) ?? 0;
    }
}