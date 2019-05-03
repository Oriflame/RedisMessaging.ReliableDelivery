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

        public ReliableSubscriber(
            IConnectionMultiplexer connectionMultiplexer,
            IMessageParser messageParser,
            ILogger<ReliableSubscriber> log = null)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _messageParser = messageParser;
            _log = log ?? NullLogger<ReliableSubscriber>.Instance;
        }

        protected virtual ISubscriber Subscriber => _connectionMultiplexer.GetSubscriber();

        // for testing
        internal Exception LastException { get; private set; }
        internal long ExceptionsCount { get; private set; }
        
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

        public void Unsubscribe(string channel)
        {
            if (_queues.TryRemove(channel, out var queue))
            {
                queue.Unsubscribe();
            }
        }

        public Task UnsubscribeAsync(string channel)
        {
            if (_queues.TryRemove(channel, out var queue))
            {
                return queue.UnsubscribeAsync();
            }

            return Task.CompletedTask;
        }

        public void UnsubscribeAll()
        {
            foreach (var queue in _queues)
            {
                queue.Value.Unsubscribe();
            }

            _queues.Clear();
        }

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


        protected virtual void OnInvalidMessageFormat(string rawMessage, IMessageHandler handler)
        {
            _log.LogWarning(
                "Invalid message format in channel '{Channel}'. rawMessage={RawMessage}",
                handler.Channel,
                rawMessage);
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
                LastException = null;
            }
            catch (Exception exception)
            {
                ++ExceptionsCount;
                LastException = exception;
                // We intentionally log an exception here. Uncaught exception here will be blindly caught
                // by a caller in StackExchange.Redis.ChannelMessageQueue::OnMessageSyncImpl()
                const string errorMessage = "Received message processing failed";
                _log.LogError(exception, errorMessage);

                // we throw an exception just for sure to a caller
                throw;
            }
        }
    }
}