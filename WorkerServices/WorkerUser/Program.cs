using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SharedLibreries.Contracts;
using SharedLibreries.RabbitMQ;
using WorkerServices.WorkerUser.Data;
using WorkerServices.WorkerUser.Handlers;
using WorkerServices.WorkerUser.Repositories;

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
            // Add DbContext
            services.AddDbContext<ToDoDbContext>(options =>
                options.UseNpgsql(context.Configuration["ConnectionStrings:PostgreSQL"]));

            // Add repositories
            services.AddScoped<IUserRepository, UserRepository>();

            // Add message handlers for user operations only
            services.AddScoped<IMessageHandler<CreateUserRequest, CreateUserResponse>, CreateUserMessageHandler>();
            services.AddScoped<IMessageHandler<GetUserRequest, GetUserResponse>, GetUserMessageHandler>();
            services.AddScoped<IMessageHandler<GetAllUsersRequest, GetAllUsersResponse>, GetAllUsersMessageHandler>();
            services.AddScoped<IMessageHandler<UpdateUserRequest, UpdateUserResponse>, UpdateUserMessageHandler>();
            services.AddScoped<IMessageHandler<DeleteUserRequest, DeleteUserResponse>, DeleteUserMessageHandler>();

            // Add RabbitMQ User RPC Server
            services.AddRabbitMqUserRpcServer();
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
    Log.Fatal(ex, "Worker Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
