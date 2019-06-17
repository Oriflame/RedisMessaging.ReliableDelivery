using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Oriflame.RedisMessaging.ReliableDelivery.Subscribe;
using StackExchange.Redis;

namespace Oriflame.RedisMessaging.ReliableDelivery
{
    /// <summary>
    /// Extensions methods for <see cref="ISubscriber"/> as entry point to reliable messaging.
    /// </summary>
    public static class StackExchangeRedisSubscriberExtensions
    {
        private static readonly ConditionalWeakTable<IConnectionMultiplexer, ReliablePublisherSubscriber> _pubSubByMultiplexers = new ConditionalWeakTable<IConnectionMultiplexer, ReliablePublisherSubscriber>();

        /// <summary>
        /// Creates a publisher and a subscriber providing reliable message delivery.
        /// </summary>
        /// <param name="subscriber">A low-level Redis subscriber responsible for publishing raw messages to Redis server</param>
        /// <param name="loggerFactory"></param>
        /// <returns>A reliable publisher and subscriber</returns>
        public static IReliablePublisherSubscriber Reliably(this ISubscriber subscriber, ILoggerFactory loggerFactory = default)
        {
            var multiplexer = subscriber.Multiplexer;
            return _pubSubByMultiplexers.GetValue(multiplexer, cm => new ReliablePublisherSubscriber(cm, loggerFactory));
        }
    }
}