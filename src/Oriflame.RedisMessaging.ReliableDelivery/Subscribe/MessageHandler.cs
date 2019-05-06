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
            }
        }

        public void CheckMissedMessages()
        {
            var messages = GetNewestMessages();
            lock (_lock)
            {
                foreach (var message in messages)
                {
                    HandleMessageImpl(message);
                }
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