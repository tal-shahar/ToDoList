using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedLibreries.Contracts;
using SharedLibreries.Constants;
using SharedLibreries.RabbitMQ;

namespace SharedLibreries.Infrastructure.RabbitMQ
{
    public class UserRabbitMqRpcServer : BaseRabbitMqRpcServer
    {
        public UserRabbitMqRpcServer(
            IRabbitMqConnectionManager connectionManager,
            ILogger<UserRabbitMqRpcServer> logger,
            IServiceProvider serviceProvider)
            : base(connectionManager, logger, serviceProvider)
        {
        }

        protected override void RegisterMessageTypes()
        {
            // User operations only
            _messageTypes[OperationTypes.CreateUser] = typeof(CreateUserRequest);
            _messageTypes[OperationTypes.GetUser] = typeof(GetUserRequest);
            _messageTypes[OperationTypes.GetAllUsers] = typeof(GetAllUsersRequest);
            _messageTypes[OperationTypes.UpdateUser] = typeof(UpdateUserRequest);
            _messageTypes[OperationTypes.DeleteUser] = typeof(DeleteUserRequest);
        }

        protected override string[] GetQueueNames()
        {
            return [QueueNames.UserQueue];
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
                [OperationTypes.DeleteUser] = async () => await ProcessUserRequestAsync<DeleteUserRequest, DeleteUserResponse>(request, scope)
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

        protected override IResponse CreateErrorResponse(string correlationId, string errorMessage)
        {
            return new CreateUserResponse
            {
                CorrelationId = correlationId,
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public static class UserRabbitMqRpcServerExtensions
    {
        public static IServiceCollection AddUserRabbitMqRpcServer(this IServiceCollection services)
        {
            services.AddRabbitMqConnectionManager();
            services.AddSingleton<IRabbitMqRpcServer, UserRabbitMqRpcServer>();
            services.AddHostedService<UserRabbitMqRpcServer>();
            return services;
        }
    }
}

