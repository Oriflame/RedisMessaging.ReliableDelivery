using System;
using System.Collections.Generic;
using System.Threading;
using Moq;
using Oriflame.RedisMessaging.ReliableDelivery.Subscribe;
using Oriflame.RedisMessaging.ReliableDelivery.Subscribe.Validation;
using StackExchange.Redis;
using Xunit;

namespace Oriflame.RedisMessaging.ReliableDelivery.Tests.Subscribe
{
    public class MessageProcessorTest
    {
        private class MessageProcessorTraceable : MessageProcessor
        {
            private readonly int _sleepMilliseconds;
            private readonly LinkedList<Message> _messagesCollector;
            private readonly LinkedList<string> _errorsCollector;
            private bool _isRunning;

            public MessageProcessorTraceable(int sleepMilliseconds, LinkedList<Message> messagesCollector, LinkedList<string> errorsCollector)
                : base(default(RedisChannel), null, null, null)
            {
                _sleepMilliseconds = sleepMilliseconds;
                _messagesCollector = messagesCollector;
                _errorsCollector = errorsCollector;
            }

            public IEnumerable<Message> NewestMessages { get; set; }

            protected override void HandleMessageImpl(Message message, string physicalOrLogicalChannel)
            {
                if (_isRunning)
                {
                    var error = $"Is running for message {message}";
                    _errorsCollector.AddLast(error);
                    return;
                }

                _isRunning = true;

                Thread.Sleep(_sleepMilliseconds * (int)message.Id);

                _messagesCollector.AddLast(message);
                _isRunning = false;
            }

            protected override IEnumerable<Message> GetNewestMessages()
            {
                return NewestMessages;
            }
        }

        [Fact]
        public void HandleMissingMessages()
        {
            // arrange
            var messageValidator = new Mock<IMessageValidator>(MockBehavior.Strict);
            var messageLoader = new Mock<IMessageLoader>(MockBehavior.Strict);
            messageValidator.Setup(_ => _.Validate(It.IsAny<Message>()))
                .Returns(new ValidationResultForMissedMessages(1));
            messageLoader.Setup(_ => _.GetMessages("test-channel", 2, 122))
                .Returns(new[] { new Message(2, "missing message") });

            // act
            var successfullyProcessedMessages = new List<Message>();
            var messageHandler = new MessageHandler((channel, msg) => successfullyProcessedMessages.Add(msg));
            var messageProcessor = new MessageProcessor("test-channel", messageValidator.Object, messageLoader.Object, messageHandler);

            var beforeTestTime = DateTime.UtcNow;
            messageProcessor.ProcessMessage(new Message(123, "message"), "test-channel");

            // assert
            Assert.True(beforeTestTime < messageProcessor.LastActivityAt);
            Assert.Equal(2, successfullyProcessedMessages.Count);
            messageValidator.Verify(_ => _.Validate(It.IsAny<Message>()), Times.Once);
            messageLoader.Verify(_ => _.GetMessages("test-channel", 2, 122), Times.Once);
        }

        [Fact]
        public void ParallelAccessHandlingMessagesAndMessage()
        {
            // arrange
            var message1 = new Message(1, "message1");
            var message2 = new Message(2, "message2");
            var message4 = new Message(4, "message4");
            var receivedMessages = new LinkedList<Message>();
            var receivedErrors = new LinkedList<string>();
            var messageProcessor = new MessageProcessorTraceable(100, receivedMessages, receivedErrors);

            // act
            var messages = new[] { message4, message2 };
            messageProcessor.NewestMessages = messages;
            ThreadPool.QueueUserWorkItem(state => messageProcessor.CheckForMissedMessages());
            Thread.Sleep(10);
            ThreadPool.QueueUserWorkItem(state => messageProcessor.ProcessMessage(message1, "test-channel"));
            Thread.Sleep(100 * (4 + 1 + 2) + 20);

            // assert
            Assert.True(0 == receivedErrors.Count, string.Join("|||", receivedErrors));
            Assert.Equal(3, receivedMessages.Count);
            Assert.Collection(
                receivedMessages,
                m => Assert.Equal(4, m.Id),
                m => Assert.Equal(2, m.Id),
                m => Assert.Equal(1, m.Id));
        }

        [Fact]
        public void ParallelAccessHandlingMessageAndMessages()
        {
            // arrange
            var message1 = new Message(1, "message1");
            var message2 = new Message(2, "message2");
            var message4 = new Message(4, "message4");
            var receivedMessages = new LinkedList<Message>();
            var receivedErrors = new LinkedList<string>();
            var messageProcessor = new MessageProcessorTraceable(
                100,
                receivedMessages,
                receivedErrors);

            // act
            var messages = new[] { message1, message2 };
            messageProcessor.NewestMessages = messages;
            ThreadPool.QueueUserWorkItem(state => messageProcessor.ProcessMessage(message4, "test-channel"));
            Thread.Sleep(1);
            ThreadPool.QueueUserWorkItem(state => messageProcessor.CheckForMissedMessages());
            Thread.Sleep(100 * (4 + 1 + 2) + 5);

            // assert
            Assert.True(0 == receivedErrors.Count, string.Join("|||", receivedErrors));
            Assert.Equal(3, receivedMessages.Count);
            Assert.Collection(
                receivedMessages,
                m => Assert.Equal(4, m.Id),
                m => Assert.Equal(1, m.Id),
                m => Assert.Equal(2, m.Id));
        }

        [Fact]
        public void ParallelAccessHandlingMessagesWithRealValidator()
        {
            // arrange
            var receivedMessages = new LinkedList<Message>();
            var messageValidator = new MessageValidator();
            var messageLoader = new Mock<IMessageLoader>(MockBehavior.Strict);
            var message1 = new Message(1, "message1");
            var message1A = new Message(1, "message1a");
            var message2 = new Message(2, "message2");

            // act
            var messageHandler = new MessageHandler((channel, msg) =>
            {
                receivedMessages.AddLast(msg);
                Thread.Sleep(10 * (int)msg.Id);
            });
            var messageProcessor = new MessageProcessor(
                "test-channel",
                messageValidator,
                messageLoader.Object,
                messageHandler);

            var messages = new[] { message1A, message2 };
            messageLoader.Setup(_ => _.GetMessages("test-channel", It.IsAny<long>(), long.MaxValue))
                .Returns(messages);
            messageProcessor.ProcessMessage(message1, "test-channel");
            ThreadPool.QueueUserWorkItem(state => messageProcessor.ProcessMessage(message1, "test-channel"));
            Thread.Sleep(1);
            ThreadPool.QueueUserWorkItem(state => messageProcessor.CheckForMissedMessages());
            Thread.Sleep(100);

            // assert
            Assert.Equal(2, receivedMessages.Count);
            Assert.Collection(
                receivedMessages,
                m =>
                {
                    Assert.Equal(1, m.Id);
                    Assert.Equal("message1", m.Content);
                },
                m => Assert.Equal(2, m.Id));
        }
    }
}