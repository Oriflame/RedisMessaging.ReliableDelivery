using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Oriflame.RedisMessaging.ReliableDelivery.Subscribe.Validation;
using StackExchange.Redis;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    /// <summary>
    /// This subscriber detects whether message was received in a correct order.
    /// Hence, it is able to detect whether some previous messages were lost.
    /// </summary>
    internal class ReliableSubscriber : IReliableSubscriber
    {
        private readonly IMessageParser _messageParser;
        private readonly ILogger<ReliableSubscriber> _log;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ConcurrentDictionary<string, ChannelMessageQueue> _queues = new ConcurrentDictionary<string, ChannelMessageQueue>();

        /// <summary>
        /// Creates an object responsible for receiving messages and handling them.
        /// It is able to detect that some messages were potentially lost.
        /// </summary>
        /// <param name="connectionMultiplexer">A multiplexer providing low-level communication with Redis server</param>
        /// <param name="messageParser">an object responsible for analyzing a raw string message and parsing it to a structured <see cref="Message"/></param>
        /// <param name="loggerFactory">logger factory for creating loggers to trace internal activity of this subscriber</param>
        public ReliableSubscriber(
            IConnectionMultiplexer connectionMultiplexer,
            IMessageParser messageParser,
            ILoggerFactory loggerFactory = null)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _messageParser = messageParser;
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _log = _loggerFactory.CreateLogger<ReliableSubscriber>();
        }

        /// <summary>
        /// Raw <see cref="ISubscriber"/> providing low-level communication
        /// from/to Redis server.
        /// </summary>
        private ISubscriber Subscriber => _connectionMultiplexer.GetSubscriber();

        /// <inheritdoc />
        /// <exception cref="ArgumentException">if a handler to a same channel is already registered</exception>
        public IMessageDeliveryChecker Subscribe(string channel, IMessageHandler handler)
        {
            EnsureNotPatternBasedChannel(channel);
            MessageProcessor messageProcessor = null;
            bool isQueueAlreadyRegistered = true;
            _queues.GetOrAdd(channel, channelName =>
            {
                var queue = Subscriber.Subscribe(channel);

                messageProcessor = CreateMessageProcessor(channel, handler);
                queue.OnMessage(channelMessage => HandleMessage(channelMessage.Channel, channelMessage.Message, messageProcessor));
                isQueueAlreadyRegistered = false;
                return queue;
            });

            if (isQueueAlreadyRegistered)
            {
                throw new ArgumentException($"There already exists a handler subscribed to channel '{channel}'");
            }

            return messageProcessor;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">if a handler to a same channel is already registered</exception>
        public async Task<IMessageDeliveryChecker> SubscribeAsync(string channel, IMessageHandler handler)
        {
            EnsureNotPatternBasedChannel(channel);
            var queue = await Subscriber.SubscribeAsync(channel).ConfigureAwait(false);

            bool isQueueAlreadyRegistered = true;
            _queues.GetOrAdd(channel, channelName =>
            {
                isQueueAlreadyRegistered = false;
                return queue;
            });

            if (isQueueAlreadyRegistered)
            {
                await queue.UnsubscribeAsync().ConfigureAwait(false);
                throw new ArgumentException($"There already exists a handler subscribed to channel '{channel}'");
            }

            var messageProcessor = CreateMessageProcessor(channel, handler);
            queue.OnMessage(channelMessage => HandleMessage(channelMessage.Channel, channelMessage.Message, messageProcessor));

            return messageProcessor;
        }

        /// <inheritdoc />
        public void Unsubscribe(string channel)
        {
            if (_queues.TryRemove(channel, out var queue))
            {
                queue.Unsubscribe();
            }
        }

        /// <inheritdoc />
        public Task UnsubscribeAsync(string channel)
        {
            if (_queues.TryRemove(channel, out var queue))
            {
                return queue.UnsubscribeAsync();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void UnsubscribeAll()
        {
            foreach (var queue in _queues)
            {
                queue.Value.Unsubscribe();
            }

            _queues.Clear();
        }

        /// <inheritdoc />
        public Task UnsubscribeAllAsync()
        {
            var tasks = new List<Task>();
            foreach (var queue in _queues)
            {
                var task = queue.Value.UnsubscribeAsync();
                tasks.Add(task);
            }
            _queues.Clear();

            return Task.WhenAll(tasks);
        }

        protected virtual void HandleMessage(string physicalChannel, RedisValue rawMessage, IMessageProcessor processor)
        {
            try
            {
                if (!_messageParser.TryParse(rawMessage, out var parsedMessage))
                {
                    OnInvalidMessageFormat(physicalChannel, rawMessage);
                    return;
                }

                processor.ProcessMessage(parsedMessage, physicalChannel);
            }
            catch (Exception exception)
            {
                var shouldRethrowException = OnMessageHandlingException(exception);

                if (shouldRethrowException)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Called when an exception occurs during processing a received message.
        /// </summary>
        /// <param name="exception">exception that was thrown while an already parsed message was in the middle of processing</param>
        //// <returns>true if an exception is not handled and should be rethrown</returns>
        protected virtual bool OnMessageHandlingException(Exception exception)
        {
            // We intentionally log an exception here. Uncaught exception here will be blindly caught
            // by a caller in StackExchange.Redis.ChannelMessageQueue::OnMessageSyncImpl()
            const string errorMessage = "Received message processing failed";
            _log.LogError(exception, errorMessage);

            // we wish to throw an exception just for sure to a caller
            return true;
        }

        /// <summary>
        /// Method raised when a raw string message received is not in a valid format,
        /// e.g. it does not contain `sequence number` and `message content`
        /// </summary>
        /// <param name="physicalChannel">name of a channel from which a message was received</param>
        /// <param name="rawMessage">a message sent from Redis server</param>
        private void OnInvalidMessageFormat(string physicalChannel, RedisValue rawMessage)
        {
            _log.LogWarning(
                "Invalid message format in channel '{Channel}'. rawMessage={RawMessage}",
                physicalChannel,
                rawMessage);
        }

        private MessageProcessor CreateMessageProcessor(string channel, IMessageHandler messageHandler)
        {
            var messageValidator = new MessageValidator();
            var messageLoader = new MessageLoader(_connectionMultiplexer);
            return new MessageProcessor(channel, messageValidator, messageLoader, messageHandler, _loggerFactory.CreateLogger<MessageProcessor>());
        }

        private static void EnsureNotPatternBasedChannel(string channel)
        {
            if (!channel.Contains("*"))
            {
                return;
            }

            throw new NotSupportedException("Subscribing to a pattern-based channel (channel name contains '*') is not supported");
        }
    }
}