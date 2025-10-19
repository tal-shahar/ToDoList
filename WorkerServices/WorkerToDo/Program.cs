using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SharedLibreries.Contracts;
using SharedLibreries.RabbitMQ;
using WorkerServices.WorkerToDo.Data;
using WorkerServices.WorkerToDo.Handlers;
using WorkerServices.WorkerToDo.Repositories;

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
            // Add DbContext
            services.AddDbContext<ToDoDbContext>(options =>
                options.UseNpgsql(context.Configuration["ConnectionStrings:PostgreSQL"]));

            // Add repositories
            services.AddScoped<IItemRepository, ItemRepository>();

            // Add message handlers for item operations
            services.AddScoped<IMessageHandler<CreateItemRequest, CreateItemResponse>, CreateItemMessageHandler>();
            services.AddScoped<IMessageHandler<GetItemRequest, GetItemResponse>, GetItemMessageHandler>();
            services.AddScoped<IMessageHandler<GetAllItemsRequest, GetAllItemsResponse>, GetAllItemsMessageHandler>();
            services.AddScoped<IMessageHandler<GetUserItemsRequest, GetUserItemsResponse>, GetUserItemsMessageHandler>();
            services.AddScoped<IMessageHandler<UpdateItemRequest, UpdateItemResponse>, UpdateItemMessageHandler>();
            services.AddScoped<IMessageHandler<DeleteItemRequest, DeleteItemResponse>, DeleteItemMessageHandler>();

            // Add RabbitMQ Item RPC Server for item operations
            services.AddRabbitMqItemRpcServer();
        })
        .Build();

    // Apply database migrations on startup
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ToDoDbContext>();
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