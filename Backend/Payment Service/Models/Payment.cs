namespace PaymentService.Models
{
    public class Payment
    {
        public string OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } // Created, Paid, Failed
        public DateTime txnTime { get; set; }
        public string? RazorpayPaymentId { get; set; }
        public string? Currency { get; set; }
    }
}
