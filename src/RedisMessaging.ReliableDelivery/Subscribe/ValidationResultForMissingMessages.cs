namespace RedisMessaging.ReliableDelivery.Subscribe
{
    public struct ValidationResultForMissingMessages : IMessageValidationResult
    {
        /// <summary>
        /// Last successfully processed message id
        /// </summary>
        public long LastProcessedMessageId { get; }

        public ValidationResultForMissingMessages(long lastProcessedMessageId)
        {
            LastProcessedMessageId = lastProcessedMessageId;
        }

        public override string ToString()
        {
            return $"MissingMessages:LastProcessedMessageId={LastProcessedMessageId}";
        }
    }
}