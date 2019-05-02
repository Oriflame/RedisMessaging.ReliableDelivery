using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using RedisMessaging.ReliableDelivery.Subscribe;
using Xunit;

namespace RedisMessaging.ReliableDelivery.Tests
{
    public class ReliableSubscriberTest: IClassFixture<RedisFixture>
    {
        private readonly RedisFixture _redis;
        private Mock<IMessageValidationFailureHandler> _errorHandler;
        private Mock<ILogger<ReliableSubscriber>> _logger;

        public ReliableSubscriberTest(RedisFixture redis)
        {
            _redis = redis;
            _errorHandler = new Mock<IMessageValidationFailureHandler>(MockBehavior.Strict);
            _logger = new Mock<ILogger<ReliableSubscriber>>(MockBehavior.Strict);
        }

        [Fact]
        public void SubscribeTwiceToSameChannelShouldFail()
        {
            // arrange
            var subscriber = new ReliableSubscriber(_redis.GetConnection(), null, null, null);

            // act & assert
            subscriber.Subscribe("testChannel", (channel, message) => { });

            var exception = Assert.Throws<ArgumentException>(() => subscriber.Subscribe("testChannel", (channel, message) => { }));
            Assert.Contains("There already exists a handler subscribed to channel", exception.Message);
        }

        [Fact]
        public async Task SubscribeAsyncTwiceToSameChannelShouldFail()
        {
            // arrange
            var subscriber = new ReliableSubscriber(_redis.GetConnection(), null, null, null);

            // act & assert
            await subscriber.SubscribeAsync("testChannel", (channel, message) => { });

            var exception = await Assert.ThrowsAsync<ArgumentException>(() => subscriber.SubscribeAsync("testChannel", (channel, message) => { }));
            Assert.Contains("There already exists a handler subscribed to channel", exception.Message);
        }

        [Fact]
        public void ReceivingMessageInInvalidFormatShouldFail()
        {
            // arrange
            _errorHandler.Setup(_ => _.OnInvalidMessageFormat(It.IsAny<string>(), It.IsAny<string>()));

            _logger.Setup(_ => _.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()));

            var subscriber = new ReliableSubscriber(_redis.GetConnection(), null, _errorHandler.Object, _logger.Object);
            //var publisher = new Publisher(_redis.GetConnection().GetSubscriber()); // standard, not-reliable publisher
            var publisher = _redis.GetConnection().GetSubscriber(); // standard, not-reliable publisher

            // act
            int receivedMessagesCount = 0;
            subscriber.Subscribe("testChannel", (channel, message) => ++receivedMessagesCount);
            publisher.Publish("testChannel", "message");
            Thread.Sleep(50);

            // assert
            Assert.Equal(0, receivedMessagesCount);
            _errorHandler.Verify(_ => _.OnInvalidMessageFormat("testChannel", "message"), Times.Once);
            _logger.Verify(_ => _.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void MessageIdOfLongTypeShouldNotCauseOverflowException()
        {
            // arrange
            _logger.Setup(_ => _.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()));

            var messageValidator = new Mock<IMessageValidator>(MockBehavior.Strict);
            messageValidator.Setup(_ => _.Validate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
                .Returns(true);

            var subscriber = new ReliableSubscriber(_redis.GetConnection(), messageValidator.Object, _errorHandler.Object, _logger.Object);
            //var publisher = new Publisher(_redis.GetConnection()); // standard, not-reliable publisher
            var publisher = _redis.GetConnection().GetSubscriber(); // standard, not-reliable publisher

            // act
            int receivedMessagesCount = 0;
            subscriber.Subscribe("testChannel", (channel, message) => ++receivedMessagesCount);
            publisher.Publish("testChannel", $"{long.MaxValue}:message"); // simulation of sending messageId of type long
            Thread.Sleep(50);

            // assert
            Assert.Equal(1, receivedMessagesCount);
            messageValidator.Verify(_ => _.Validate("testChannel", "message", long.MaxValue), Times.Once);
        }

        [Fact]
        public void ReceivingMessageWithIdInInvalidFormatShouldFail()
        {
            // arrange
            _errorHandler.Setup(_ => _.OnInvalidMessageFormat(It.IsAny<string>(), It.IsAny<string>()));

            _logger.Setup(_ => _.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()));

            var subscriber = new ReliableSubscriber(_redis.GetConnection(), null, _errorHandler.Object, _logger.Object);
            var publisher = _redis.GetConnection().GetSubscriber(); // standard, not-reliable publisher

            // act
            int receivedMessagesCount = 0;
            subscriber.Subscribe("testChannel", (channel, message) => ++receivedMessagesCount);
            publisher.Publish("testChannel", "invalid:message");
            Thread.Sleep(50);

            // assert
            Assert.Equal(0, receivedMessagesCount);
            _errorHandler.Verify(_ => _.OnInvalidMessageFormat("testChannel", "invalid:message"), Times.Once);
            _logger.Verify(_ => _.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()), Times.Once);
        }
    }
}