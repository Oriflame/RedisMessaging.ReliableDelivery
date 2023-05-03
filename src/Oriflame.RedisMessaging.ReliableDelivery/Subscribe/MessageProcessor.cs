using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Oriflame.RedisMessaging.ReliableDelivery.Subscribe.Validation;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    /// <inheritdoc cref="IMessageDeliveryChecker" />
    internal class MessageProcessor : IMessageProcessor, IMessageDeliveryChecker
    {
        private readonly TimeSpan _pubSubReceivedHandledThreshold = TimeSpan.FromSeconds(2);
        private readonly IMessageValidator _messageValidator;
        private readonly IMessageLoader _messageLoader;
        private readonly IMessageHandler _messageHandler;
        private readonly ILogger<MessageProcessor> _log;
        private readonly object _lock = new object();

        private string Channel { get; }

        /// <inheritdoc />
        public DateTime LastActivityAt { get; private set; }

        private DateTime LastPubSubMessageAt { get; set; }

        /// <summary>
        /// Creates a message handler
        /// </summary>
        /// <param name="channel"> name of a channel from that a message is received and processed</param>
        /// <param name="messageValidator">validator checking each message received whether it is out of order based on its sequence number</param>
        /// <param name="messageLoader">provides capability to directly get messages stored in Redis server</param>
        /// <param name="messageHandler">a handler responsible for processing a message received</param>
        /// <param name="log">logger tracing internal activity of this subscriber</param>
        /// <param name="pubSubReceivedHandledThreshold">Threshold for difference of message received time and message handled time (used for logging)</param>
        public MessageProcessor(
            string channel,
            IMessageValidator messageValidator,
            IMessageLoader messageLoader,
            IMessageHandler messageHandler,
            ILogger<MessageProcessor> log = default,
            TimeSpan? pubSubReceivedHandledThreshold = null)
        {
            Channel = channel;
            _messageValidator = messageValidator;
            _messageLoader = messageLoader;
            _messageHandler = messageHandler;
            _log = log ?? NullLogger<MessageProcessor>.Instance;
            _pubSubReceivedHandledThreshold = pubSubReceivedHandledThreshold ?? TimeSpan.FromSeconds(2);
        }

        /// <inheritdoc />
        public void CheckForMissedMessages()
        {
            var channel = Channel;
            _log.LogDebug("Checking missed messages in channel '{channel}'", channel);
            var messages = GetNewestMessages();
            int messagesCount = 0;
            long firstMessageId = 0;
            long lastMessageId = 0;
            TimeSpan lastPubSubMessageBefore;
            lock (_lock)
            {
                foreach (var message in messages)
                {
                    HandleMessageImpl(message, channel, isBulkProcessing: true, out var validationResult);
                    if (validationResult is AlreadyProcessedValidationResult)
                    {
                        // the message has been received from PubSub channel already => not count on that
                        continue;
                    }

                    if (messagesCount == 0)
                    {
                        firstMessageId = message.Id;
                    }
                    lastMessageId = message.Id;
                    ++messagesCount;
                }

                LastActivityAt = Now;
                lastPubSubMessageBefore = LastActivityAt - LastPubSubMessageAt;
            }
            if (messagesCount > 0)
            {
                _log.LogWarning("Missed messages in channel '{channel}' processed: messagesCount={messagesCount}, " +
                    "IDs range=<{firstMessageId}, {lastMessageId}>, last PubSub message before={lastPubSubMessageBefore}",
                    channel, messagesCount, firstMessageId, lastMessageId, lastPubSubMessageBefore);
            }
            else
            {
                // TODO Activity counter that will provide information about how many times this method was called
                _log.LogDebug("Checked missed messages: no messages missed found in channel '{channel}'.", channel);
            }
        }

        /// <summary>
        /// Gets messages form Redis server that were not yet received
        /// via Redis subscriber
        /// </summary>
        /// <returns>collection of messages not yet processed by this <see cref="IMessageHandler"/>></returns>
        protected virtual IEnumerable<Message> GetNewestMessages()
        {
            var fromMessageId = _messageValidator.LastMessageId + 1;
            return _messageLoader.GetMessages(Channel, fromMessageId);
        }

        public void ProcessMessage(Message message, string physicalChannel)
        {
            var messageReceivedAt = Now;
            lock (_lock)
            {
                var messageReceivedHandledDiff = Now - messageReceivedAt;
                if (messageReceivedHandledDiff > _pubSubReceivedHandledThreshold)
                {
                    _log.LogWarning("Message handling in channel '{channel}' was delayed: messageId={MessageId}, delay={messageReceivedHandledDiff}",
                        physicalChannel, message.Id, messageReceivedHandledDiff);
                }

                LastPubSubMessageAt = Now;
                HandleMessageImpl(message, physicalChannel, isBulkProcessing: false, out _);
                LastActivityAt = Now;
            }
        }

        /// <summary>
        /// Current date and time
        /// </summary>
        protected virtual DateTime Now => DateTime.UtcNow;

        /// <summary>
        /// A template method responsible for processing a received message
        /// </summary>
        /// <param name="message">received message to be processed</param>
        /// <param name="physicalOrLogicalChannel"> name of a channel from that a message is received and processed</param>
        /// <param name="isBulkProcessing"><c>True</c> if message was received from bulk of messages</param>
        /// <param name="validationResult">Message validation result</param>
        protected virtual void HandleMessageImpl(Message message, string physicalOrLogicalChannel, bool isBulkProcessing, out IMessageValidationResult validationResult)
        {
            validationResult = _messageValidator.Validate(message);
            switch (validationResult)
            {
                case SuccessValidationResult _:
                    OnExpectedMessage(message, physicalOrLogicalChannel);
                    break;
                case ValidationResultForMissedMessages missedMessagesResult:
                    var lastProcessedMessageId = missedMessagesResult.LastProcessedMessageId;
                    OnMissedMessages(message, lastProcessedMessageId, physicalOrLogicalChannel);
                    OnExpectedMessage(message, physicalOrLogicalChannel);
                    break;
                case AlreadyProcessedValidationResult _:
                    OnDuplicatedMessage(message, physicalOrLogicalChannel, isBulkProcessing);
                    break;
                default:
                    OnOtherValidationResult(message, validationResult, physicalOrLogicalChannel);
                    break;
            }
        }

        /// <summary>
        /// Invoked when a message that was received currently is out of order - its sequence
        /// number is larger that expected
        /// </summary>
        /// <param name="currentMessage">currently received message</param>
        /// <param name="lastProcessedMessageId">message ID of a message that was successfully processed last time</param>
        /// <param name="physicalOrLogicalChannel"> name of a channel from that a message is received and processed</param>
        protected virtual void OnMissedMessages(Message currentMessage, long lastProcessedMessageId, string physicalOrLogicalChannel)
        {
            var fromMessageId = lastProcessedMessageId + 1;
            var toMessageId = currentMessage.Id - 1;
            _log.LogWarning("Missed messages in channel '{channel}' detected: IDs range=<{fromMessageId}, {toMessageId}>",
                physicalOrLogicalChannel,
                fromMessageId,
                toMessageId);
            var missedMessages = _messageLoader.GetMessages(Channel, fromMessageId, toMessageId);
            var messagesCount = 0;
            foreach (var missedMessage in missedMessages)
            {
                ++messagesCount;
                _messageHandler.OnMissedMessage(physicalOrLogicalChannel, missedMessage);
            }

            var expectedMessagesCount = toMessageId - fromMessageId + 1;
            if (expectedMessagesCount > messagesCount)
            {
                var missingMessagesCount = expectedMessagesCount - messagesCount;
                _log.LogError(
                    "It was not possible to get {MissingMessages} missed messages from expected {ExpectedMessages} messages in channel '{channel}'.",
                    missingMessagesCount,
                    expectedMessagesCount,
                    physicalOrLogicalChannel);
                _messageHandler.OnMissingMessages(physicalOrLogicalChannel, missingMessagesCount);
            }
        }

        /// <summary>
        /// Invoked when a received message is expected (message ID is not out of order)
        /// </summary>
        /// <param name="message">currently received message</param>
        /// <param name="physicalOrLogicalChannel"> name of a channel from that a message is received and processed</param>
        protected virtual void OnExpectedMessage(Message message, string physicalOrLogicalChannel)
        {
            _messageHandler.OnExpectedMessage(physicalOrLogicalChannel, message);
        }

        /// <summary>
        /// Invoked when a message is received that was already processed
        /// (its message ID is smaller than lastProcessedMessageId)
        /// </summary>
        /// <param name="message">currently received message</param>
        /// <param name="physicalOrLogicalChannel"> name of a channel from that a message is received and processed</param>
        /// <param name="isBulkProcessing"><c>True</c> if message was received from bulk of messages</param>
        protected virtual void OnDuplicatedMessage(Message message, string physicalOrLogicalChannel, bool isBulkProcessing)
        {
            _messageHandler.OnDuplicatedMessage(physicalOrLogicalChannel, message);
            if (!isBulkProcessing)
            {
                // message was received again and came from PubSub => PubSub was late
                _log.LogDebug("Message in channel '{channel}' was received again: messageId={MessageId}", physicalOrLogicalChannel, message.Id);
            }
        }

        /// <summary>
        /// Invoked when none of <see cref="OnExpectedMessage"/>, <see cref="OnMissedMessages"/>,
        /// <see cref="OnDuplicatedMessage"/> is called.
        /// </summary>
        /// <param name="message">currently received message</param>
        /// <param name="validationResult">result from <see cref="IMessageValidator.Validate"/></param>
        /// <param name="physicalOrLogicalChannel"> name of a channel from that a message is received and processed</param>
        protected virtual void OnOtherValidationResult(Message message, IMessageValidationResult validationResult, string physicalOrLogicalChannel)
        {
            _log.LogDebug("OtherValidationResult occured in channel '{channel}': {ValidationResult}, messageId={MessageId}",
                physicalOrLogicalChannel,
                validationResult,
                message.Id);
        }
    }
}