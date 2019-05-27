using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Oriflame.RedisMessaging.ReliableDelivery.Publish;
using Oriflame.RedisMessaging.ReliableDelivery.Subscribe;
using StackExchange.Redis;

namespace Oriflame.RedisMessaging.ReliableDelivery
{
    internal class ReliablePublisherSubscriber : IReliablePublisherSubscriber
    {
        private readonly ReliablePublisher _publisher;
        private readonly ReliableSubscriber _subscriber;
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public ReliablePublisherSubscriber(IConnectionMultiplexer connectionMultiplexer, ILoggerFactory loggerFactory = default)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _publisher = new ReliablePublisher(connectionMultiplexer);
            var messageParser = new MessageParser(ToLogger<MessageParser>(loggerFactory));
            _subscriber = new ReliableSubscriber(connectionMultiplexer, messageParser, loggerFactory);
        }

        public void Publish(string channel, string message, TimeSpan messageExpiration = default)
        {
            _publisher.Publish(channel, message, messageExpiration);
        }

        public Task PublishAsync(string channel, string message, TimeSpan messageExpiration = default)
        {
            return _publisher.PublishAsync(channel, message, messageExpiration);
        }

        public IMessageDeliveryChecker Subscribe(
            string channel,
            MessageAction onExpectedMessage,
            MessageAction onMissedMessage = default,
            MessageAction onDuplicatedMessage = default,
            MessagesCountAction onMissingMessages = default)
        {
            var messageHandler = new MessageHandler(onExpectedMessage, onMissedMessage, onDuplicatedMessage, onMissingMessages);
            return _subscriber.Subscribe(channel, messageHandler);
        }

        public Task<IMessageDeliveryChecker> SubscribeAsync(
            string channel,
            MessageAction onExpectedMessage,
            MessageAction onMissedMessage = default,
            MessageAction onDuplicatedMessage = default,
            MessagesCountAction onMissingMessages = default)
        {
            var messageHandler = new MessageHandler(onExpectedMessage, onMissedMessage, onDuplicatedMessage, onMissingMessages);
            return _subscriber.SubscribeAsync(channel, messageHandler);
        }

        public void Unsubscribe(string channel)
        {
            _connectionMultiplexer.GetSubscriber().Unsubscribe(channel);
        }

        public Task UnsubscribeAsync(string channel)
        {
            return _connectionMultiplexer.GetSubscriber().UnsubscribeAsync(channel);
        }

        private static ILogger<T> ToLogger<T>(ILoggerFactory loggerFactory)
        {
            return loggerFactory == default
                ? NullLogger<T>.Instance
                : loggerFactory.CreateLogger<T>();
        }
    }
}