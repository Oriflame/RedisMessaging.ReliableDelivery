using System.Collections.Generic;
using Moq;
using RedisMessaging.ReliableDelivery.Subscribe;
using Xunit;

namespace RedisMessaging.ReliableDelivery.Tests.Subscribe
{
    public class MessageHandlerTest
    {
        [Fact]
        public void FailoverMissingMessages()
        {
            // arrange
            var messageValidator = new Mock<IMessageValidator>(MockBehavior.Strict);
            var messageLoader = new Mock<IMessageLoader>(MockBehavior.Strict);
            messageValidator.Setup(_ => _.Validate(It.IsAny<Message>()))
                .Returns(new ValidationResultForMissingMessages(1));
            messageLoader.Setup(_ => _.GetMessages("test-channel", 2, 122))
                .Returns(new [] { new Message(2, "missing message") });

            // act
            var successfullyProcessedMessages = new List<Message>();
            var messageHandler = new MessageHandler("test-channel", msg => successfullyProcessedMessages.Add(msg), messageValidator.Object, messageLoader.Object);

            messageHandler.HandleMessage(new Message(123, "message"));

            // assert
            Assert.Equal(2, successfullyProcessedMessages.Count);
            messageLoader.Verify(_ => _.GetMessages("test-channel", 2, 122));
        }
    }
}