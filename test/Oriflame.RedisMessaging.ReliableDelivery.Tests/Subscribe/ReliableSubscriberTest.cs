﻿using System;
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
                ILoggerFactory loggerFactory) : base(connectionMultiplexer, messageParser, loggerFactory)
            {
            }

            public Exception LastException { get; private set; }

            public long ExceptionsCount { get; private set; }

            protected override bool OnMessageHandlingException(Exception exception)
            {
                ++ExceptionsCount;
                LastException = exception;

                return base.OnMessageHandlingException(exception);
            }
        }

        private readonly RedisFixture _redis;
        private readonly Mock<IMessageParser> _messageParser;
        private readonly Mock<IMessageHandler> _messageHandler;

        public ReliableSubscriberTest(RedisFixture redis)
        {
            _redis = redis;
            _messageParser = new Mock<IMessageParser>(MockBehavior.Strict);
            _messageHandler = new Mock<IMessageHandler>(MockBehavior.Strict);
        }

        [Fact]
        public void SubscribeTwiceToSameChannelShouldFail()
        {
            // arrange
            var subscriber = new ReliableSubscriber(_redis.GetConnection(), null);

            // act & assert
            subscriber.Subscribe("testChannel", _messageHandler.Object);

            var exception = Assert.Throws<ArgumentException>(() => subscriber.Subscribe("testChannel", _messageHandler.Object));
            Assert.Contains("There already exists a handler subscribed to channel", exception.Message);
        }

        [Fact]
        public async Task SubscribeAsyncTwiceToSameChannelShouldFail()
        {
            // arrange
            var subscriber = new ReliableSubscriber(_redis.GetConnection(), null);

            // act & assert
            await subscriber.SubscribeAsync("testChannel", _messageHandler.Object);

            var exception = await Assert.ThrowsAsync<ArgumentException>(() => subscriber.SubscribeAsync("testChannel", _messageHandler.Object));
            Assert.Contains("There already exists a handler subscribed to channel", exception.Message);
        }

        [Fact]
        public void ReceivingMessageInInvalidFormatShouldNotInvokeMessageHandler()
        {
            // arrange
            var log = new Mock<ILoggerFactory>(MockBehavior.Strict);
            log.Setup(_ => _.CreateLogger(It.IsAny<string>()).Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()));

            Message parsedMessage;
            _messageParser.Setup(_ => _.TryParse(It.IsAny<string>(), out parsedMessage))
                .Returns(false); // simulation of message in invalid format

            var subscriber = new ReliableSubscriberTraceable(_redis.GetConnection(), _messageParser.Object, log.Object);
            var publisher = _redis.GetConnection().GetSubscriber(); // standard, not-reliable publisher

            // act
            subscriber.Subscribe("testChannel", _messageHandler.Object);
            publisher.Publish("testChannel", "message");
            Thread.Sleep(50);

            // assert
            Assert.Null(subscriber.LastException);
            Assert.Equal(0, subscriber.ExceptionsCount);
            _messageParser.Verify(_ => _.TryParse("message", out parsedMessage), Times.Once);

            log.Verify(_ => _.CreateLogger(It.IsAny<string>()).Log(
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

            _messageHandler.Setup(_ => _.OnExpectedMessage(It.IsAny<string>(), It.IsAny<Message>()));

            var subscriber = new ReliableSubscriberTraceable(_redis.GetConnection(), _messageParser.Object, null);
            var publisher = _redis.GetConnection().GetSubscriber(); // standard, not-reliable publisher

            // act
            subscriber.Subscribe("testChannel", _messageHandler.Object);
            publisher.Publish("testChannel", $"{messageId}:message"); // simulation of sending messageId of type long
            Thread.Sleep(50);

            // assert
            Assert.Null(subscriber.LastException);
            Assert.Equal(0, subscriber.ExceptionsCount);
            _messageParser.Verify(_ => _.TryParse($"{messageId}:message", out parsedMessage), Times.Once);
            _messageHandler.Verify(_ => _.OnExpectedMessage("testChannel", new Message(messageId, "message")), Times.Once);
        }
    }
}