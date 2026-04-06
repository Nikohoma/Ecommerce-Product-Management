namespace ReportingService.Exceptions
{
    public class ReportingException : Exception
    {
        public ReportingException(string message) : base(message) { }
        public ReportingException(string message, Exception inner) : base(message, inner) { }
    }

    public class ReportQueryException : ReportingException
    {
        public ReportQueryException(string operation, Exception inner)
            : base($"Database error during report query '{operation}'.", inner) { }
    }

    public class ReportNotFoundException : ReportingException
    {
        public ReportNotFoundException(int productId)
            : base($"No reports found for product ID {productId}.") { }
    }

    public class EmptyReportSetException : ReportingException
    {
        public EmptyReportSetException(string operation)
            : base($"Cannot compute '{operation}' — no product reports exist.") { }
    }
    public class DashboardAggregationException : ReportingException
    {
        public DashboardAggregationException(Exception inner)
            : base("Failed to aggregate dashboard data.", inner) { }
    }

    public class ApprovalRateException : ReportingException
    {
        public ApprovalRateException(Exception inner)
            : base("Failed to compute approval rate.", inner) { }
    }

    public class RecentReportsException : ReportingException
    {
        public RecentReportsException(Exception inner)
            : base("Failed to retrieve recent reports.", inner) { }
    }
    public class RabbitMqConnectionException : ReportingException
    {
        public RabbitMqConnectionException(Exception inner)
            : base("Failed to establish RabbitMQ connection or channel.", inner) { }
    }

    public class QueueDeclarationException : ReportingException
    {
        public QueueDeclarationException(string queueName, Exception inner)
            : base($"Failed to declare queue '{queueName}'.", inner) { }
    }

    public class MessageDeserializationException : ReportingException
    {
        public MessageDeserializationException(Exception inner)
            : base("Failed to deserialize incoming queue message.", inner) { }
    }

    public class ReportPersistenceException : ReportingException
    {
        public ReportPersistenceException(int productId, Exception inner)
            : base($"Failed to persist report for ProductId {productId}.", inner) { }
    }
}