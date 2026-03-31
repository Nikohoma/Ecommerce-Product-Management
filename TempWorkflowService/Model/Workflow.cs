namespace WorkflowService.Model
{
    public class Workflow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int productId { get; set; }
        public string oldStatus { get; set; }
        public string newStatus { get; set; }
        public DateTime Date { get; set; }
    }
}
