using Auth.Exceptions;
using Auth.Repository;
using Auth.Services;
using ECommerceProductManagement.Data;
using ECommerceProductManagement.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var logger = LogManager.Setup()
    .LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();


var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Host.UseNLog();
// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddCors();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UsersDb")));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<Auth.Services.AuthService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false, // Setting to false as we have multiple audiences
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseCors(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
