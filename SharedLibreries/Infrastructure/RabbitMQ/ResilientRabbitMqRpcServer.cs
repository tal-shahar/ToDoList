using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedLibreries.Contracts;
using SharedLibreries.Constants;
using SharedLibreries.RabbitMQ;

namespace SharedLibreries.Infrastructure.RabbitMQ
{
    public class ResilientRabbitMqRpcServer : BaseRabbitMqRpcServer
    {
        public ResilientRabbitMqRpcServer(
            IRabbitMqConnectionManager connectionManager,
            ILogger<ResilientRabbitMqRpcServer> logger,
            IServiceProvider serviceProvider)
            : base(connectionManager, logger, serviceProvider)
        {
        }

        protected override void RegisterMessageTypes()
        {
            // User operations
            _messageTypes[OperationTypes.CreateUser] = typeof(CreateUserRequest);
            _messageTypes[OperationTypes.GetUser] = typeof(GetUserRequest);
            _messageTypes[OperationTypes.GetAllUsers] = typeof(GetAllUsersRequest);
            _messageTypes[OperationTypes.UpdateUser] = typeof(UpdateUserRequest);
            _messageTypes[OperationTypes.DeleteUser] = typeof(DeleteUserRequest);

            // Item operations
            _messageTypes[OperationTypes.CreateItem] = typeof(CreateItemRequest);
            _messageTypes[OperationTypes.GetItem] = typeof(GetItemRequest);
            _messageTypes[OperationTypes.GetAllItems] = typeof(GetAllItemsRequest);
            _messageTypes[OperationTypes.GetUserItems] = typeof(GetUserItemsRequest);
            _messageTypes[OperationTypes.UpdateItem] = typeof(UpdateItemRequest);
            _messageTypes[OperationTypes.DeleteItem] = typeof(DeleteItemRequest);
        }

        protected override string[] GetQueueNames()
        {
            return [QueueNames.UserQueue, QueueNames.ItemQueue];
        }

        protected override async Task<IResponse> ProcessRequestAsync(IRequest request, string operationType)
        {
            using var scope = _serviceProvider.CreateScope();
            
            var operationHandlers = new Dictionary<string, Func<Task<IResponse>>>
            {
                [OperationTypes.CreateUser] = async () => await ProcessUserRequestAsync<CreateUserRequest, CreateUserResponse>(request, scope),
                [OperationTypes.GetUser] = async () => await ProcessUserRequestAsync<GetUserRequest, GetUserResponse>(request, scope),
                [OperationTypes.GetAllUsers] = async () => await ProcessUserRequestAsync<GetAllUsersRequest, GetAllUsersResponse>(request, scope),
                [OperationTypes.UpdateUser] = async () => await ProcessUserRequestAsync<UpdateUserRequest, UpdateUserResponse>(request, scope),
                [OperationTypes.DeleteUser] = async () => await ProcessUserRequestAsync<DeleteUserRequest, DeleteUserResponse>(request, scope),
                
                [OperationTypes.CreateItem] = async () => await ProcessItemRequestAsync<CreateItemRequest, CreateItemResponse>(request, scope),
                [OperationTypes.GetItem] = async () => await ProcessItemRequestAsync<GetItemRequest, GetItemResponse>(request, scope),
                [OperationTypes.GetAllItems] = async () => await ProcessItemRequestAsync<GetAllItemsRequest, GetAllItemsResponse>(request, scope),
                [OperationTypes.GetUserItems] = async () => await ProcessItemRequestAsync<GetUserItemsRequest, GetUserItemsResponse>(request, scope),
                [OperationTypes.UpdateItem] = async () => await ProcessItemRequestAsync<UpdateItemRequest, UpdateItemResponse>(request, scope),
                [OperationTypes.DeleteItem] = async () => await ProcessItemRequestAsync<DeleteItemRequest, DeleteItemResponse>(request, scope)
            };

            if (operationHandlers.TryGetValue(operationType, out var handler))
            {
                return await handler();
            }

            throw new NotSupportedException($"Operation type {operationType} is not supported");
        }

        private static async Task<TResponse> ProcessUserRequestAsync<TRequest, TResponse>(IRequest request, IServiceScope scope)
            where TRequest : IRequest
            where TResponse : IResponse, new()
        {
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TRequest, TResponse>>();
            return await handler.HandleAsync((TRequest)request);
        }

        private static async Task<TResponse> ProcessItemRequestAsync<TRequest, TResponse>(IRequest request, IServiceScope scope)
            where TRequest : IRequest
            where TResponse : IResponse, new()
        {
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TRequest, TResponse>>();
            return await handler.HandleAsync((TRequest)request);
        }

        protected override IResponse CreateErrorResponse(string correlationId, string errorMessage)
        {
            return new CreateUserResponse // Use any response type as base
            {
                CorrelationId = correlationId,
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public static class ResilientRabbitMqRpcServerExtensions
    {
        public static IServiceCollection AddResilientRabbitMqRpcServer(this IServiceCollection services)
        {
            services.AddRabbitMqConnectionManager();
            services.AddSingleton<IRabbitMqRpcServer, ResilientRabbitMqRpcServer>();
            services.AddHostedService<ResilientRabbitMqRpcServer>();
            return services;
        }
    }
}
