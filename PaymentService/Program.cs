using PaymentService.Services;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// Razorpay Config
// --------------------
builder.Services.Configure<RazorpaySettings>(
    builder.Configuration.GetSection("Razorpay"));

// --------------------
// Services
// --------------------
builder.Services.AddScoped<RazorpayService>();

// --------------------
// Controllers
// --------------------
builder.Services.AddControllers();

// --------------------
// Swagger (for testing)
// --------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --------------------
// (Optional) CORS - if frontend calls directly
// --------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// --------------------
// Middleware
// --------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

// If you later add JWT
// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

app.Run();