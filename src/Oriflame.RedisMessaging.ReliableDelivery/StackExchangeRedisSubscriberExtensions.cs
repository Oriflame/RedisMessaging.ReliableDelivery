using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Oriflame.RedisMessaging.ReliableDelivery.Publish;
using Oriflame.RedisMessaging.ReliableDelivery.Subscribe;
using StackExchange.Redis;

namespace Oriflame.RedisMessaging.ReliableDelivery
{
    /// <summary>
    /// Extensions methods for <see cref="ISubscriber"/> as entry point to reliable messaging.
    /// </summary>
    public static class StackExchangeRedisSubscriberExtensions
    {
        /// <summary>
        /// Creates a publisher providing reliable message delivery.
        /// </summary>
        /// <param name="subscriber">A low-level Redis subscriber responsible for publishing raw messages to Redis server</param>
        /// <param name="messageExpiration">An optional time-to-live (TTL) of a message</param>
        /// <returns><see cref="IReliableSubscriber"/> that guaranties reliable message publishing</returns>
        public static IReliablePublisher AddReliablePublisher(this ISubscriber subscriber, TimeSpan messageExpiration = default)
        {
            var publisher = new ReliablePublisher(subscriber.Multiplexer);
            if (messageExpiration != default)
            {
                publisher.MessageExpiration = messageExpiration;
            }

            return publisher;
        }

        /// <summary>
        /// Creates a subscriber providing reliable message delivery.
        /// </summary>
        /// <param name="subscriber">A low-level Redis subscriber responsible for receiving raw messages from Redis server</param>
        /// <param name="messageHandler">An object fro processing received messages</param>
        /// <param name="messageParser">An optional <see cref="IMessageParser"/> for analyzing and parsing a message</param>
        /// <param name="loggerFactory">An optional logger factory for tracing internal activity of a subscriber</param>
        public static void AddReliableSubscriber(
            this ISubscriber subscriber,
            IMessageHandler messageHandler,
            IMessageParser messageParser = default,
            ILoggerFactory loggerFactory = default)
        {
            var reliableSubscriber = CreateReliableSubscriber(subscriber, messageParser, loggerFactory);
            reliableSubscriber.Subscribe(messageHandler);
        }

        /// <summary>
        /// Creates a subscriber (asynchronously) providing reliable message delivery.
        /// </summary>
        /// <param name="subscriber">A low-level Redis subscriber responsible for receiving raw messages from Redis server</param>
        /// <param name="messageHandler">An object fro processing received messages</param>
        /// <param name="messageParser">An optional <see cref="IMessageParser"/> for analyzing and parsing a message</param>
        /// <param name="loggerFactory">An optional logger factory for tracing internal activity of a subscriber</param>
        /// <returns>A continuation task</returns>
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