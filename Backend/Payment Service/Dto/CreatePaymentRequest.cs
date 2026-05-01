namespace PaymentService.Dto
{
    public class CreatePaymentRequest
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
    }
}
