namespace Shared.Contracts
{
    public class ProductWorkflowEvent
    {
        public int ProductId { get; set; }
        public string Action { get; set; } // Submit / Approve / Reject
    }
}
