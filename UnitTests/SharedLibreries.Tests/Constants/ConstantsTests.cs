using SharedLibreries.Constants;

namespace SharedLibreries.Tests.Constants
{
    public class QueueConstantsTests
    {
        [Fact]
        public void QueueNames_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.Equal("user.operations", QueueNames.UserQueue);
            Assert.Equal("item.operations", QueueNames.ItemQueue);
            Assert.Equal("dead.letter.exchange", QueueNames.DeadLetterExchange);
            Assert.Equal("dead.letter.queue", QueueNames.DeadLetterQueue);
        }

        [Fact]
        public void OperationTypes_UserOperations_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.Equal("CreateUser", OperationTypes.CreateUser);
            Assert.Equal("GetUser", OperationTypes.GetUser);
            Assert.Equal("GetAllUsers", OperationTypes.GetAllUsers);
            Assert.Equal("UpdateUser", OperationTypes.UpdateUser);
            Assert.Equal("DeleteUser", OperationTypes.DeleteUser);
        }

        [Fact]
        public void OperationTypes_ItemOperations_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.Equal("CreateItem", OperationTypes.CreateItem);
            Assert.Equal("GetItem", OperationTypes.GetItem);
            Assert.Equal("GetAllItems", OperationTypes.GetAllItems);
            Assert.Equal("GetUserItems", OperationTypes.GetUserItems);
            Assert.Equal("UpdateItem", OperationTypes.UpdateItem);
            Assert.Equal("DeleteItem", OperationTypes.DeleteItem);
        }

        [Fact]
        public void RabbitMQConfig_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.Equal("rabbitmq", RabbitMQConfig.HostName);
            Assert.Equal(5672, RabbitMQConfig.Port);
            Assert.Equal("/", RabbitMQConfig.VirtualHost);
            Assert.Equal("guest", RabbitMQConfig.Username);
            Assert.Equal("guest", RabbitMQConfig.Password);
            Assert.Equal(10, RabbitMQConfig.RequestTimeoutSeconds);
        }

        [Fact]
        public void QueueNames_ShouldNotBeNullOrEmpty()
        {
            // Assert
            Assert.False(string.IsNullOrEmpty(QueueNames.UserQueue));
            Assert.False(string.IsNullOrEmpty(QueueNames.ItemQueue));
            Assert.False(string.IsNullOrEmpty(QueueNames.DeadLetterExchange));
            Assert.False(string.IsNullOrEmpty(QueueNames.DeadLetterQueue));
        }

        [Fact]
        public void OperationTypes_ShouldNotBeNullOrEmpty()
        {
            // Assert
            Assert.False(string.IsNullOrEmpty(OperationTypes.CreateUser));
            Assert.False(string.IsNullOrEmpty(OperationTypes.GetUser));
            Assert.False(string.IsNullOrEmpty(OperationTypes.GetAllUsers));
            Assert.False(string.IsNullOrEmpty(OperationTypes.UpdateUser));
            Assert.False(string.IsNullOrEmpty(OperationTypes.DeleteUser));
            Assert.False(string.IsNullOrEmpty(OperationTypes.CreateItem));
            Assert.False(string.IsNullOrEmpty(OperationTypes.GetItem));
            Assert.False(string.IsNullOrEmpty(OperationTypes.GetAllItems));
            Assert.False(string.IsNullOrEmpty(OperationTypes.GetUserItems));
            Assert.False(string.IsNullOrEmpty(OperationTypes.UpdateItem));
            Assert.False(string.IsNullOrEmpty(OperationTypes.DeleteItem));
        }

        [Fact]
        public void RabbitMQConfig_Port_ShouldBeValid()
        {
            // Assert
            Assert.True(RabbitMQConfig.Port > 0);
            Assert.True(RabbitMQConfig.Port <= 65535);
        }

        [Fact]
        public void RabbitMQConfig_RequestTimeoutSeconds_ShouldBeValid()
        {
            // Assert
            Assert.True(RabbitMQConfig.RequestTimeoutSeconds > 0);
        }
    }
}
