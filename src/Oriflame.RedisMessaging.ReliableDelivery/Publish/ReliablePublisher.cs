using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Oriflame.RedisMessaging.ReliableDelivery.Publish
{
    /// <summary>
    /// Represents an object responsible for publishing messages via Redis channel.
    /// Additionally, during publishing, Redis extends a message being published
    /// with unique sequence number to be used later in <see cref="Oriflame.RedisMessaging.ReliableDelivery.Subscribe.ReliableSubscriber"/>
    /// that checks order of a message. This publisher also stores the message in Redis for limited time, <seealso cref="MessageExpiration"/>
    /// </summary>
    public class ReliablePublisher : IReliablePublisher
    {
        /// <summary>
        /// A character separating message sequence number and a message content being published
        /// </summary>
        public const char MessagePartSeparator = ':';
        private static readonly LuaScript _publishScript = LuaScript.Prepare(TrimScript(""
                                         + "local message = @message\n"
                                         + "local channel = @channel\n"
                                         + "local expiration = @expiration\n"
                                         + "local message_id = redis.call('INCR','ch:{'..channel..'}:id')\n"
                                         + "local msg = message_id..'" + MessagePartSeparator + "'..message\n"
                                         + "local clusterNodes = redis.pcall('CLUSTER', 'NODES')\n"
                                         + "redis.call('SET', 'ch:{'..channel..'}:'..message_id, message, 'EX', expiration)\n"
                                         // If a variable 'clusterNodes' is a string variable then command 'cluster nodes' did not fail
                                         // hence the server mode is clustered.
                                         // If it fails (only if server mode=standalone) then a variable 'clusterNodes' is an error object
                                         + "if type(clusterNodes) ~= 'string' or string.match(clusterNodes, 'myself,master') then\n"
                                         //    publishing a message only if this script is executed in master node
                                         //    We do not want the message to be published again by slave node(s).
                                         + "   redis.call('PUBLISH',channel,msg)\n"
                                         + "end\n"
                                         ));

        private readonly IConnectionMultiplexer _connectionMultiplexer;

        /// <summary>
        /// Redis database into that messages are stored
        /// </summary>
        protected virtual IDatabase Database => _connectionMultiplexer.GetDatabase();

        /// <summary>
        /// Creates a publisher responsible for reliable messages delivery
        /// </summary>
        /// <param name="connectionMultiplexer">A multiplexer providing low-level communication with Redis server</param>
        public ReliablePublisher(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        /// <summary>
        /// Time-to live (TTL) of a message stored in Redis server. Default is 10 minutes.
        /// </summary>
        public TimeSpan MessageExpiration { get; set; } = TimeSpan.FromMinutes(10);

        /// <inheritdoc />
        public void Publish(string channel, string message)
        {
            var parameters = CreateScriptParameters(channel, message);
            Database.ScriptEvaluate(_publishScript, parameters, CommandFlags.DemandMaster | CommandFlags.PreferMaster | CommandFlags.NoRedirect);
        }

        /// <inheritdoc />
        public Task PublishAsync(string channel, string message)
        {
            var parameters = CreateScriptParameters(channel, message);
            return Database.ScriptEvaluateAsync(_publishScript, parameters, CommandFlags.DemandMaster);
        }

        private object CreateScriptParameters(string channel, string message)
        {
            return new
            {
                channel = (RedisKey)channel,
                message = message,
                expiration = (int)MessageExpiration.TotalSeconds
            };
        }

        private static string TrimScript(string script)
        {
            return Regex.Replace(script, @" {2,}", "")
                .Trim('\r', '\n');
        }
    }
}