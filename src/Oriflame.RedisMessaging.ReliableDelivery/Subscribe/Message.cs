using System;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    /// <summary>
    /// A POCO representing a parsed message
    /// </summary>
    public readonly struct Message : IEquatable<Message>
    {
        /// <summary>
        /// Message ID. It should always be greater than zero.
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// Message data
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Creates an immutable message
        /// </summary>
        /// <param name="id">message ID</param>
        /// <param name="content">message data</param>
        public Message(long id, string content)
        {
            Id = id;
            Content = content;
        }

        /// <summary>
        /// Default message without message ID and message content initialized
        /// </summary>
        public static Message Undefined { get; } = default;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Message:Id={Id}";
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Message other && Equals(other);
        }

        /// <summary>
        /// Compares two messages based on their ID and content
        /// </summary>
        /// <param name="other">a message to be compared with this object</param>
        /// <returns></returns>
        public bool Equals(Message other)
        {
            return Id == other.Id && string.Equals(Content, other.Content);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Id.GetHashCode() * 397) ^ (Content?.GetHashCode() ?? 0);
            }
        }

        /// <summary>
        /// Compares two messages based on their ID and content
        /// </summary>
        /// <param name="left">first message</param>
        /// <param name="right">second message</param>
        /// <returns>true if messages are equal</returns>
        /// <see cref="Equals(Message)"/>
        public static bool operator ==(Message left, Message right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two messages based on their ID and content
        /// </summary>
        /// <param name="left">first message</param>
        /// <param name="right">second message</param>
        /// <returns>true if messages are not equal</returns>
        /// <see cref="Equals(Message)"/>
        public static bool operator !=(Message left, Message right)
        {
            return !left.Equals(right);
        }
    }
}
