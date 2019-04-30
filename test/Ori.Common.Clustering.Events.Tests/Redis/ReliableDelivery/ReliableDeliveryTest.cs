using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using RedisMessaging.ReliableDelivery.Publish;
using RedisMessaging.ReliableDelivery.Subscribe;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace RedisMessaging.ReliableDelivery.Tests.Redis.ReliableDelivery
{
    [Trait("Category", "Integration test")]
    public class ReliableDeliveryTest : IClassFixture<RedisFixture>
    {
        private readonly ITestOutputHelper _output;
        private readonly RedisFixture _redis;
        private readonly ConnectionMultiplexer _subscriberConnection;

        public ReliableDeliveryTest(ITestOutputHelper output, RedisFixture redis)
        {
            _output = output;
            _redis = redis;
            var endpoint = redis.GetConnection().GetEndPoints().First();

            // we create a separate subscriber multiplexer in order to avoid some unknown interference with a publisher multiplexer
            _subscriberConnection = ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { endpoint }
            });

            _output.WriteLine($"Running redis for {GetType().Name} on {endpoint}");
        }

        public string RandomSuffix => DateTime.Now.Ticks.ToString();

        [Fact]
        public void PublishTest()
        {
            // arrange
            var channelName = nameof(PublishTest) + RandomSuffix;
            var publisher = new ReliablePublisher(_redis.GetConnection());
            var messageCaught = "";
            var subscriber = _subscriberConnection.GetSubscriber();
            subscriber.Subscribe(channelName, (channel, message) => messageCaught += message); // standard, not-reliable subscriber
            //var subscriber = _subscriberConnection.GetSubscriber().Subscribe(channelName, (channel, message) => messageCaught += message) new Subscriber(_subscriberConnection); // standard, not-reliable subscriber
            //subscriber.Subscribe(channelName, (channel, message) => messageCaught += message);

            // act
            publisher.Publish(channelName, "test message:my message");
            Thread.Sleep(2);

            // assert
            Assert.Equal("1:test message:my message", messageCaught);
        }

        [Fact]
        public async Task PublishAsyncTest()
        {
            // arrange
            var channelName = nameof(PublishAsyncTest) + RandomSuffix;
            var publisher = new ReliablePublisher(_redis.GetConnection());
            var subscriber = _subscriberConnection.GetSubscriber(); // new Subscriber(_subscriberConnection); // standard, not-reliable subscriber
            var messageCaught = "";
            await _subscriberConnection.GetSubscriber().SubscribeAsync(channelName, (channel, message) => messageCaught += message);

            // act
            await publisher.PublishAsync(channelName, "test message");
            Thread.Sleep(5);

            // assert
            Assert.Equal("1:test message", messageCaught);
        }

        [Fact]
        public void SubscribeLoadTest()
        {
            // arrange
            var publisher = new ReliablePublisher(_redis.GetConnection());
            var errorHandler = new Mock<IMessageValidationFailureHandler>(MockBehavior.Strict);
            var messagePreprocessor = new MessageValidator(errorHandler.Object, true);
            var subscriber = new ReliableSubscriber(_subscriberConnection, messagePreprocessor, null, null);
            var channelName = nameof(SubscribeLoadTest) + RandomSuffix;
            var testMessage = JsonConvert.SerializeObject(new { myKey = "test value's" });

            // act
            int messagesReceivedCount = 0;
            subscriber.Subscribe(channelName, (ch, message) =>
            {
                Interlocked.Increment(ref messagesReceivedCount);
            });

            const int messagesCount = 5000;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < messagesCount; i++)
            {
                publisher.PublishAsync(channelName, testMessage);
            }

            // assert
            _output.WriteLine($"All {messagesCount} messages sent at {stopwatch.Elapsed}. Already received messages={messagesReceivedCount}");
            WaitUntil(() => messagesReceivedCount == messagesCount, 1000);
            _output.WriteLine($"All {messagesReceivedCount} messages received at {stopwatch.Elapsed}.");

            Assert.Equal(messagesCount, messagesReceivedCount);
            errorHandler.Verify(_ => _.OnInvalidMessage(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public void MessageDeliveryFailure()
        {
            // arrange
            var publisher = new ReliablePublisher(_redis.GetConnection());
            var errorHandler = new Mock<IMessageValidationFailureHandler>(MockBehavior.Strict);
            int errorsCount = 0;
            errorHandler.Setup(_ => _.OnInvalidMessage(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<long>())).Callback(() => ++errorsCount);
            int formatValidationErrorsCount = 0;
            errorHandler.Setup(_ => _.OnInvalidMessageFormat(
                It.IsAny<string>(), It.IsAny<string>()))
                .Callback(() => ++formatValidationErrorsCount);
            var messagePreprocessor = new MessageValidator(errorHandler.Object, true);
            var subscriber = new ReliableSubscriber(_subscriberConnection, messagePreprocessor, errorHandler.Object, null);
            var channelName = nameof(MessageDeliveryFailure) + RandomSuffix;
            var testMessage = JsonConvert.SerializeObject(new { testKey = "test value's" });

            // act
            int messagesReceivedCount = 0;
            void SubscribeHandler(string channel, string message)
            {
                Interlocked.Increment(ref messagesReceivedCount);
            }

            subscriber.Subscribe(channelName, SubscribeHandler);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            publisher.Publish(channelName, testMessage);
            Thread.Sleep(10);
            Assert.Equal(1, messagesReceivedCount);

            subscriber.Unsubscribe(channelName); // simulation of message lost
            publisher.Publish(channelName, testMessage);
            Thread.Sleep(10);
            Assert.Equal(1, messagesReceivedCount);
            Assert.Equal(0, errorsCount);
            Assert.Equal(0, formatValidationErrorsCount);

            subscriber.Subscribe(channelName, SubscribeHandler);
            publisher.Publish(channelName, testMessage);
            Thread.Sleep(100);
            Assert.Equal(2, messagesReceivedCount);
            Assert.Equal(1, errorsCount);
            Assert.Equal(0, formatValidationErrorsCount);

            // assert
            _output.WriteLine($"All sent={stopwatch.Elapsed}. Received messages={messagesReceivedCount}");
            Thread.Sleep(10);

            _output.WriteLine($"All received={stopwatch.Elapsed}. Received messages={messagesReceivedCount}");
            Assert.Equal(1, errorsCount);
        }

        private static void WaitUntil(Func<bool> stopCondition, int maxTimeoutMilliseconds)
        {
            int elapsedMilliseconds = 0;
            while (true)
            {
                if (stopCondition()
                    || elapsedMilliseconds >= maxTimeoutMilliseconds)
                {
                    return;
                }
                Thread.Sleep(10);
                elapsedMilliseconds += 10;
            }
        }
    }
}