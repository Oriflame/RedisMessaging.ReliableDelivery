using Oriflame.RedisMessaging.ReliableDelivery.Subscribe;
using Oriflame.RedisMessaging.ReliableDelivery.Subscribe.Validation;
using Xunit;

namespace Oriflame.RedisMessaging.ReliableDelivery.Tests.Subscribe.Validation
{
    public class MessageValidatorTest
    {
        [Fact]
        public void ConsecutiveMessagesShouldPassValidation()
        {
            // arrange
            var validator = new MessageValidator();

            // act & assert
            var message1 = new Message(2, "test-message");
            Assert.IsType<SuccessValidationResult>(validator.Validate(message1));
            var message2 = new Message(3, "test-message");
            Assert.IsType<SuccessValidationResult>(validator.Validate(message2));
        }

        [Fact]
        public void WhenMessageMissingIsDetectedThenValidationShouldFail()
        {
            // arrange
            var validator = new MessageValidator();

            // act & assert
            var message1 = new Message(1, "test-message");
            Assert.IsType<SuccessValidationResult>(validator.Validate(message1));

            var message2 = new Message(3, "test-message");
            var failureResult = validator.Validate(message2);
            Assert.Equal(3, validator.LastMessageId);
            var resultForMissingMessages = Assert.IsAssignableFrom<ValidationResultForMissingMessages>(failureResult);
            Assert.Equal(1, resultForMissingMessages.LastProcessedMessageId); // message with ID=2 is missing
        }

        [Fact]
        public void DuplicitMessageReceived()
        {
            // arrange
            var validator = new MessageValidator();
            var message1 = new Message(1, "test-message");

            // act & assert
            Assert.IsType<SuccessValidationResult>(validator.Validate(message1));
            Assert.IsType<AlreadyProcessedValidationResult>(validator.Validate(message1));
        }

        [Fact]
        public void SkipValidationWhenMessagesNotReceivedYet()
        {
            // arrange
            var validator = new MessageValidator();

            // act & assert
            var message = new Message(2000, "test-message"); // some large message ID
            Assert.IsType<SuccessValidationResult>(validator.Validate(message));
        }
    }
}