using Microsoft.Extensions.Logging;
using SharedLibreries.Contracts;
using SharedLibreries.DTOs;
using SharedLibreries.Models;
using SharedLibreries.RabbitMQ;
using WorkerServices.WorkerUser.Repositories;

namespace WorkerServices.WorkerUser.Handlers
{
    public class CreateUserMessageHandler : IMessageHandler<SharedLibreries.Contracts.CreateUserRequest, SharedLibreries.Contracts.CreateUserResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<CreateUserMessageHandler> _logger;

        public CreateUserMessageHandler(IUserRepository userRepository, ILogger<CreateUserMessageHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<SharedLibreries.Contracts.CreateUserResponse> HandleAsync(SharedLibreries.Contracts.CreateUserRequest request)
        {
            try
            {
                _logger.LogInformation("Processing CreateUser request for email {Email}", request.Email);

                var existingUser = await _userRepository.GetByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return new SharedLibreries.Contracts.CreateUserResponse
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
                return new SharedLibreries.Contracts.CreateUserResponse
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
                return new SharedLibreries.Contracts.CreateUserResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class GetUserMessageHandler : IMessageHandler<SharedLibreries.Contracts.GetUserRequest, SharedLibreries.Contracts.GetUserResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetUserMessageHandler> _logger;

        public GetUserMessageHandler(IUserRepository userRepository, ILogger<GetUserMessageHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<SharedLibreries.Contracts.GetUserResponse> HandleAsync(SharedLibreries.Contracts.GetUserRequest request)
        {
            try
            {
                _logger.LogInformation("Processing GetUser request for ID {UserId}", request.UserId);

                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return new SharedLibreries.Contracts.GetUserResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"User with ID {request.UserId} not found."
                    };
                }

                return new SharedLibreries.Contracts.GetUserResponse
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
                return new SharedLibreries.Contracts.GetUserResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class GetAllUsersMessageHandler : IMessageHandler<SharedLibreries.Contracts.GetAllUsersRequest, SharedLibreries.Contracts.GetAllUsersResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetAllUsersMessageHandler> _logger;

        public GetAllUsersMessageHandler(IUserRepository userRepository, ILogger<GetAllUsersMessageHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<SharedLibreries.Contracts.GetAllUsersResponse> HandleAsync(SharedLibreries.Contracts.GetAllUsersRequest request)
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

                return new SharedLibreries.Contracts.GetAllUsersResponse
                {
                    IsSuccess = true,
                    Users = userResponses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return new SharedLibreries.Contracts.GetAllUsersResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class UpdateUserMessageHandler : IMessageHandler<SharedLibreries.Contracts.UpdateUserRequest, SharedLibreries.Contracts.UpdateUserResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UpdateUserMessageHandler> _logger;

        public UpdateUserMessageHandler(IUserRepository userRepository, ILogger<UpdateUserMessageHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<SharedLibreries.Contracts.UpdateUserResponse> HandleAsync(SharedLibreries.Contracts.UpdateUserRequest request)
        {
            try
            {
                _logger.LogInformation("Processing UpdateUser request for ID {UserId}", request.UserId);

                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return new SharedLibreries.Contracts.UpdateUserResponse
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
                        return new SharedLibreries.Contracts.UpdateUserResponse
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
                return new SharedLibreries.Contracts.UpdateUserResponse
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
                return new SharedLibreries.Contracts.UpdateUserResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class DeleteUserMessageHandler : IMessageHandler<SharedLibreries.Contracts.DeleteUserRequest, SharedLibreries.Contracts.DeleteUserResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<DeleteUserMessageHandler> _logger;

        public DeleteUserMessageHandler(IUserRepository userRepository, ILogger<DeleteUserMessageHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<SharedLibreries.Contracts.DeleteUserResponse> HandleAsync(SharedLibreries.Contracts.DeleteUserRequest request)
        {
            try
            {
                _logger.LogInformation("Processing DeleteUser request for ID {UserId}", request.UserId);

                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return new SharedLibreries.Contracts.DeleteUserResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"User with ID {request.UserId} not found."
                    };
                }

                await _userRepository.DeleteAsync(request.UserId);

                _logger.LogInformation("User deleted successfully with ID {UserId}", request.UserId);
                return new SharedLibreries.Contracts.DeleteUserResponse
                {
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {UserId}", request.UserId);
                return new SharedLibreries.Contracts.DeleteUserResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
