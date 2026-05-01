using NLog;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var start = DateTime.UtcNow;

        await _next(context);

        var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;

        Logger.Info("{method} {path}{query} => {status} ({elapsed}ms)",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            context.Response.StatusCode,
            Math.Round(elapsed, 1));
    }
}