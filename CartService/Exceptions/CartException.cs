// Exceptions/CartException.cs
namespace CartService.Exceptions
{
    public class CartException : Exception
    {
        public CartException(string message) : base(message) { }
        public CartException(string message, Exception inner) : base(message, inner) { }
    }

    public class CartNotFoundException : CartException
    {
        public CartNotFoundException(string userId)
            : base($"Cart for user {userId} was not found.") { }
    }

    public class CartItemNotFoundException : CartException
    {
        public CartItemNotFoundException(int itemId)
            : base($"Cart item with ID {itemId} was not found.") { }
    }

    public class InvalidQuantityException : CartException
    {
        public InvalidQuantityException(int quantity)
            : base($"Quantity must be greater than zero. Received: {quantity}.") { }
    }
}