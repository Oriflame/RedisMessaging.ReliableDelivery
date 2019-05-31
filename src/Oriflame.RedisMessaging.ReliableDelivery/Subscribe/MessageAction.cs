namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    /// <summary>
    /// Action to be called when a message is being processed
    /// </summary>
    /// <param name="physicalOrLogicalChannel">name of a channel from which a message was received</param>
    /// <param name="message">A parsed message</param>
    public delegate void MessageAction(string physicalOrLogicalChannel, Message message);

    /// <summary>
    /// Action to be called when a message list is handled
    /// </summary>
    /// <param name="physicalOrLogicalChannel">name of a channel from which a message was received</param>
    /// <param name="messagesCount">count of messages in a list</param>
    public delegate void MessagesCountAction(string physicalOrLogicalChannel, long messagesCount);
}