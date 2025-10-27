using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SharedLibreries.Contracts;
using SharedLibreries.Infrastructure.Database;
using SharedLibreries.Infrastructure.RabbitMQ;
using SharedLibreries.RabbitMQ;
using WorkerServices.WorkerToDo.Repositories;
using WorkerToDo.Data;
using WorkerToDo.Handlers;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting ToDo Worker Service");

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((context, services) =>
        {
            // Add resilient database with connection pooling and retry policies
            services.AddResilientDatabase<ToDoDbContext>(context.Configuration, "PostgreSQL");

            // Add repositories
            services.AddScoped<IItemRepository, ItemRepository>();

            // Add message handlers for item operations
            services.AddScoped<IMessageHandler<CreateItemRequest, CreateItemResponse>, CreateItemMessageHandler>();
            services.AddScoped<IMessageHandler<GetItemRequest, GetItemResponse>, GetItemMessageHandler>();
            services.AddScoped<IMessageHandler<GetAllItemsRequest, GetAllItemsResponse>, GetAllItemsMessageHandler>();
            services.AddScoped<IMessageHandler<GetUserItemsRequest, GetUserItemsResponse>, GetUserItemsMessageHandler>();
            services.AddScoped<IMessageHandler<UpdateItemRequest, UpdateItemResponse>, UpdateItemMessageHandler>();
            services.AddScoped<IMessageHandler<DeleteItemRequest, DeleteItemResponse>, DeleteItemMessageHandler>();

            // Add resilient RabbitMQ RPC Server with circuit breaker and retry policies for item operations only
            services.AddItemRabbitMqRpcServer();
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
    Log.Fatal(ex, "ToDo Worker Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}