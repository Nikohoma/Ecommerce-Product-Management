namespace ReportingService.DTO
{
    public class DashboardDto
    {
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Pending { get; set; }
        public decimal TotalInventoryValue { get; set; }
    }
}
