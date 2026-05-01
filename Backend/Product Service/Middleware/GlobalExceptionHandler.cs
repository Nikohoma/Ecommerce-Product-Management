using CatalogService.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Middleware
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
            _logger.LogError(exception, "An exception occurred: {Message}", exception.Message);

            var (statusCode, title, code) = exception switch
            {
                ProductNotFoundException ex => (StatusCodes.Status404NotFound, "Product Not Found", ex.Code),
                VariantNotFoundException ex => (StatusCodes.Status404NotFound, "Variant Not Found", ex.Code),
                CategoryNotFoundException ex => (StatusCodes.Status404NotFound, "Category Not Found", ex.Code),
                
                ProductAlreadyExistsException ex => (StatusCodes.Status409Conflict, "Product Conflict", ex.Code),
                VariantSkuConflictException ex => (StatusCodes.Status409Conflict, "SKU Conflict", ex.Code),
                
                InvalidProductStatusTransitionException ex => (StatusCodes.Status422UnprocessableEntity, "Invalid Status Transition", ex.Code),
                NegativeStockException ex => (StatusCodes.Status422UnprocessableEntity, "Invalid Stock", ex.Code),
                InsufficientStockException ex => (StatusCodes.Status422UnprocessableEntity, "Insufficient Inventory", ex.Code),

                ArgumentException => (StatusCodes.Status400BadRequest, "Invalid Argument", "BAD_REQUEST"),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized Access", "UNAUTHORIZED"),
                
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "INTERNAL_ERROR")
            };

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = _env.IsDevelopment() ? exception.Message : (statusCode == 500 ? "An unexpected error occurred. Please try again later." : exception.Message),
                Instance = httpContext.Request.Path
            };

            problemDetails.Extensions.Add("errorCode", code);
            
            if (_env.IsDevelopment())
            {
                problemDetails.Extensions.Add("stackTrace", exception.StackTrace);
            }

            httpContext.Response.StatusCode = statusCode;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
