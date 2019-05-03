using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Oriflame.RedisMessaging.ReliableDelivery.Subscribe;
using StackExchange.Redis;
using Xunit;

namespace Oriflame.RedisMessaging.ReliableDelivery.Tests.Subscribe
{
    public class ReliableSubscriberTest : IClassFixture<RedisFixture>
    {
        private class ReliableSubscriberTraceable : ReliableSubscriber
        {
            public ReliableSubscriberTraceable(
                IConnectionMultiplexer connectionMultiplexer,
                IMessageParser messageParser,
                ILogger<ReliableSubscriber> log = null) : base(connectionMultiplexer, messageParser, log)
            {
            }

            public Exception LastException { get; private set; }

            public long ExceptionsCount { get; private set; }

            protected override bool OnMessageHandlingException(Exception exception, RedisValue rawMessage)
            {
                ++ExceptionsCount;
                LastException = exception;

                return base.OnMessageHandlingException(exception, rawMessage);
            }
        }

        private readonly RedisFixture _redis;
        private readonly Mock<IMessageParser> _messageParser;
        private readonly Mock<ILogger<ReliableSubscriber>> _log;
        private readonly Mock<IMessageHandler> _messageHandler;

        public ReliableSubscriberTest(RedisFixture redis)
        {
            _redis = redis;
            _messageParser = new Mock<IMessageParser>(MockBehavior.Strict);
            _log = new Mock<ILogger<ReliableSubscriber>>(MockBehavior.Strict);
            _messageHandler = new Mock<IMessageHandler>(MockBehavior.Strict);
        }

        [Fact]
        public void SubscribeTwiceToSameChannelShouldFail()
        {
            // arrange
            var subscriber = new ReliableSubscriber(_redis.GetConnection(), null);
            _messageHandler.SetupGet(_ => _.Channel)
                .Returns("testChannel");

            // act & assert
            subscriber.Subscribe(_messageHandler.Object);

            var exception = Assert.Throws<ArgumentException>(() => subscriber.Subscribe(_messageHandler.Object));
            Assert.Contains("There already exists a handler subscribed to channel", exception.Message);
        }

        [Fact]
        public async Task SubscribeAsyncTwiceToSameChannelShouldFail()
        {
            // arrange
            var subscriber = new ReliableSubscriber(_redis.GetConnection(), null);
            _messageHandler.SetupGet(_ => _.Channel)
                .Returns("testChannel");

            // act & assert
            await subscriber.SubscribeAsync(_messageHandler.Object);

            var exception = await Assert.ThrowsAsync<ArgumentException>(() => subscriber.SubscribeAsync(_messageHandler.Object));
            Assert.Contains("There already exists a handler subscribed to channel", exception.Message);
        }

        [Fact]
        public void ReceivingMessageInInvalidFormatShouldNotInvokeMessageHandler()
        {
            // arrange
            _log.Setup(_ => _.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()));

            Message parsedMessage;
            _messageParser.Setup(_ => _.TryParse(It.IsAny<string>(), out parsedMessage))
                .Returns(false); // simulation of message in invalid format


            var subscriber = new ReliableSubscriberTraceable(_redis.GetConnection(), _messageParser.Object, _log.Object);
            var publisher = _redis.GetConnection().GetSubscriber(); // standard, not-reliable publisher

            _messageHandler.SetupGet(_ => _.Channel)
                .Returns("testChannel");

            // act
            subscriber.Subscribe(_messageHandler.Object);
            publisher.Publish("testChannel", "message");
            Thread.Sleep(50);

            // assert
            Assert.Null(subscriber.LastException);
            Assert.Equal(0, subscriber.ExceptionsCount);
            _messageParser.Verify(_ => _.TryParse("message", out parsedMessage), Times.Once);

            _log.Verify(_ => _.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                null,
                It.IsAny<Func<object, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void MessageWithValidFormatShouldInvokeMessageHandler()
        {
            // arrange
            var messageId = 123;
            var parsedMessage = new Message(messageId, "message");
            _messageParser.Setup(_ => _.TryParse(It.IsAny<string>(), out parsedMessage))
                .Returns(true); // simulation of message in valid format

            _messageHandler.SetupGet(_ => _.Channel)
                .Returns("testChannel");
            _messageHandler.Setup(_ => _.HandleMessage(It.IsAny<Message>()));

            var subscriber = new ReliableSubscriberTraceable(_redis.GetConnection(), _messageParser.Object, _log.Object);
            var publisher = _redis.GetConnection().GetSubscriber(); // standard, not-reliable publisher

            // act
            subscriber.Subscribe(_messageHandler.Object);
            publisher.Publish("testChannel", $"{messageId}:message"); // simulation of sending messageId of type long
            Thread.Sleep(50);

            // assert
            Assert.Null(subscriber.LastException);
            Assert.Equal(0, subscriber.ExceptionsCount);
            _messageParser.Verify(_ => _.TryParse($"{messageId}:message", out parsedMessage), Times.Once);
            _messageHandler.Verify(_ => _.HandleMessage(new Message(messageId, "message")), Times.Once);
        }
    }
}