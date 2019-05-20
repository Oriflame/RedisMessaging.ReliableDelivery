using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    /// <summary>
    /// This subscriber detects whether message was received in a correct order.
    /// Hence, it is able to detect whether some previous messages were lost.
    /// </summary>
    public class ReliableSubscriber : IReliableSubscriber
    {
        private readonly IMessageParser _messageParser;
        private readonly ILogger<ReliableSubscriber> _log;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ConcurrentDictionary<string, ChannelMessageQueue> _queues = new ConcurrentDictionary<string, ChannelMessageQueue>();

        /// <summary>
        /// Creates an object responsible for receiving messages and handling them.
        /// It is able to detect that some messages were potentially lost.
        /// </summary>
        /// <param name="connectionMultiplexer">A multiplexer providing low-level communication with Redis server</param>
        /// <param name="messageParser">an object responsible for analyzing a raw string message and parsing it to a structured <see cref="Message"/></param>
        /// <param name="log">logger tracing internal activity of this subscriber</param>
        public ReliableSubscriber(
            IConnectionMultiplexer connectionMultiplexer,
            IMessageParser messageParser,
            ILogger<ReliableSubscriber> log = null)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _messageParser = messageParser;
            _log = log ?? NullLogger<ReliableSubscriber>.Instance;
        }

        /// <summary>
        /// Raw <see cref="ISubscriber"/> providing low-level communication
        /// from/to Redis server.
        /// </summary>
        protected virtual ISubscriber Subscriber => _connectionMultiplexer.GetSubscriber();

        /// <inheritdoc />
        /// <exception cref="ArgumentException">if a handler to a same channel is already registered</exception>
        public void Subscribe(IMessageHandler messageHandler)
        {
            var channel = messageHandler.Channel;
            bool isQueueAlreadyRegistered = true;
            _queues.GetOrAdd(channel, channelName =>
            {
                var queue = Subscriber.Subscribe(channel);
                queue.OnMessage(channelMessage => HandleMessage(channelMessage.Message, messageHandler));
                isQueueAlreadyRegistered = false;
                return queue;
            });

            if (isQueueAlreadyRegistered)
            {
                throw new ArgumentException($"There already exists a handler subscribed to channel '{channel}'");
            }
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">if a handler to a same channel is already registered</exception>
        public async Task SubscribeAsync(IMessageHandler handler)
        {
            var channel = handler.Channel;
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

            queue.OnMessage(channelMessage => HandleMessage(channelMessage.Message, handler));
        }

        /// <inheritdoc />
        public void Unsubscribe(string channel)
        {
            // TODO throw exception if channel not subscribed
            if (_queues.TryRemove(channel, out var queue))
            {
                queue.Unsubscribe();
            }
        }

        /// <inheritdoc />
        public Task UnsubscribeAsync(string channel)
        {
            // TODO throw exception if channel not subscribed
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

        /// <summary>
        /// Method raised when a raw string message received is not in a valid format,
        /// e.g. it does not contain `sequence number` and `message content`
        /// </summary>
        /// <param name="rawMessage">a message sent from Redis server</param>
        /// <param name="handler">message handler processing a message if it was parsed successfully</param>
        protected virtual void OnInvalidMessageFormat(RedisValue rawMessage, IMessageHandler handler)
        {
            _log.LogWarning(
                "Invalid message format in channel '{Channel}'. rawMessage={RawMessage}",
                handler.Channel,
                rawMessage);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception">exception that was thrown while an already parsed message was in the middle of processing</param>
        /// <param name="rawMessage">a message sent from Redis server</param>
        //// <returns>true if an exception is not handled and should be rethrown</returns>
        protected virtual bool OnMessageHandlingException(Exception exception, RedisValue rawMessage)
        {
            // We intentionally log an exception here. Uncaught exception here will be blindly caught
            // by a caller in StackExchange.Redis.ChannelMessageQueue::OnMessageSyncImpl()
            const string errorMessage = "Received message processing failed";
            _log.LogError(exception, errorMessage);

            // we wish to throw an exception just for sure to a caller
            return true;
        }

        private void HandleMessage(RedisValue rawMessage, IMessageHandler handler)
        {
            try
            {
                if (!_messageParser.TryParse(rawMessage, out var parsedMessage))
                {
                    OnInvalidMessageFormat(rawMessage, handler);
                    return;
                }

                handler.HandleMessage(parsedMessage);
            }
            catch (Exception exception)
            {
                var shouldRethrowException = OnMessageHandlingException(exception, rawMessage);

                if (shouldRethrowException)
                {
                    throw;
                }
            }
        }
    }
}