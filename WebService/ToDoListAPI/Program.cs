using AspNetCoreRateLimit;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;
using SharedLibreries.Infrastructure.RabbitMQ;
using ToDoListAPI.Services;
using ToDoListAPI.Validators;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add CORS
builder.Services.AddCors();

// Add response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Add rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ToDo API", Version = "v1" });
});

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

// Add resilient RabbitMQ service with connection pooling
builder.Services.AddResilientRabbitMqService(maxPoolSize: 20);

// Add application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IItemService, ItemService>();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline

// Set global request timeout to 15 seconds
app.Use(async (context, next) =>
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
    context.RequestAborted = cts.Token;
    
    try
    {
        await next(context);
    }
    catch (OperationCanceledException)
    {
        if (context.Response.HasStarted == false)
        {
            context.Response.StatusCode = 408;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Request Timeout\"}");
        }
    }
});

// Enable response compression
app.UseResponseCompression();

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

// Add CORS policy
app.UseCors(policy =>
{
    policy
        .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                    ?? new[] { "http://localhost:3000", "http://localhost:8080" })
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
});

// Enable rate limiting (disabled for load testing - uncomment for production)
if (builder.Environment.IsProduction())
{
    app.UseIpRateLimiting();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ToDo API v1");
    c.RoutePrefix = "swagger"; // Serve Swagger UI at /swagger
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

try
{
    Log.Information("Starting ToDo API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
