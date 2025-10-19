# WorkerToDo Service

## Overview
The WorkerToDo service is responsible for handling all item/todo-related operations in the ToDoList application. It operates as a separate microservice with its own database context and message queue.

## Architecture
- **Database**: PostgreSQL with Entity Framework Core
- **Message Queue**: RabbitMQ (Item Queue only)
- **Logging**: Serilog
- **Dependency Injection**: .NET Core DI Container

## Responsibilities
- Create, read, update, delete todo items
- Manage item-user relationships
- Handle item-specific business logic
- Process item-related messages from the API

## Queue Separation
- **WorkerToDo**: Consumes from `item.operations` queue only
- **WorkerUser**: Consumes from `user.operations` queue only

## Message Handlers
- CreateItemMessageHandler
- GetItemMessageHandler
- GetAllItemsMessageHandler
- GetUserItemsMessageHandler
- UpdateItemMessageHandler
- DeleteItemMessageHandler

## Database Context
- Separate ToDoDbContext for item operations only
- Includes Item entity with soft delete support
- Automatic timestamp management

## Configuration
- appsettings.json: Connection strings and RabbitMQ configuration
- appsettings.Development.json: Development-specific settings
