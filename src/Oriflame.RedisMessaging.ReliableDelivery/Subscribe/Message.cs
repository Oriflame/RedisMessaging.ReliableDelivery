using System;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    public readonly struct Message : IEquatable<Message>
    {
        public long Id { get; }
        public string Content { get; }

        public Message(long id, string content)
        {
            Id = id;
            Content = content;
        }

        public static Message Undefined { get; } = default(Message);

        public override string ToString()
        {
            return $"Message:Id={Id}";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Message other && Equals(other);
        }

        public bool Equals(Message other)
        {
            return Id == other.Id && string.Equals(Content, other.Content, StringComparison.InvariantCulture);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id.GetHashCode() * 397) ^ (Content?.GetHashCode() ?? 0);
            }
        }

        public static bool operator ==(Message left, Message right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Message left, Message right)
        {
            return !left.Equals(right);
        }
    }
}
