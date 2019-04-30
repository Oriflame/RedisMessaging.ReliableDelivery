using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RedisMessaging.ReliableDelivery.Publish;
using StackExchange.Redis;

namespace RedisMessaging.ReliableDelivery.Subscribe
{
    /// <summary>
    /// This subscriber detects whether message was received in a correct order.
    /// Hence, it is able to detect whether some previous messages were lost.
    /// </summary>
    public class ReliableSubscriber : IReliableSubscriber
    {
        private static readonly char[] Separator = { ReliablePublisher.MessagePartSeparator };
        private readonly IMessageValidator _messageValidator;
        private readonly IMessageValidationFailureHandler _messageValidationFailureHandler;
        private readonly ILogger<ReliableSubscriber> _log;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ConcurrentDictionary<string, ChannelMessageQueue> _queues = new ConcurrentDictionary<string, ChannelMessageQueue>();

        public ReliableSubscriber(
            IConnectionMultiplexer connectionMultiplexer,
            IMessageValidator messageValidator,
            IMessageValidationFailureHandler messageValidationFailureHandler,
            ILogger<ReliableSubscriber> log)
        {
            _messageValidator = messageValidator;
            _messageValidationFailureHandler = messageValidationFailureHandler;
            _log = log;
            _connectionMultiplexer = connectionMultiplexer;
        }

        protected virtual ISubscriber Subscriber => _connectionMultiplexer.GetSubscriber();

        public void Subscribe(string channel, Action<string, string> handler)
        {
            bool isQueueAlreadyRegistered = true;
            _queues.GetOrAdd(channel, channelName =>
            {
                var queue = Subscriber.Subscribe(channel);
                queue.OnMessage(channelMessage => HandleMessage(channelMessage.Channel, channelMessage.Message, handler));
                isQueueAlreadyRegistered = false;
                return queue;
            });

            if (isQueueAlreadyRegistered)
            {
                throw new ArgumentException($"There already exists a handler subscribed to channel '{channel}'");
            }
        }

        public async Task SubscribeAsync(string channel, Action<string, string> handler)
        {
            var queue = await Subscriber.SubscribeAsync(channel).ConfigureAwait(false);
            bool isQueueAlreadyRegistered = true;
            _queues.GetOrAdd(channel, channelName =>
            {
                isQueueAlreadyRegistered = false;
                return queue;
            });

            if (isQueueAlreadyRegistered)
            {
                await queue.UnsubscribeAsync();
                throw new ArgumentException($"There already exists a handler subscribed to channel '{channel}'");
            }

            queue.OnMessage(channelMessage => HandleMessage(channelMessage.Channel, channelMessage.Message, handler));
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

        private void HandleMessage(string redisChannel, RedisValue fullMessage, Action<string, string> handler)
        {
            try
            {
                if (!ValidateMessageFormat(fullMessage, out var parsedMessage))
                {
                    _messageValidationFailureHandler.OnInvalidMessageFormat(redisChannel, fullMessage);
                    return;
                }

                var (messageId, messageContent) = parsedMessage;
                if (_messageValidator.Validate(redisChannel, messageContent, messageId))
                {
                    handler(redisChannel, messageContent);
                }
            }
            catch (Exception exception)
            {
                // We intentionally log an exception here. Uncaught exception here will be blindly caught
                // by a caller in StackExchange.Redis.ChannelMessageQueue::OnMessageSyncImpl()
                const string errorMessage = "Received message processing failed";
                _log.LogError(exception, errorMessage);

                // we throw an exception just for sure to a caller
                throw;
            }
        }

        /// <param name="message">raw message delivered from Redis</param>
        /// <param name="parsedMessageParts">long - message id, string - message content</param>
        /// <returns>true when message format is valid</returns>
        protected virtual bool ValidateMessageFormat(RedisValue message, out (long, string) parsedMessageParts)
        {
            var messageParts = ((string)message).Split(Separator, 2);
            if (messageParts.Length != 2)
            {
                _log.LogError($"Message format should be 'messageId{ReliablePublisher.MessagePartSeparator}messageContent'. It contains {{0}} parts.", messageParts.Length);
                parsedMessageParts.Item1 = 0;
                parsedMessageParts.Item2 = null;
                return false;
            }

            parsedMessageParts.Item2 = messageParts[1];

            if (!long.TryParse(messageParts[0], out var messageId))
            {
                _log.LogError("MessageId should be convertible to integer (messageId length={0}).", messageParts[0].Length);
                parsedMessageParts.Item1 = 0;
                return false;
            }

            parsedMessageParts.Item1 = messageId;

            return true;
        }
    }
}