using System.Collections.Generic;
using StackExchange.Redis;

namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe
{
    /// <inheritdoc />
    public class MessageLoader : IMessageLoader
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        private static readonly LuaScript _getMessagesScript = LuaScript.Prepare("" +
                                                                                 "local channel = @channel\n" +
                                                                                 "local fromId = @fromId\n" +
                                                                                 "local toId = @toId\n" +
                                                                                 "local lastMessageId = redis.call('GET', 'ch:{'..channel..'}:id')\n" +
                                                                                 "if lastMessageId == false then\n" + // channel was not initialized, hence no messages created yet
                                                                                 "   return {}\n" +
                                                                                 "end\n" +
                                                                                 "toId = math.min(toId, lastMessageId)\n" +
                                                                                 "local result = {}\n" +
                                                                                 "for id = fromId, toId, 1\n" +
                                                                                 "do\n" +
                                                                                 "    local messageContent = redis.call('GET', 'ch:{'..channel..'}:'..id)\n" +
                                                                                 "    if messageContent ~= false then\n" +
                                                                                 "        table.insert(result, id)\n" +
                                                                                 "        table.insert(result, messageContent)\n" +
                                                                                 "    end\n" +
                                                                                 "end\n" +
                                                                                 "return result");

        /// <summary>
        /// Creates a <see cref="MessageLoader"/> to get data from Redis server
        /// </summary>
        /// <param name="connectionMultiplexer">A multiplexer providing low-level communication with Redis server</param>
        public MessageLoader(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        /// <inheritdoc />
        public IEnumerable<Message> GetMessages(string channelName, long fromMessageId, long toMessageId = long.MaxValue)
        {
            // TODO get messages in batches

            object parameters = new
            {
                channel = (RedisKey) channelName,
                fromId = fromMessageId,
                toId = toMessageId
            };
            var results = (RedisResult[]) _connectionMultiplexer.GetDatabase().ScriptEvaluate(_getMessagesScript, parameters);

            var i = 0;
            while (i < results.Length)
            {
                var id = (long) results[i++];
                var content = (string) results[i++];

                yield return new Message(id, content);
            }
        }
    }
}