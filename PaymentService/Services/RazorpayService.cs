using Razorpay.Api;
using System.Text;


namespace PaymentService.Services
{
    public class RazorpaySettings
    {
        public string KeyId { get; set; }
        public string KeySecret { get; set; }
    }
    public class RazorpayService
    {
        private readonly IConfiguration _config;

        public RazorpayService(IConfiguration config)
        {
            _config = config;
        }

        public Order CreateOrder(decimal amount, string currency, int receiptId)
        {
            var key = _config["Razorpay:KeyId"];
            var secret = _config["Razorpay:KeySecret"];

            RazorpayClient client = new RazorpayClient(key, secret);

            Dictionary<string, object> options = new Dictionary<string, object>();
            options.Add("amount", (int)(amount * 100));
            options.Add("currency", currency);
            options.Add("receipt", $"order_rcptid_{receiptId}");

            Order order = client.Order.Create(options);

            return order;
        }
    }
}
