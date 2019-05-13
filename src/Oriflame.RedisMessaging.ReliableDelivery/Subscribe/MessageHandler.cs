using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Oriflame.RedisMessaging.ReliableDelivery.Subscribe.Validation;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    /// <inheritdoc />
    public class MessageHandler : IMessageHandler
    {
        private readonly Action<Message> _onSuccessMessage;
        private readonly IMessageValidator _messageValidator;
        private readonly IMessageLoader _messageLoader;
        private readonly ILogger<MessageHandler> _log;
        private readonly object _lock = new object();

        /// <inheritdoc />
        public string Channel { get; }

        /// <inheritdoc />
        public DateTime LastActivityAt { get; private set; }

        /// <summary>
        /// Creates a message handler
        /// </summary>
        /// <param name="channel"> name of a channel from that a message is received and processed</param>
        /// <param name="onSuccessMessage">function called when an expected message is received, e.g. its sequence number is not out of order</param>
        /// <param name="messageValidator">validator checking each message received whether it is out of order based on its sequence number</param>
        /// <param name="messageLoader">provides capability to directly get messages stored in Redis server</param>
        /// <param name="log">logger tracing internal activity of this subscriber</param>
        public MessageHandler(
            string channel,
            Action<Message> onSuccessMessage,
            IMessageValidator messageValidator,
            IMessageLoader messageLoader,
            ILogger<MessageHandler> log = null)
        {
            Channel = channel;
            _onSuccessMessage = onSuccessMessage;
            _messageValidator = messageValidator;
            _messageLoader = messageLoader;
            _log = log ?? NullLogger<MessageHandler>.Instance;
        }

        /// <inheritdoc />
        public void HandleMessage(Message message)
        {
            lock (_lock)
            {
                HandleMessageImpl(message);
                LastActivityAt = Now;
            }
        }

        /// <summary>
        /// Current date and time
        /// </summary>
        protected virtual DateTime Now => DateTime.Now;

        /// <inheritdoc />
        public void CheckMissedMessages()
        {
            var messages = GetNewestMessages();
            lock (_lock)
            {
                foreach (var message in messages)
                {
                    HandleMessageImpl(message);
                }

                LastActivityAt = Now;
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

        /// <summary>
        /// A template method responsible for processing a received message
        /// </summary>
        /// <param name="message">received message to be processed</param>
        protected virtual void HandleMessageImpl(Message message)
        {
            var validationResult = _messageValidator.Validate(message);
            switch (validationResult)
            {
                case SuccessValidationResult _:
                    OnSuccessfulMessage(message);
                    break;
                case ValidationResultForMissingMessages missingMessagesResult:
                    var lastProcessedMessageId = missingMessagesResult.LastProcessedMessageId;
                    OnMissingMessages(message, lastProcessedMessageId);
                    break;
                case AlreadyProcessedValidationResult _:
                    OnMessageAgain(message);
                    break;
                default:
                    OnOtherValidationResult(message, validationResult);
                    break;
            }
        }

        /// <summary>
        /// Invoked when a message that was received currently is out of order - its sequence
        /// number is larger that expected
        /// </summary>
        /// <param name="currentMessage">currently received message</param>
        /// <param name="lastProcessedMessageId">message ID of a message that was successfully processed last time</param>
        protected virtual void OnMissingMessages(Message currentMessage, long lastProcessedMessageId)
        {
            var lostMessages = _messageLoader.GetMessages(Channel, lastProcessedMessageId + 1, currentMessage.Id - 1);
            foreach (var lostMessage in lostMessages)
            {
                OnSuccessfulMessage(lostMessage);
            }
            OnSuccessfulMessage(currentMessage);
        }

        /// <summary>
        /// Invoked when a received message is expected (message ID is not out of order)
        /// </summary>
        /// <param name="message">currently received message</param>
        protected virtual void OnSuccessfulMessage(Message message)
        {
            _onSuccessMessage(message);
        }

        /// <summary>
        /// Invoked when a message is received that was already processed
        /// (its message ID is smaller than lastProcessedMessageId)
        /// </summary>
        /// <param name="message">currently received message</param>
        protected virtual void OnMessageAgain(Message message)
        {
            _log.LogWarning("Message was received again: {Message}", message);
        }

        /// <summary>
        /// Invoked when none of <see cref="OnSuccessfulMessage"/>, <see cref="OnMissingMessages"/>,
        /// <see cref="OnMessageAgain"/> is called.
        /// </summary>
        /// <param name="message">currently received message</param>
        /// <param name="validationResult">result from <see cref="IMessageValidator.Validate"/></param>
        protected virtual void OnOtherValidationResult(Message message, IMessageValidationResult validationResult)
        {
            _log.LogDebug("OtherValidationResult occured: {ValidationResult}, Message: {Message}", validationResult, message);
        }
    }
}