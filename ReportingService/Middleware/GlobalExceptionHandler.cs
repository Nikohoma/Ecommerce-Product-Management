using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ReportingService.Exceptions;

namespace ReportingService.Middleware
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
            _logger.LogError(exception, "Reporting Service Exception: {Message}", exception.Message);

            var (statusCode, title, errorCode) = exception switch
            {
                ReportNotFoundException => (StatusCodes.Status404NotFound, "Report Not Found", "REPORT_NOT_FOUND"),
                EmptyReportSetException => (StatusCodes.Status404NotFound, "No Data Available", "EMPTY_REPORTS"),
                
                RabbitMqConnectionException or 
                QueueDeclarationException => (StatusCodes.Status503ServiceUnavailable, "Data Sync Failure", "SYNC_SERVICE_ERROR"),
                
                ReportQueryException or 
                DashboardAggregationException or 
                ReportPersistenceException => (StatusCodes.Status500InternalServerError, "Report Generation Error", "QUERY_ERROR"),

                ReportingException => (StatusCodes.Status400BadRequest, "Reporting Operation Failed", "BAD_REQUEST"),
                
                ArgumentException => (StatusCodes.Status400BadRequest, "Invalid Parameters", "INVALID_INPUT"),
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
                return "An error occurred while generating the report. Please try again or contact support if the issue persists.";
            
            if (statusCode == StatusCodes.Status503ServiceUnavailable)
                return "The reporting system is currently having trouble synchronizing with other services. Reports might be slightly delayed.";

            return ex.Message;
        }
    }
}
