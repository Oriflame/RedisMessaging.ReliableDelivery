using Moq;
using RedisMessaging.ReliableDelivery.Subscribe;
using Xunit;

namespace RedisMessaging.ReliableDelivery.Tests
{
    public class MessageValidatorTest
    {
        private readonly Mock<IMessageValidationFailureHandler> _failureHandler;

        public MessageValidatorTest()
        {
            _failureHandler = new Mock<IMessageValidationFailureHandler>(MockBehavior.Strict);
        }

        [Fact]
        public void ConsecutiveMessagesShouldPassValidation()
        {
            // arrange
            var validator = new MessageValidator(_failureHandler.Object, false);

            // act & assert
            Assert.True(validator.Validate("test-channel", "test-message", 2));
            Assert.True(validator.Validate("test-channel", "test-message", 3));
        }

        [Fact]
        public void WhenMessageMissingIsDetectedThenValidationShouldFail()
        {
            // arrange
            var validator = new MessageValidator(_failureHandler.Object, false);
            _failureHandler.Setup(_ => _.OnInvalidMessage(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<long>()));

            // act & assert
            Assert.True(validator.Validate("test-channel", "test-message", 1));
            Assert.False(validator.Validate("test-channel", "test-message", 3)); // message with ID=2 is missing
            Assert.Equal(3, validator.LastMessageId);
            _failureHandler.Verify(_ => _.OnInvalidMessage(
                "test-channel",
                "test-message",
                3,
                1),Times.Once);
        }

        [Fact]
        public void SkipValidationWhenAnyMessagesNotReceivedYet()
        {
            // arrange
            var validator = new MessageValidator(_failureHandler.Object, false);

            // act & assert
            Assert.True(validator.Validate("test-channel", "test-message", 2000)); // some large message ID
        }
    }
}