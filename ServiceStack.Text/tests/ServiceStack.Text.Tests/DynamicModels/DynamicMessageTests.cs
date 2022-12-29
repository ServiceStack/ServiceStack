using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.DynamicModels
{
    public class DynamicMessage : IMessageHeaders
    {
        public Guid Id { get; set; }
        public string ReplyTo { get; set; }
        public int Priority { get; set; }
        public int RetryAttempts { get; set; }
        public object Body { get; set; }

        public Type Type { get; set; }
        public object GetBody()
        {
            //When deserialized this.Body is a string so use the serilaized 
            //this.Type to deserialize it back into the original type
            return this.Body is string
            ? TypeSerializer.DeserializeFromString((string)this.Body, this.Type)
            : this.Body;
        }
    }

    public class GenericMessage<T> : IMessageHeaders
    {
        public Guid Id { get; set; }
        public string ReplyTo { get; set; }
        public int Priority { get; set; }
        public int RetryAttempts { get; set; }
        public T Body { get; set; }
    }

    public class StrictMessage : IMessageHeaders
    {
        public Guid Id { get; set; }
        public string ReplyTo { get; set; }
        public int Priority { get; set; }
        public int RetryAttempts { get; set; }
        public MessageBody Body { get; set; }
    }

    [RuntimeSerializable]
    public class MessageBody
    {
        public MessageBody()
        {
            this.Arguments = new List<string>();
        }

        public string Action { get; set; }
        public List<string> Arguments { get; set; }
    }

    /// Common interface not required, used only to simplify validation
    public interface IMessageHeaders
    {
        Guid Id { get; set; }
        string ReplyTo { get; set; }
        int Priority { get; set; }
        int RetryAttempts { get; set; }
    }

    [TestFixture]
    public class DynamicMessageTests
    {
        [Test]
        public void Object_Set_To_Object_Test()
        {
            var original = new DynamicMessage
            {
                Id = Guid.NewGuid(),
                Priority = 3,
                ReplyTo = "http://path/to/reply.svc",
                RetryAttempts = 1,
                Type = typeof(MessageBody),
                Body = new Object()
            };

            var jsv = TypeSerializer.SerializeToString(original);
            var json = JsonSerializer.SerializeToString(original);
            var jsvDynamicType = TypeSerializer.DeserializeFromString<DynamicMessage>(jsv);
            var jsonDynamicType = JsonSerializer.DeserializeFromString<DynamicMessage>(json);

            AssertHeadersAreEqual(jsvDynamicType, original);
            AssertHeadersAreEqual(jsonDynamicType, original);
            AssertHeadersAreEqual(jsvDynamicType, jsonDynamicType);
        }

        [Test]
        public void Can_deserialize_between_dynamic_generic_and_strict_messages()
        {
            var original = new DynamicMessage
            {
                Id = Guid.NewGuid(),
                Priority = 3,
                ReplyTo = "http://path/to/reply.svc",
                RetryAttempts = 1,
                Type = typeof(MessageBody),
                Body = new MessageBody
                {
                    Action = "Alphabet",
                    Arguments = { "a", "b", "c" }
                }
            };

            var jsv = TypeSerializer.SerializeToString(original);
            var dynamicType = TypeSerializer.DeserializeFromString<DynamicMessage>(jsv);
            var genericType = TypeSerializer.DeserializeFromString<GenericMessage<MessageBody>>(jsv);
            var strictType = TypeSerializer.DeserializeFromString<StrictMessage>(jsv);

            AssertHeadersAreEqual(dynamicType, original);
            AssertBodyIsEqual(dynamicType.GetBody(), (MessageBody)original.Body);

            AssertHeadersAreEqual(genericType, original);
            AssertBodyIsEqual(genericType.Body, (MessageBody)original.Body);

            AssertHeadersAreEqual(strictType, original);
            AssertBodyIsEqual(strictType.Body, (MessageBody)original.Body);

            //Debug purposes
            Console.WriteLine(strictType.Dump());
            /*
			 {
				Id: 891653ea2d0a4626ab0623fc2dc9dce1,
				ReplyTo: http://path/to/reply.svc,
				Priority: 3,
				RetryAttempts: 1,
				Body: 
				{
					Action: Alphabet,
					Arguments: 
					[
						a,
						b,
						c
					]
				}
			}
			*/
        }

        public void AssertHeadersAreEqual(IMessageHeaders actual, IMessageHeaders expected)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.ReplyTo, Is.EqualTo(expected.ReplyTo));
            Assert.That(actual.Priority, Is.EqualTo(expected.Priority));
            Assert.That(actual.RetryAttempts, Is.EqualTo(expected.RetryAttempts));
        }

        public void AssertBodyIsEqual(object actual, MessageBody expected)
        {
            var actualBody = actual as MessageBody;
            Assert.That(actualBody, Is.Not.Null);
            Assert.That(actualBody.Action, Is.EqualTo(expected.Action));
            Assert.That(actualBody.Arguments, Is.EquivalentTo(expected.Arguments));
        }
    }
}