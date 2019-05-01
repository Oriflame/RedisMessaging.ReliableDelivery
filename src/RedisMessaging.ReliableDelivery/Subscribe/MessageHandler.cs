using System;
using StackExchange.Redis;

namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public class MessageHandler : IMessageHandler
    {
        private readonly Action<string> _successMessageHandler;
        private readonly IMessageValidator _messageValidator;

        public RedisChannel Channel { get; }

        public MessageHandler(
            RedisChannel channel,
            Action<string> successMessageHandler,
            IMessageValidator messageValidator)
        {
            _successMessageHandler = successMessageHandler;
            _messageValidator = messageValidator;
            Channel = channel;
        }

        protected virtual void HandleMissingMessages(string currentMessageContent, long currentMessageId, long lastProcessedMessageId)
        {
            throw new NotImplementedException();
        }

        protected virtual void HandleSuccessfulMessage(string messageContent, long messageId)
        {
            _successMessageHandler(messageContent);
        }

        protected virtual void HandleOtherValidation(IMessageValidationResult validationResult)
        {
        }

        public void HandleMessage(long messageId, string messageContent)
        {
            var validationResult = _messageValidator.Validate(messageContent, messageId);
            switch (validationResult)
            {
                case var success when MessageValidationResult.Success.Equals(success):
                    HandleSuccessfulMessage(messageContent, messageId);
                    break;
                case ValidationResultForMissingMessages missingMessagesResult:
                    var lastProcessedMessageId = missingMessagesResult.LastProcessedMessageId;
                    HandleMissingMessages(messageContent, messageId, lastProcessedMessageId);
                    break;
                default:
                    HandleOtherValidation(validationResult);
                    break;
            }
        }
    }

    public interface IMessageHandler
    {
        RedisChannel Channel { get; }

        void HandleMessage(long messageId, string messageContent);
    }
}