using RedisMessaging.ReliableDelivery.Subscribe;
using Xunit;

namespace RedisMessaging.ReliableDelivery.Tests.Subscribe
{
    public class MessageValidatorTest
    {
        [Fact]
        public void ConsecutiveMessagesShouldPassValidation()
        {
            // arrange
            var validator = new MessageValidator();

            // act & assert
            Assert.Same(MessageValidationResult.Success,  validator.Validate("test-message", 2));
            Assert.Same(MessageValidationResult.Success, validator.Validate("test-message", 3));
        }

        [Fact]
        public void WhenMessageMissingIsDetectedThenValidationShouldFail()
        {
            // arrange
            var validator = new MessageValidator();

            // act & assert
            Assert.Same(MessageValidationResult.Success, validator.Validate("test-message", 1));

            var failureResult = validator.Validate("test-message", 3);
            Assert.Equal(3, validator.LastMessageId);
            var resultForMissingMessages = Assert.IsAssignableFrom<ValidationResultForMissingMessages>(failureResult);
            Assert.Equal(1, resultForMissingMessages.LastProcessedMessageId); // message with ID=2 is missing
        }

        [Fact]
        public void SkipValidationWhenMessagesNotReceivedYet()
        {
            // arrange
            var validator = new MessageValidator();

            // act & assert
            Assert.Same(MessageValidationResult.Success, validator.Validate("test-message", 2000)); // some large message ID
        }
    }
}