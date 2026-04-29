using Microsoft.AspNetCore.Mvc;
using PaymentService.Dto;
using PaymentService.Services;
using System.Text;
using PaymentService.Services.Messaging;
using PaymentService.Data;
using PaymentService.Models;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly RazorpayService _razorpayService;
        private readonly IConfiguration _config;
        private readonly Publisher _publish;
        private readonly PaymentDbContext _context;


        public PaymentController(RazorpayService razorpayService, IConfiguration config, Publisher publish, PaymentDbContext context)
        {
            _razorpayService = razorpayService;
            _config = config;
            _publish = publish;
            _context = context;
        }

        [HttpPost("create-order")]
        public IActionResult CreateOrder(CreatePaymentRequest request)
        {
            var order = _razorpayService.CreateOrder(request.Amount,request.Currency,request.OrderId);

            var paymentRecord = new Payment
            {
                OrderId = request.OrderId.ToString(),
                Amount = request.Amount,
                Currency = request.Currency,
                Status = "Created",
                txnTime = DateTime.UtcNow
            };
            _context.payments.Add(paymentRecord); _context.SaveChangesAsync();

            return Ok(new{
                Id = order["id"].ToString(),
                Amount = Convert.ToInt32(order["amount"]),
                Currency = order["currency"].ToString()
            });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyPayment(VerifyPaymentDto data)
        {
            string key = _config["Razorpay:KeySecret"];

            string orderId = data.razorpay_order_id;
            string paymentId = data.razorpay_payment_id;
            string signature = data.razorpay_signature;

            string generatedSignature = GenerateSignature(orderId, paymentId, key);

            var payment = await _context.payments.FirstOrDefaultAsync(p => p.OrderId == orderId);

            if (payment == null)
                return BadRequest("Payment record not found");

            if (generatedSignature != signature)
            {
                payment.Status = "Declined";
                await _context.SaveChangesAsync();
                await _publish.PaymentResult(payment);
                return BadRequest("Invalid Signature");
            }

            payment.Status = "Paid";
            await _context.SaveChangesAsync();

            await _publish.PaymentResult(payment);

            return Ok("Payment Verified");
        }

        [HttpPost("test-signature")]
        public IActionResult TestSignature(VerifyPaymentDto data)
        {
            var key = _config["Razorpay:KeySecret"];

            var sig = GenerateSignature(data.razorpay_order_id,data.razorpay_payment_id,key);

            return Ok(sig);
        }

        private string GenerateSignature(string orderId, string paymentId, string secret)
        {
            var payload = $"{orderId}|{paymentId}";

            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }


    }
}
