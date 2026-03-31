using Microsoft.EntityFrameworkCore;
using WorkflowService.Model;

namespace WorkflowService.Data
{
    public class WorkflowDbContext : DbContext
    {
        public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options) { }

        public DbSet<Workflow> WorkflowDb { get; set; }
    }
}
