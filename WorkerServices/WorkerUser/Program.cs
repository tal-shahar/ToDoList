using Microsoft.EntityFrameworkCore;
using Serilog;
using SharedLibreries.Contracts;
using SharedLibreries.Infrastructure.Database;
using SharedLibreries.Infrastructure.RabbitMQ;
using SharedLibreries.RabbitMQ;
using WorkerServices.WorkerUser.Handlers;
using WorkerUser.Data;
using WorkerUser.Repositories;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting Worker Service");

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((context, services) =>
        {
            // Add resilient database with connection pooling and retry policies
            services.AddResilientDatabase<ToDoDbContext>(context.Configuration, "PostgreSQL");

            // Add repositories
            services.AddScoped<IUserRepository, UserRepository>();

            // Add message handlers for user operations only
            services.AddScoped<IMessageHandler<CreateUserRequest, CreateUserResponse>, CreateUserMessageHandler>();
            services.AddScoped<IMessageHandler<GetUserRequest, GetUserResponse>, GetUserMessageHandler>();
            services.AddScoped<IMessageHandler<GetAllUsersRequest, GetAllUsersResponse>, GetAllUsersMessageHandler>();
            services.AddScoped<IMessageHandler<UpdateUserRequest, UpdateUserResponse>, UpdateUserMessageHandler>();
            services.AddScoped<IMessageHandler<DeleteUserRequest, DeleteUserResponse>, DeleteUserMessageHandler>();

            // Add resilient RabbitMQ RPC Server with circuit breaker and retry policies for user operations only
            services.AddUserRabbitMqRpcServer();
        })
        .Build();

    // Apply database migrations on startup with resilience
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ToDoDbContext>();
        var healthChecker = scope.ServiceProvider.GetRequiredService<IDatabaseHealthChecker>();
        
        Log.Information("Checking database health...");
        var isHealthy = await healthChecker.IsHealthyAsync();
        
        if (!isHealthy)
        {
            Log.Warning("Database is not healthy, attempting to ensure database exists...");
            await healthChecker.EnsureDatabaseExistsAsync();
        }
        
        Log.Information("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully");
    }

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
