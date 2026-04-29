using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CartService.Exceptions;

namespace CartService.Middleware
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
            _logger.LogError(exception, "Cart Service Exception: {Message}", exception.Message);

            var (statusCode, title, errorCode) = exception switch
            {
                CartNotFoundException => (StatusCodes.Status404NotFound, "Cart Not Found", "CART_NOT_FOUND"),
                CartItemNotFoundException => (StatusCodes.Status404NotFound, "Cart Item Not Found", "ITEM_NOT_FOUND"),
                
                InvalidQuantityException => (StatusCodes.Status400BadRequest, "Invalid Quantity", "INVALID_QUANTITY"),
                
                ArgumentException => (StatusCodes.Status400BadRequest, "Invalid Input", "BAD_REQUEST"),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized Access", "UNAUTHORIZED"),
                
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "INTERNAL_ERROR")
            };

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = _env.IsDevelopment() ? exception.Message : (statusCode == 500 ? "An unexpected error occurred while processing your shopping cart." : exception.Message),
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
    }
}
