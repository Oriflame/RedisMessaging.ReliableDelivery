using System;
using System.Linq;
using RedisMessaging.ReliableDelivery.Subscribe;
using Xunit;

namespace RedisMessaging.ReliableDelivery.Tests.Subscribe
{
    public class MessageLoaderTest : IClassFixture<RedisFixture>
    {
        private readonly RedisFixture _redis;

        public MessageLoaderTest(RedisFixture redis)
        {
            _redis = redis;
        }

        [Fact]
        public void GetMessages()
        {
            // arrange
            var connectionMultiplexer = _redis.GetConnection();
            var database = connectionMultiplexer.GetDatabase();
            database.StringSet("ch:{test-channel}:1", DateTime.Now.Ticks);
            database.StringSet("ch:{test-channel}:3", DateTime.Now.Ticks);

            // act
            var loader = new MessageLoader(connectionMultiplexer);
            var messages = loader.GetMessages("test-channel", 1, 30)
                .ToList();

            // assert
            Assert.Equal(2, messages.Count);
            Assert.All(messages, message => Assert.NotNull(message.Content));
        }
    }
}