using System;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public class MessageHandler : IMessageHandler
    {
        private readonly Action<Message> _onSuccessMessage;
        private readonly IMessageValidator _messageValidator;
        private readonly IMessageLoader _messageLoader;
        private readonly ILogger<MessageHandler> _log;

        public RedisChannel Channel { get; }

        public MessageHandler(
            RedisChannel channel,
            Action<Message> onSuccessMessage,
            IMessageValidator messageValidator,
            IMessageLoader messageLoader,
            ILogger<MessageHandler> log)
        {
            Channel = channel;
            _onSuccessMessage = onSuccessMessage;
            _messageValidator = messageValidator;
            _messageLoader = messageLoader;
            _log = log;
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

        protected virtual void OnOtherValidationResult(IMessageValidationResult validationResult)
        {
            _log.LogDebug("OtherValidationResult occured: {ValidationResult}", validationResult);
        }

        public void HandleMessage(Message message)
        {
            var validationResult = _messageValidator.Validate(message);
            switch (validationResult)
            {
                case var result when MessageValidationResult.Success.Equals(result):
                    OnSuccessfulMessage(message);
                    break;
                case ValidationResultForMissingMessages missingMessagesResult:
                    var lastProcessedMessageId = missingMessagesResult.LastProcessedMessageId;
                    OnMissingMessages(message, lastProcessedMessageId);
                    break;
                default:
                    OnOtherValidationResult(validationResult);
                    break;
            }
        }
    }
}