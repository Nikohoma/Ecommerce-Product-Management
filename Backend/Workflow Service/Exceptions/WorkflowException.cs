namespace WorkflowService.Exceptions
{
    public class WorkflowException : Exception
    {
        public WorkflowException(string message) : base(message) { }
        public WorkflowException(string message, Exception inner) : base(message, inner) { }
    }

    public class PublisherConnectionException : WorkflowException
    {
        public PublisherConnectionException(Exception inner)
            : base("Failed to establish RabbitMQ connection for publishing.", inner) { }
    }

    public class QueueDeclarationException : WorkflowException
    {
        public QueueDeclarationException(string queueName, Exception inner)
            : base($"Failed to declare queue '{queueName}' before publishing.", inner) { }
    }

    public class MessagePublishException : WorkflowException
    {
        public MessagePublishException(string queueName, Exception inner)
            : base($"Failed to publish message to queue '{queueName}'.", inner) { }
    }

    public class MessageSerializationException : WorkflowException
    {
        public MessageSerializationException(Exception inner)
            : base("Failed to serialize message before publishing.", inner) { }
    }
    public class WorkflowActionException : WorkflowException
    {
        public WorkflowActionException(string action, int productId, Exception inner)
            : base($"Failed to execute workflow action '{action}' for ProductId {productId}.", inner) { }
    }

    public class WorkflowLogException : WorkflowException
    {
        public WorkflowLogException(int productId, Exception inner)
            : base($"Failed to persist workflow log entry for ProductId {productId}.", inner) { }
    }

    public class WorkflowPublishException : WorkflowException
    {
        public WorkflowPublishException(string action, int productId, Exception inner)
            : base($"Failed to publish '{action}' event for ProductId {productId}.", inner) { }
    }
}