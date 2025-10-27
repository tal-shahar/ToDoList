using SharedLibreries.Contracts;
using SharedLibreries.DTOs;
using SharedLibreries.Models;
using SharedLibreries.RabbitMQ;
using WorkerUser.Repositories;

namespace WorkerServices.WorkerUser.Handlers
{
    public class CreateUserMessageHandler : IMessageHandler<SharedLibreries.Contracts.CreateUserRequest, CreateUserResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<CreateUserMessageHandler> _logger;

        public CreateUserMessageHandler(IUserRepository userRepository, ILogger<CreateUserMessageHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<CreateUserResponse> HandleAsync(SharedLibreries.Contracts.CreateUserRequest request)
        {
            try
            {
                _logger.LogInformation("Processing CreateUser request for email {Email}", request.Email);

                var existingUser = await _userRepository.GetByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return new CreateUserResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"User with email {request.Email} already exists."
                    };
                }

                var user = new User
                {
                    Name = request.Name,
                    Email = request.Email
                };

                await _userRepository.AddAsync(user);

                _logger.LogInformation("User created successfully with ID {UserId}", user.Id);
                return new CreateUserResponse
                {
                    IsSuccess = true,
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user with email {Email}", request.Email);
                return new CreateUserResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class GetUserMessageHandler : IMessageHandler<GetUserRequest, GetUserResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetUserMessageHandler> _logger;

        public GetUserMessageHandler(IUserRepository userRepository, ILogger<GetUserMessageHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<GetUserResponse> HandleAsync(GetUserRequest request)
        {
            try
            {
                _logger.LogInformation("Processing GetUser request for ID {UserId}", request.UserId);

                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return new GetUserResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"User with ID {request.UserId} not found."
                    };
                }

                return new GetUserResponse
                {
                    IsSuccess = true,
                    User = new UserResponse
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = user.UpdatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with ID {UserId}", request.UserId);
                return new GetUserResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class GetAllUsersMessageHandler : IMessageHandler<GetAllUsersRequest, GetAllUsersResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetAllUsersMessageHandler> _logger;

        public GetAllUsersMessageHandler(IUserRepository userRepository, ILogger<GetAllUsersMessageHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<GetAllUsersResponse> HandleAsync(GetAllUsersRequest request)
        {
            try
            {
                _logger.LogInformation("Processing GetAllUsers request");

                var users = await _userRepository.GetAllAsync();
                var userResponses = users.Select(u => new UserResponse
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                }).ToList();

                return new GetAllUsersResponse
                {
                    IsSuccess = true,
                    Users = userResponses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return new GetAllUsersResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class UpdateUserMessageHandler : IMessageHandler<SharedLibreries.Contracts.UpdateUserRequest, UpdateUserResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UpdateUserMessageHandler> _logger;

        public UpdateUserMessageHandler(IUserRepository userRepository, ILogger<UpdateUserMessageHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<UpdateUserResponse> HandleAsync(SharedLibreries.Contracts.UpdateUserRequest request)
        {
            try
            {
                _logger.LogInformation("Processing UpdateUser request for ID {UserId}", request.UserId);

                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return new UpdateUserResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"User with ID {request.UserId} not found."
                    };
                }

                // Check if email is being changed and if it already exists
                if (user.Email != request.Email)
                {
                    var existingUser = await _userRepository.GetByEmailAsync(request.Email);
                    if (existingUser != null)
                    {
                        return new UpdateUserResponse
                        {
                            IsSuccess = false,
                            ErrorMessage = $"User with email {request.Email} already exists."
                        };
                    }
                }

                user.Name = request.Name;
                user.Email = request.Email;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("User updated successfully with ID {UserId}", user.Id);
                return new UpdateUserResponse
                {
                    IsSuccess = true,
                    User = new UserResponse
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = user.UpdatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId}", request.UserId);
                return new UpdateUserResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class DeleteUserMessageHandler : IMessageHandler<DeleteUserRequest, DeleteUserResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<DeleteUserMessageHandler> _logger;

        public DeleteUserMessageHandler(IUserRepository userRepository, ILogger<DeleteUserMessageHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<DeleteUserResponse> HandleAsync(DeleteUserRequest request)
        {
            try
            {
                _logger.LogInformation("Processing DeleteUser request for ID {UserId}", request.UserId);

                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return new DeleteUserResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"User with ID {request.UserId} not found."
                    };
                }

                await _userRepository.DeleteAsync(request.UserId);

                _logger.LogInformation("User deleted successfully with ID {UserId}", request.UserId);
                return new DeleteUserResponse
                {
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {UserId}", request.UserId);
                return new DeleteUserResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
