using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Oriflame.RedisMessaging.ReliableDelivery.Publish;
using Oriflame.RedisMessaging.ReliableDelivery.Subscribe;
using StackExchange.Redis;

namespace Oriflame.RedisMessaging.ReliableDelivery
{
    public static class StackExchangeRedisSubscriberExtensions
    {
        public static IReliablePublisher AddReliablePublisher(this ISubscriber subscriber, TimeSpan messageExpiration = default)
        {
            var publisher = new ReliablePublisher(subscriber.Multiplexer);
            if (messageExpiration != default)
            {
                publisher.MessageExpiration = messageExpiration;
            }

            return publisher;
        }

        public static void AddReliableSubscriber(
            this ISubscriber subscriber,
            IMessageHandler messageHandler,
            IMessageParser messageParser = default,
            ILoggerFactory loggerFactory = default)
        {
            var reliableSubscriber = CreateReliableSubscriber(subscriber, messageParser, loggerFactory);
            reliableSubscriber.Subscribe(messageHandler);
        }

        public static Task AddReliableSubscriberAsync(
            this ISubscriber subscriber,
            IMessageHandler messageHandler,
            IMessageParser messageParser = default,
            ILoggerFactory loggerFactory = default)
        {
            var reliableSubscriber = CreateReliableSubscriber(subscriber, messageParser, loggerFactory);

            return reliableSubscriber.SubscribeAsync(messageHandler);
        }

        private static ReliableSubscriber CreateReliableSubscriber(
            ISubscriber subscriber,
            IMessageParser messageParser,
            ILoggerFactory loggerFactory)
        {
            if (messageParser == default)
            {
                var parserLog = loggerFactory?.CreateLogger<MessageParser>();
                messageParser = new MessageParser(parserLog);
            }

            var subscriberLog = loggerFactory?.CreateLogger<ReliableSubscriber>();

            return new ReliableSubscriber(subscriber.Multiplexer, messageParser, subscriberLog);
        }
    }
}