using RedisMessaging.ReliableDelivery.Subscribe;
using Xunit;

namespace RedisMessaging.ReliableDelivery.Tests.Subscribe
{
    public class MessageParserTest
    {
        [Fact]
        public void ValidMessage()
        {
            // arrange&act
            var messageParser = new MessageParser();
            long messageId = long.MaxValue;
            var isValid = messageParser.TryParse($"{messageId}:message", out var parsedMessage);

            // assert
            Assert.True(isValid);
            Assert.Equal(messageId, parsedMessage.Id);
            Assert.Equal("message", parsedMessage.Content);
        }

        [Theory]
        [InlineData("message")]
        [InlineData("invalid:message")]
        public void InvalidMessage(string rawMessage)
        {
            // arrange&act
            var messageParser = new MessageParser();
            var isValid = messageParser.TryParse(rawMessage, out _);

            // assert
            Assert.False(isValid);
        }
    }
}