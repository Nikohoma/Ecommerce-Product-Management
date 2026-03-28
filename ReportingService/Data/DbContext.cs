using Microsoft.EntityFrameworkCore;
using ReportingService.Models;

public class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options)
        : base(options) { }

    public DbSet<ProductReport> ProductReports { get; set; }
}