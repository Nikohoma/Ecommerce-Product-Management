namespace ReportingService.Models
{
    public class ProductActivity
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        // "PriceUpdated" | "StockUpdated" | "MediaUploaded"
        public string ActivityType { get; set; }

        public DateTime UpdatedAt { get; set; }

        public decimal? OldPrice { get; set; }
        public decimal? NewPrice { get; set; }

        public int? OldStock { get; set; }
        public int? NewStock { get; set; }

        public string? MediaUrl { get; set; }
    }
}

