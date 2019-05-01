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

namespace RedisMessaging.ReliableDelivery.Tests
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

        // TODO move to ReliablePublisherTest
        [Fact]
        public void PublishTest()
        {
            // arrange
            var channelName = nameof(PublishTest) + RandomSuffix;
            var publisher = new ReliablePublisher(_redis.GetConnection());
            var receivedMessage = "";
            var subscriber = _subscriberConnection.GetSubscriber(); // standard, not-reliable subscriber
            subscriber.Subscribe(channelName, (channel, message) => receivedMessage += message);

            // act
            publisher.Publish(channelName, "test message:my message");
            Thread.Sleep(2);

            // assert
            Assert.Equal("1:test message:my message", receivedMessage);
        }

        // TODO move to ReliablePublisherTest
        [Fact]
        public async Task PublishAsyncTest()
        {
            // arrange
            var channelName = nameof(PublishAsyncTest) + RandomSuffix;
            var publisher = new ReliablePublisher(_redis.GetConnection());
            var subscriber = _subscriberConnection.GetSubscriber();  // standard, not-reliable subscriber
            var receivedMessage = "";
            await subscriber.SubscribeAsync(channelName, (channel, message) => receivedMessage += message);

            // act
            await publisher.PublishAsync(channelName, "test message");
            Thread.Sleep(5);

            // assert
            Assert.Equal("1:test message", receivedMessage);
        }

        [Fact]
        public void SubscribeLoadTest()
        {
            // arrange
            var publisher = new ReliablePublisher(_redis.GetConnection());
            var messageParser = new MessageParser();
            var subscriber = new ReliableSubscriber(null, _subscriberConnection, messageParser);
            var channelName = nameof(SubscribeLoadTest) + RandomSuffix;
            var testMessage = JsonConvert.SerializeObject(new { myKey = "test value's" });

            var messageValidator = new MessageValidator();
            int messagesReceivedCount = 0;
            var messageHandler = new MessageHandler(channelName, message =>
            {
                Interlocked.Increment(ref messagesReceivedCount);
            }, messageValidator);

            // act
            subscriber.Subscribe(messageHandler);

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
        }

        // TODO
        //[Fact]
        //public void MessageDeliveryFailure()
        //{
        //    // arrange
        //    var publisher = new ReliablePublisher(_redis.GetConnection());
        //    //var errorHandler = new Mock<IMessageValidationFailureHandler>(MockBehavior.Strict);
        //    int errorsCount = 0;
        //    //errorHandler.Setup(_ => _.OnInvalidMessage(
        //    //    It.IsAny<string>(),
        //    //    It.IsAny<string>(),
        //    //    It.IsAny<long>(),
        //    //    It.IsAny<long>())).Callback(() => ++errorsCount);
        //    int formatValidationErrorsCount = 0;
        //    //errorHandler.Setup(_ => _.OnInvalidMessageFormat(
        //    //    It.IsAny<string>(), It.IsAny<string>()))
        //    //    .Callback(() => ++formatValidationErrorsCount);
        //    var messageParser = new MessageParser();
        //    var subscriber = new ReliableSubscriber(null, _subscriberConnection, messageParser);
        //    var channelName = nameof(MessageDeliveryFailure) + RandomSuffix;
        //    var testMessage = JsonConvert.SerializeObject(new { testKey = "test value's" });

        //    var messageValidator = new MessageValidator();
        //    int messagesReceivedCount = 0;
        //    var messageHandler = new MessageHandler(channelName, message =>
        //    {
        //        Interlocked.Increment(ref messagesReceivedCount);
        //    }, messageValidator);

        //    // act
        //    subscriber.Subscribe(messageHandler);

        //    var stopwatch = new Stopwatch();
        //    stopwatch.Start();
        //    publisher.Publish(channelName, testMessage);
        //    Thread.Sleep(10);
        //    Assert.Equal(1, messagesReceivedCount);

        //    subscriber.Unsubscribe(channelName); // simulation of message lost
        //    publisher.Publish(channelName, testMessage);
        //    Thread.Sleep(100);
        //    Assert.Equal(1, messagesReceivedCount);
        //    Assert.Equal(0, errorsCount);

        //    subscriber.Subscribe(messageHandler);
        //    Thread.Sleep(1000);
        //    publisher.Publish(channelName, testMessage);
        //    Thread.Sleep(1000);
        //    Assert.Equal(2, messagesReceivedCount);
        //    //Assert.Equal(1, errorsCount);

        //    // assert
        //    _output.WriteLine($"All sent={stopwatch.Elapsed}. Received messages={messagesReceivedCount}");
        //    Thread.Sleep(10);

        //    _output.WriteLine($"All received={stopwatch.Elapsed}. Received messages={messagesReceivedCount}");
        //    //Assert.Equal(1, errorsCount);
        //}

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