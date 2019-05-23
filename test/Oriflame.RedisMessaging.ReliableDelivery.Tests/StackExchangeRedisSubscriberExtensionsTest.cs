using System;
using System.Diagnostics;
using Oriflame.RedisMessaging.ReliableDelivery.Subscribe;
using Oriflame.RedisMessaging.ReliableDelivery.Subscribe.Validation;
using StackExchange.Redis;
using Xunit;

namespace Oriflame.RedisMessaging.ReliableDelivery.Tests
{
    public class StackExchangeRedisSubscriberExtensionsTest : IClassFixture<RedisFixture>
    {
        private readonly RedisFixture _redis;

        public StackExchangeRedisSubscriberExtensionsTest(RedisFixture redis)
        {
            _redis = redis;
        }

        [Fact]
        public void ReliablyReturnsSameObjectForSameMultiplexer()
        {
            // arrange & act
            var multiplexer = _redis.CreateMultiplexer();
            var multiplexer2 = _redis.CreateMultiplexer();
            Assert.NotSame(multiplexer, multiplexer2);

            // assert
            Assert.Same(multiplexer.GetSubscriber().Reliably(), multiplexer.GetSubscriber().Reliably());
            Assert.NotSame(multiplexer.GetSubscriber().Reliably(), multiplexer2.GetSubscriber().Reliably());
        }

#if !DEBUG
        [Fact]
        public void ReliablyExtensionMethodDoesNotBlockMultiplexerFromGarbageCollection()
        {
            // arrange
            var multiplexer = _redis.CreateMultiplexer();
            var weakMultiplexer = new WeakReference(multiplexer);
            multiplexer.GetSubscriber().Reliably().Subscribe("test", null);
            multiplexer.GetSubscriber().UnsubscribeAll();

            multiplexer.Dispose();
            multiplexer = null;
            
            // act
            GC.Collect();
            GC.WaitForFullGCComplete();

            // assert
            Assert.False(weakMultiplexer.IsAlive);
        }
#endif
    }
}