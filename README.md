# To-Do List Application

A backend-only To-Do application built with .NET 8, featuring a microservices architecture using RabbitMQ for inter-service communication and PostgreSQL for data persistence.

## 🏗️ Architecture

The application consists of:

- **Web Service (ToDoListAPI)**: RESTful API exposed via Swagger for managing Users and Items
- **Worker Service (WorkerUser)**: Background service that handles user-related operations and persists user data to PostgreSQL
- **Worker Service (WorkerToDo)**: Background service that handles item/todo-related operations and persists item data to PostgreSQL
- **PostgreSQL**: Database for data persistence
- **RabbitMQ**: Message broker for RPC-style communication between services with separate queues for user and item operations
- **SharedLibreries**: Common models, DTOs, contracts, and utilities

## 🚀 Quick Start

### Prerequisites

- [Docker](https://www.docker.com/get-started) and [Docker Compose](https://docs.docker.com/compose/install/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for local development)
- For load testing: [k6](https://k6.io/) (optional)

### Running the Application

1. **Clone the repository** (if not already done):
   ```bash
   git clone <repository-url>
   cd ToDoList
   ```

2. **Start all services using Docker Compose**:
   ```bash
   # Basic startup (1 worker each)
   docker-compose up --build
   
   # Production-ready startup (10 workers each for load handling)
   docker-compose up --scale worker-user=10 --scale worker-todo=10 -d
   ```

   This command will:
   - Build Docker images for the API and Worker services
   - Start PostgreSQL database
   - Start RabbitMQ message broker
   - Start the Web API service
   - Start the WorkerUser service (handles user operations)
   - Start the WorkerToDo service (handles item operations)
   - Set up networking between all services
   
   **Note**: For load testing with 1000 concurrent users, use the scaled version with 10 workers each.

3. **Wait for services to be ready** (usually takes 1-2 minutes):
   - PostgreSQL will initialize the database
   - RabbitMQ will set up separate queues for user and item operations
   - WorkerUser service will apply database migrations for user data
   - WorkerToDo service will apply database migrations for item data
   - API service will start and be ready to accept requests

4. **Access the Swagger UI**:
   Open your browser and navigate to: **http://localhost:8080/swagger**

## 📋 Testing the API

### Available Endpoints

The Swagger UI provides interactive documentation for all available endpoints:

#### User Management
- `POST /api/users` - Create a new user
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

#### Item Management
- `POST /api/items` - Create a new item
- `GET /api/items` - Get all items
- `GET /api/items/{id}` - Get item by ID
- `GET /api/items/user/{userId}` - Get items for a specific user
- `PUT /api/items/{id}` - Update item
- `DELETE /api/items/{id}` - Delete item (soft delete)

### Sample API Usage

#### 1. Create a User
```json
POST /api/users
{
  "name": "John Doe",
  "email": "john.doe@example.com"
}
```

#### 2. Create an Item for the User
```json
POST /api/items
{
  "userId": "user-id-from-step-1",
  "title": "Complete project documentation",
  "description": "Write comprehensive README and API documentation"
}
```

#### 3. Get User's Items
```
GET /api/items/user/{userId}
```

## 🏗️ Microservices Architecture

### Service Separation

The application implements a clean separation of concerns between user and item operations:

#### **WorkerUser Service**
- **Responsibility**: Handles all user-related operations
- **Queue**: Consumes from `user.operations` queue only
- **Database**: Manages User entities and related operations
- **Message Handlers**: CreateUser, GetUser, GetAllUsers, UpdateUser, DeleteUser

#### **WorkerToDo Service**  
- **Responsibility**: Handles all item/todo-related operations
- **Queue**: Consumes from `item.operations` queue only
- **Database**: Manages Item entities and related operations
- **Message Handlers**: CreateItem, GetItem, GetAllItems, GetUserItems, UpdateItem, DeleteItem

#### **Queue Communication**
- **User Operations**: API → `user.operations` queue → WorkerUser service
- **Item Operations**: API → `item.operations` queue → WorkerToDo service
- **Benefits**: Independent scaling, fault isolation, clear separation of concerns

## 🔧 Service Details

### PostgreSQL Database
- **Host**: localhost
- **Port**: 5432
- **Database**: todoapp
- **Username**: postgres
- **Password**: postgres

### RabbitMQ Management
- **Management UI**: http://localhost:15672
- **Username**: guest
- **Password**: guest
- **AMQP Port**: 5672
- **Queues**: 
  - `user.operations` - User-related message queue
  - `item.operations` - Item-related message queue

### Web API Service
- **URL**: http://localhost:8080
- **Swagger UI**: http://localhost:8080/swagger
- **Health Check**: http://localhost:8080/health
- **Request Timeout**: 15 seconds
- **Rate Limiting**: 10 requests/second, 100 requests/minute
- **RabbitMQ Connection Pool**: 100 connections
- **Channel Pool**: 20 channels
- **Compression**: GZIP enabled for all responses

## 📈 Scaling and Performance

### Worker Scaling

The application supports horizontal scaling via Docker Compose:

```bash
# Scale to 10 workers each (20 total) for production load
docker-compose up --scale worker-user=10 --scale worker-todo=10 -d

# Check running workers
docker-compose ps
```

### Database Configuration

Each worker is configured with:
- **Database Connection Pool**: 200 connections
- **Connection Lifetime**: 300 seconds

This allows each worker to handle up to ~75-80 concurrent users effectively.

## 🧪 Load Testing

The application includes comprehensive load testing capabilities to verify performance under high concurrent load.

### Running Load Tests

```bash
# Quick test (1 minute)
docker run --rm -v ${PWD}:/scripts --network host grafana/k6 run /scripts/load-test.js --duration 1m

# Full load test (ramps to 1000 concurrent users)
docker run --rm -v ${PWD}:/scripts --network host grafana/k6 run /scripts/load-test.js
```

### Performance Metrics

- **Target Response Time**: p(95) < 2 seconds
- **Maximum Concurrent Users**: 1000
- **Optimal Configuration**: 10 workers each (20 total workers)
- **Expected Performance**: ~77% success rate at 1000 concurrent users (770 users handled)
- **Database Connection Pool**: 200 connections per worker
- **Request Timeout**: 15 seconds
- **Rate Limiting**: 10 requests/second per IP

**Load Test Results:**
- 5 workers: 75% success rate (750/1000 users) with 25% failures
- 10 workers: 77% success rate (770/1000 users) with 0% failures ✅
- 15 workers: 67.58% success rate (degradation due to resource contention)

See `load-test.js` for detailed test scenarios.

## 🔐 Security

### Security Features

- **Rate Limiting**: Prevents API abuse and DoS attacks
  - 10 requests per second
  - 100 requests per minute
- **Request Timeout**: Automatic timeout after 3 seconds
- **Connection Pooling**: Prevents connection exhaustion
- **Error Handling**: Graceful handling of invalid responses
- **Health Monitoring**: Automatic connection health checks

### Running Security Checks

```bash
# Check for common security issues
powershell -ExecutionPolicy Bypass -File .\security-check.ps1

# Run OWASP ZAP baseline scan
docker run -t ghcr.io/zaproxy/zaproxy:stable zap-baseline.py -t http://localhost:8080
```

## 🛠️ Development

### Running Tests

To run the comprehensive unit test suite:

```bash
# Run all tests
dotnet test UnitTests/

# Run specific test projects
dotnet test UnitTests/SharedLibreries.Tests/
dotnet test UnitTests/ToDoListAPI.Tests/
dotnet test UnitTests/WorkerUser.Tests/
dotnet test UnitTests/WorkerToDo.Tests/
```

### Integration Tests

Integration tests verify the complete application stack including API, RabbitMQ, and database:

```bash
# Basic integration test (health check + API connectivity)
powershell -ExecutionPolicy Bypass -File .\integration-test-simple.ps1

# Full integration test (creates, updates, deletes test data)
powershell -ExecutionPolicy Bypass -File .\integration-test.ps1
```

**Note**: Integration tests verify:
- API endpoints are accessible
- RabbitMQ messaging works correctly
- Database persistence is functioning
- Request/response flow through the entire system

**Status**: ✅ All 128 unit tests passing | ✅ Basic integration tests passing

### Project Structure

```
ToDoList/
├── WebService/
│   └── ToDoListAPI/           # Web API service
├── WorkerServices/
│   ├── WorkerUser/           # Worker service for user operations
│   └── WorkerToDo/           # Worker service for item operations
├── SharedLibreries/          # Shared models, DTOs, contracts
├── UnitTests/               # Comprehensive unit tests
│   ├── SharedLibreries.Tests/
│   ├── ToDoListAPI.Tests/
│   ├── WorkerUser.Tests/     # Tests for user operations
│   └── WorkerToDo.Tests/     # Tests for item operations
├── docker-compose.yml       # Docker orchestration
└── README.md               # This file
```

### Local Development

If you prefer to run services locally instead of Docker:

1. **Start PostgreSQL and RabbitMQ**:
   ```bash
   docker-compose up postgres rabbitmq
   ```

2. **Run the Worker Services**:
   ```bash
   # Terminal 1 - WorkerUser service
   cd WorkerServices/WorkerUser
   dotnet run
   
   # Terminal 2 - WorkerToDo service
   cd WorkerServices/WorkerToDo
   dotnet run
   ```

3. **Run the Web API**:
   ```bash
   cd WebService/ToDoListAPI
   dotnet run
   ```

## 🔍 Monitoring and Debugging

### Docker Compose Logs

View logs for all services:
```bash
docker-compose logs -f
```

View logs for specific service:
```bash
docker-compose logs -f todoapi
docker-compose logs -f worker-user
docker-compose logs -f worker-todo
docker-compose logs -f postgres
docker-compose logs -f rabbitmq
```

### Database Access

Connect to PostgreSQL directly:
```bash
# Linux/Mac
docker exec -it todo-postgres psql -U postgres -d todoapp

# Windows (if you get TTY errors)
docker exec todo-postgres psql -U postgres -d todoapp
```

Check database tables:
```bash
docker exec todo-postgres psql -U postgres -d todoapp -c '\dt'
```

### RabbitMQ Management

Access the RabbitMQ management interface at http://localhost:15672 to:
- Monitor queues and exchanges
- View message statistics
- Debug message routing
- Manage connections and channels

## 🚨 Troubleshooting

### Common Issues

1. **Services not starting**:
   - Ensure Docker is running
   - Check if ports 5432, 5672, 8080, 15672 are available
   - Run `docker-compose down` and `docker-compose up --build` to rebuild

2. **Database connection issues**:
   - Wait for PostgreSQL to fully initialize (check logs)
   - Ensure the connection string is correct

3. **RabbitMQ connection issues**:
   - Check RabbitMQ management UI at http://localhost:15672
   - Verify queues are created properly

4. **API not responding**:
   - Check API service logs: `docker-compose logs todoapi`
   - Verify the service is healthy: `docker-compose ps`

5. **Port conflicts**:
   - If you get "port is already allocated" errors, stop conflicting containers:
     ```bash
     docker ps
     docker stop <container-name>
     docker-compose up --build
     ```

6. **Database tables not found**:
   - If you get "relation does not exist" errors, ensure migrations are applied:
     ```bash
     # Apply migrations for both worker services
     cd WorkerServices/WorkerUser
     dotnet ef migrations add InitialCreate
     cd ../WorkerToDo
     dotnet ef migrations add InitialCreate
     
     # Rebuild containers
     docker-compose down
     docker volume rm todolist_postgres_data
     docker-compose up --build
     ```

7. **RabbitMQ queue conflicts**:
   - If you get "PRECONDITION_FAILED" errors, clear RabbitMQ data:
     ```bash
     docker-compose down
     docker volume rm todolist_rabbitmq_data
     docker-compose up --build
     ```

### Health Checks

Check service status:
```bash
docker-compose ps
```

All services should show "Up" status with healthy indicators.

## 📊 Features

- ✅ **RESTful API** with Swagger documentation
- ✅ **Microservices Architecture** with RabbitMQ RPC communication and separated worker services
- ✅ **PostgreSQL Database** with Entity Framework Core
- ✅ **Soft Delete** for items
- ✅ **Comprehensive Unit Tests** (128 tests covering all components)
- ✅ **Docker Support** for easy deployment
- ✅ **Clean Architecture** following SOLID principles
- ✅ **Advanced Error Handling** with retry policies and circuit breakers
- ✅ **Performance Optimizations**:
  - RabbitMQ connection pool (100 connections, 20 channels)
  - Database connection pool (200 connections per worker)
  - HTTP response compression (GZIP)
  - Request timeout protection (15 seconds)
  - Horizontal scaling support (tested up to 10 workers)
- ✅ **Security & Reliability**:
  - Rate limiting (10 req/sec, 100 req/min)
  - Circuit breaker protection
  - Enhanced JSON error handling
  - Connection health monitoring

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add/update tests as needed
5. Ensure all tests pass
6. Submit a pull request

## 📝 License

This project is licensed under the MIT License.
