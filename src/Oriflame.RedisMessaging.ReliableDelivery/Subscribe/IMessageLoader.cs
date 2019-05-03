using System.Collections.Generic;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    public interface IMessageLoader
    {
        IEnumerable<Message> GetMessages(string channelName, long fromMessageId, long toMessageId = long.MaxValue);
    }
}