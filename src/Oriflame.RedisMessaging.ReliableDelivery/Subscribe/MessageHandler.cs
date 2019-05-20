using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Oriflame.RedisMessaging.ReliableDelivery.Subscribe.Validation;
using StackExchange.Redis;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    public class MessageHandler : IMessageHandler
    {
        private readonly Action<Message> _onSuccessMessage;
        private readonly IMessageValidator _messageValidator;
        private readonly IMessageLoader _messageLoader;
        private readonly ILogger<MessageHandler> _log;
        private readonly object _lock = new object();

        public RedisChannel Channel { get; }

        public DateTime LastActivityAt { get; private set; }

        public MessageHandler(
            RedisChannel channel,
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

        public void HandleMessage(Message message)
        {
            lock (_lock)
            {
                HandleMessageImpl(message);
                LastActivityAt = Now;
            }
        }

        protected virtual DateTime Now => DateTime.Now;

        public void CheckMissedMessages()
        {
            _log.LogDebug("Checking missed messages");
            var messages = GetNewestMessages();
            int messagesCount = 0;
            long firstMessageId = 0;
            long lastMessageId = 0;
            lock (_lock)
            {
                foreach (var message in messages)
                {
                    if (messagesCount == 0)
                    {
                        firstMessageId = message.Id;
                    }
                    HandleMessageImpl(message);
                    ++messagesCount;
                    lastMessageId = message.Id;
                }

                LastActivityAt = Now;
            }
            if (messagesCount > 0)
            {
                _log.LogWarning("Missed messages processed: messagesCount={messagesCount}, IDs range=<{firstMessageId}, {lastMessageId}>",
                    messagesCount, firstMessageId, lastMessageId);
            }
            else
            {
                // TODO Activity counter that will provide information about how many times this method was called
                _log.LogDebug("Checked missed messages: no messages missed found.");
            }
        }

        protected virtual IEnumerable<Message> GetNewestMessages()
        {
            var fromMessageId = _messageValidator.LastMessageId + 1;
            return _messageLoader.GetMessages(Channel, fromMessageId);
        }

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

        protected virtual void OnMissingMessages(Message currentMessage, long lastProcessedMessageId)
        {
            var lostMessages = _messageLoader.GetMessages(Channel, lastProcessedMessageId + 1, currentMessage.Id - 1);
            foreach (var lostMessage in lostMessages)
            {
                OnSuccessfulMessage(lostMessage);
            }
            OnSuccessfulMessage(currentMessage);
        }

        protected virtual void OnSuccessfulMessage(Message message)
        {
            _onSuccessMessage(message);
        }

        protected virtual void OnMessageAgain(Message message)
        {
            _log.LogWarning("Message was received again: {Message}", message);
        }

        protected virtual void OnOtherValidationResult(Message message, IMessageValidationResult validationResult)
        {
            _log.LogDebug("OtherValidationResult occured: {ValidationResult}, Message: {Message}", validationResult, message);
        }
    }
}