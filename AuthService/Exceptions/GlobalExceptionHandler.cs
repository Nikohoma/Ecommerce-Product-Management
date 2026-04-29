using System.Net;
using System.Text.Json;
using Auth.Exceptions;

namespace Auth.Exceptions
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");

                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var statusCode = ex switch
            {
                RegistrationException => HttpStatusCode.BadRequest,
                LoginException => HttpStatusCode.Unauthorized,
                OtpValidationException => HttpStatusCode.BadRequest,
                OtpDeliveryException => HttpStatusCode.InternalServerError,
                TokenRefreshException => HttpStatusCode.Unauthorized,
                LogoutException => HttpStatusCode.BadRequest,
                PasswordResetException => HttpStatusCode.BadRequest,
                UserPersistenceException => HttpStatusCode.InternalServerError,
                TokenRevocationException => HttpStatusCode.InternalServerError,

                _ => HttpStatusCode.InternalServerError
            };

            response.StatusCode = (int)statusCode;

            var result = JsonSerializer.Serialize(new
            {
                success = false,
                message = ex.Message,
                type = ex.GetType().Name
            });

            await response.WriteAsync(result);
        }
    }
}