using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;

// Boot NLog before anything else
var logger = LogManager.Setup()
    .LoadConfigurationFromFile("nlog.config")
    .GetCurrentClassLogger();

logger.Debug("Gateway starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Configuration
        .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
        .AddJsonFile("swagger.json", optional: false, reloadOnChange: true);

    // SSL bypass for local dev
    builder.Services.AddHttpClient("SwaggerForOcelot")
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

    // JWT
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer("Bearer", options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudiences = new[]
                {
                    builder.Configuration["Jwt:Audience0"],
                    builder.Configuration["Jwt:Audience1"],
                    builder.Configuration["Jwt:Audience2"],
                    builder.Configuration["Jwt:Audience3"]
                },
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
            };
        });

    // CORS (dev): allow Angular dev server to call Gateway
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DevCors", policy =>
        {
            policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin();
        });
    });

    builder.Services.AddAuthorization();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddSwaggerForOcelot(builder.Configuration);
    //builder.Services.AddOcelot();
    builder.Services.AddOcelot().AddCacheManager(x => x.WithDictionaryHandle());

    var app = builder.Build();
    app.UseCors("DevCors");
    app.UseExceptionHandler();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseSwaggerForOcelotUI(opt =>
    {
        opt.PathToSwaggerGenerator = "/swagger/docs";
    });

    await app.UseOcelot();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Gateway stopped due to exception");
}
finally
{
    LogManager.Shutdown();
}