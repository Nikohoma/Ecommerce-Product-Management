// Middleware/GlobalExceptionHandler.cs
using CatalogService.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

        public async ValueTask<bool> TryHandleAsync(HttpContext context,Exception exception,CancellationToken ct)
        {
            var (status, title) = exception switch
            {
                ProductNotFoundException or
                VariantNotFoundException => (404, "Not Found"),

                ProductAlreadyExistsException or
                VariantSkuConflictException => (409, "Conflict"),

                InvalidProductStatusTransitionException or
                NegativeStockException or
                InsufficientStockException => (422, "Unprocessable Entity"),

                _ => (500, "Internal Server Error")
            };

            if (status == 500)
                _logger.LogError(exception, "Unhandled exception.");
            else
                _logger.LogWarning(exception, "Handled domain exception: {Code}",
                    (exception as CatalogException)?.Code);

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exception.Message,
                Extensions =
                {
                    ["code"] = (exception as CatalogException)?.Code ?? "INTERNAL_ERROR"
                }
            };

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(problem, ct);
            return true;
        }
    }
}