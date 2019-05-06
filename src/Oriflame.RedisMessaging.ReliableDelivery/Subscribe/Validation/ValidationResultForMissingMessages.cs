using System;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe.Validation
{
    public readonly struct ValidationResultForMissingMessages : IMessageValidationResult, IEquatable<ValidationResultForMissingMessages>
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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ValidationResultForMissingMessages other && Equals(other);
        }

        public bool Equals(ValidationResultForMissingMessages other)
        {
            return LastProcessedMessageId == other.LastProcessedMessageId;
        }

        public override int GetHashCode()
        {
            return LastProcessedMessageId.GetHashCode();
        }

        public static bool operator ==(ValidationResultForMissingMessages left, ValidationResultForMissingMessages right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ValidationResultForMissingMessages left, ValidationResultForMissingMessages right)
        {
            return !left.Equals(right);
        }
    }
}