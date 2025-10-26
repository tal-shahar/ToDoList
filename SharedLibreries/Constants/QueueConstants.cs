namespace SharedLibreries.Constants
{
    public static class QueueNames
    {
        public const string UserQueue = "user.operations";
        public const string ItemQueue = "item.operations";
        public const string DeadLetterQueue = "dead.letter.queue";
        public const string DeadLetterExchange = "dead.letter.exchange";
    }

    public static class OperationTypes
    {
        public const string CreateUser = "CreateUser";
        public const string GetUser = "GetUser";
        public const string GetAllUsers = "GetAllUsers";
        public const string UpdateUser = "UpdateUser";
        public const string DeleteUser = "DeleteUser";

        public const string CreateItem = "CreateItem";
        public const string GetItem = "GetItem";
        public const string GetAllItems = "GetAllItems";
        public const string GetUserItems = "GetUserItems";
        public const string UpdateItem = "UpdateItem";
        public const string DeleteItem = "DeleteItem";
    }

    public static class RabbitMQConfig
    {
        public const string HostName = "rabbitmq";
        public const int Port = 5672;
        public const string VirtualHost = "/";
        public const string Username = "guest";
        public const string Password = "guest";
        public const int RequestTimeoutSeconds = 30;
        public const int RetryCount = 3;
    }
}
