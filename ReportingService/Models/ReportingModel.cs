namespace ReportingService.Models
{
    public class ProductReport
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Status { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal Price { get; set; } = 0;
    }
}
