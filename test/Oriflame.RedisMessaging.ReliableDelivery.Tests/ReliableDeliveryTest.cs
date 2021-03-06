﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Oriflame.RedisMessaging.ReliableDelivery.Publish;
using Oriflame.RedisMessaging.ReliableDelivery.Subscribe;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace Oriflame.RedisMessaging.ReliableDelivery.Tests
{
    [Trait("Category", "Integration test")]
    public class ReliableDeliveryTest : IClassFixture<RedisFixture>
    {
        private class TogglingSubscriber : ReliableSubscriber
        {
            public TogglingSubscriber(
                IConnectionMultiplexer connectionMultiplexer,
                IMessageParser messageParser) : base(connectionMultiplexer, messageParser)
            {
            }

            public bool IsEnabled { get; set; } = true;

            protected override void HandleMessage(string physicalChannel, RedisValue rawMessage, IMessageProcessor processor)
            {
                if (IsEnabled)
                {
                    base.HandleMessage(physicalChannel, rawMessage, processor);
                }
            }
        }

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

        private static string RandomSuffix => DateTime.Now.Ticks.ToString();

        [Fact]
        public void SubscribeLoadTest()
        {
            // arrange
            var publisher = new ReliablePublisher(_redis.GetConnection());
            var messageParser = new MessageParser();
            var subscriber = new ReliableSubscriber(_subscriberConnection, messageParser);
            var channelName = nameof(SubscribeLoadTest) + RandomSuffix;
            var testMessage = JsonConvert.SerializeObject(new { myKey = "test value's" });

            int messagesReceivedCount = 0;

            var messageHandler = new MessageHandler((channel, message) =>
            {
                Interlocked.Increment(ref messagesReceivedCount);
            });

            // act
            subscriber.Subscribe(channelName, messageHandler);

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

        [Fact]
        public void GetSavedMessages()
        {
            // arrange
            var connectionMultiplexer = _redis.GetConnection();
            var publisher = new ReliablePublisher(connectionMultiplexer);
            var loader = new MessageLoader(connectionMultiplexer);

            // act
            const string channelName = "test-channel-" + nameof(GetSavedMessages);
            publisher.Publish(channelName, "message1");
            publisher.Publish(channelName, "message2");
            var savedMessages = loader.GetMessages(channelName, 0)
                .ToList();

            // assert
            Assert.Equal(2, savedMessages.Count);
        }

        [Fact]
        public void TestMissingMessages()
        {
            // arrange
            var connectionMultiplexer = _redis.GetConnection();
            var publisher = new ReliablePublisher(connectionMultiplexer);
            var subscriber = new TogglingSubscriber(connectionMultiplexer, new MessageParser());
            int receivedMessagesCount = 0;
            const string channelName = "test-channel-" + nameof(TestMissingMessages);
            var messageHandler = new MessageHandler((channel, msg) => Interlocked.Increment(ref receivedMessagesCount));
            subscriber.Subscribe(channelName, messageHandler);

            // act
            publisher.Publish(channelName, "message1");
            Wait(10);
            subscriber.IsEnabled = false; // simulation of disability to process messages
            publisher.Publish(channelName, "message2");
            Wait(10);
            subscriber.IsEnabled = true;
            publisher.Publish(channelName, "message3");
            Wait(10);

            // assert
            Assert.Equal(3, receivedMessagesCount);
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
                Wait(10);
                elapsedMilliseconds += 10;
            }
        }

        private static void Wait(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
    }
}