using System;
using System.Threading;
using System.Threading.Tasks;
using Oriflame.RedisMessaging.ReliableDelivery.Publish;
using Xunit;

namespace Oriflame.RedisMessaging.ReliableDelivery.Tests.Publish
{
    public class ReliablePublisherTest : IClassFixture<RedisFixture>
    {
        private readonly RedisFixture _redis;

        public ReliablePublisherTest(RedisFixture redis)
        {
            _redis = redis;
        }

        [Fact]
        public void PublishTest()
        {
            // arrange
            var channelName = "test-channel-" + nameof(PublishTest);
            var publisher = new ReliablePublisher(_redis.GetConnection());
            var receivedMessage = "";
            var subscriber = _redis.GetConnection().GetSubscriber(); // standard, not-reliable subscriber
            subscriber.Subscribe(channelName, (channel, message) => receivedMessage += message);

            // act
            publisher.Publish(channelName, "test message:my message");
            Thread.Sleep(50);

            // assert
            Assert.Equal("1:test message:my message", receivedMessage);
        }

        [Fact]
        public async Task PublishAsyncTest()
        {
            // arrange
            var channelName = "test-channel-" + nameof(PublishAsyncTest);
            var publisher = new ReliablePublisher(_redis.GetConnection());
            var subscriber = _redis.GetConnection().GetSubscriber();  // standard, not-reliable subscriber
            var receivedMessage = "";
            await subscriber.SubscribeAsync(channelName, (channel, message) => receivedMessage += message);

            // act
            await publisher.PublishAsync(channelName, "test message");
            Thread.Sleep(20);

            // assert
            Assert.Equal("1:test message", receivedMessage);
        }

        [Fact]
        public void PublishWillSaveMessageInRedis()
        {
            // arrange
            var channelName = "test-channel-" + nameof(PublishWillSaveMessageInRedis);
            var publisher = new ReliablePublisher(_redis.GetConnection());

            // act
            var messageExpiration = TimeSpan.FromSeconds(30);
            publisher.Publish(channelName, "test message:my message", messageExpiration);

            // assert
            var database = _redis.GetConnection().GetDatabase();
            var savedMessage = database.StringGetWithExpiry($"ch:{{{channelName}}}:1");
            Assert.Equal("test message:my message", savedMessage.Value);
            Assert.NotNull(savedMessage.Expiry);
            Assert.True(savedMessage.Expiry > TimeSpan.Zero && savedMessage.Expiry < TimeSpan.FromSeconds(60), $"Timespan {savedMessage.Expiry} is not within given range.");
        }
    }
}