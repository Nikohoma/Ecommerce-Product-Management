using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WorkflowService.Exceptions;

namespace WorkflowService.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Workflow Exception: {Message}", exception.Message);

            var (statusCode, title, errorCode) = exception switch
            {
                PublisherConnectionException => (StatusCodes.Status503ServiceUnavailable, "Service Communication Failure", "COMMUNICATION_ERROR"),
                QueueDeclarationException => (StatusCodes.Status503ServiceUnavailable, "Service Configuration Failure", "CONFIG_ERROR"),
                WorkflowPublishException => (StatusCodes.Status503ServiceUnavailable, "Event Dispatch Failure", "DISPATCH_ERROR"),
                
                MessagePublishException or 
                MessageSerializationException or 
                WorkflowLogException => (StatusCodes.Status500InternalServerError, "Service Processing Failure", "PROCESS_ERROR"),
                
                WorkflowActionException => (StatusCodes.Status400BadRequest, "Invalid Workflow State", "WORKFLOW_ERROR"),
                WorkflowException => (StatusCodes.Status400BadRequest, "Workflow Operation Failed", "BAD_REQUEST"),

                ArgumentException => (StatusCodes.Status400BadRequest, "Invalid Input Parameters", "INVALID_INPUT"),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Access Denied", "UNAUTHORIZED"),
                
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "INTERNAL_ERROR")
            };

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = _env.IsDevelopment() ? exception.Message : GetUserFriendlyMessage(statusCode, exception),
                Instance = httpContext.Request.Path
            };

            problemDetails.Extensions.Add("errorCode", errorCode);

            if (_env.IsDevelopment())
            {
                problemDetails.Extensions.Add("stackTrace", exception.StackTrace);
            }

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }

        private string GetUserFriendlyMessage(int statusCode, Exception ex)
        {
            if (statusCode == StatusCodes.Status500InternalServerError)
                return "A system error occurred while processing the workflow. Our engineers have been notified.";
            
            if (statusCode == StatusCodes.Status503ServiceUnavailable)
                return "The workflow system is currently unable to communicate with external messaging services. Please try again later.";

            return ex.Message;
        }
    }
}
