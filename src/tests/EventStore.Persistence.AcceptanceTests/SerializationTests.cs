using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Persistence.AcceptanceTests;
using EventStore.Persistence.AcceptanceTests.BDD;
using Xunit;
using Xunit.Should;

namespace EventStore.Serialization.AcceptanceTests
{
    public class when_serializing_a_simple_message : SerializationConcern
	{
		readonly SimpleMessage Message = new SimpleMessage().Populate();
		byte[] serialized;
		SimpleMessage deserialized;

        protected override void Context()
        {
            serialized = Serializer.Serialize(Message);
        }

        protected override void Because()
        {
            deserialized = Serializer.Deserialize<SimpleMessage>(serialized);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Id_as_the_serialized_message()
        {
			deserialized.Id.ShouldBe(Message.Id);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Value_as_the_serialized_message()
        {
			deserialized.Value.ShouldBe(Message.Value);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Created_value_as_the_serialized_message()
        {
			deserialized.Created.ShouldBe(Message.Created);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Count_as_the_serialized_message()
        {
			deserialized.Count.ShouldBe(Message.Count);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_number_of_elements_as_the_serialized_message()
        {
			deserialized.Contents.Count.ShouldBe(Message.Contents.Count);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Contents_as_the_serialized_message()
        {
			deserialized.Contents.SequenceEqual(Message.Contents).ShouldBeTrue();
        }
	}

	public class when_serializing_a_list_of_event_messages : SerializationConcern
	{
		private readonly List<EventMessage> Messages = new List<EventMessage>
		{
			new EventMessage { Body = "some value" },
			new EventMessage { Body = 42 },
			new EventMessage { Body = new SimpleMessage() }
		};
		byte[] serialized;
		List<EventMessage> deserialized;

	    protected override void Context()
	    {
	        serialized = Serializer.Serialize(Messages);
	    }
        
	    protected override void Because()
		{
		    deserialized = Serializer.Deserialize<List<EventMessage>>(serialized);
		}

        [Fact]
        public void should_deserialize_the_same_number_of_event_messages_as_it_serialized()
		{
		    Messages.Count.ShouldBe(deserialized.Count);
		}

        [Fact]
        public void should_deserialize_the_the_complex_types_within_the_event_messages()
		{
		    deserialized.Last().Body.ShouldBeInstanceOf<SimpleMessage>();
		}
	}

	public class when_serializing_a_list_of_commit_headers : SerializationConcern
	{
		private readonly Dictionary<string, object> Headers = new Dictionary<string, object>
		{
			{ "HeaderKey", "SomeValue" },
			{ "AnotherKey", 42 },
			{ "AndAnotherKey", Guid.NewGuid() },
			{ "LastKey", new SimpleMessage() }
		};
		byte[] serialized;
		Dictionary<string, object> deserialized;

        protected override void Context()
	    {
	        serialized = Serializer.Serialize(Headers);
	    }

        protected override void Because()
		{
		    deserialized = Serializer.Deserialize<Dictionary<string, object>>(serialized);
		}

        [Fact]
        public void should_deserialize_the_same_number_of_event_messages_as_it_serialized()
		{
		    Headers.Count.ShouldBe(deserialized.Count);
		}

        [Fact]
        public void should_deserialize_the_the_complex_types_within_the_event_messages()
		{
		    deserialized.Last().Value.ShouldBeInstanceOf<SimpleMessage>();
		}
	}

	public class when_serializing_a_commit_message : SerializationConcern
	{
		readonly Commit Message = Guid.NewGuid().BuildCommit();
		byte[] serialized;
		Commit deserialized;

		protected override void Context()
	    {
	        serialized = Serializer.Serialize(Message);
	    }

	    protected override void Because()
		{
		    deserialized = Serializer.Deserialize<Commit>(serialized);
		}

        [Fact]
        public void should_deserialize_a_commit_which_contains_the_same_StreamId_as_the_serialized_commit()
		{
		    deserialized.StreamId.ShouldBe(Message.StreamId);
		}

        [Fact]
        public void should_deserialize_a_commit_which_contains_the_same_CommitId_as_the_serialized_commit()
		{
		    deserialized.CommitId.ShouldBe(Message.CommitId);
		}

        [Fact]
        public void should_deserialize_a_commit_which_contains_the_same_StreamRevision_as_the_serialized_commit()
		{
		    deserialized.StreamRevision.ShouldBe(Message.StreamRevision);
		}

        [Fact]
        public void should_deserialize_a_commit_which_contains_the_same_CommitSequence_as_the_serialized_commit()
		{
		    deserialized.CommitSequence.ShouldBe(Message.CommitSequence);
		}

        [Fact]
        public void should_deserialize_a_commit_which_contains_the_same_number_of_headers_as_the_serialized_commit()
		{
		    deserialized.Headers.Count.ShouldBe(Message.Headers.Count);
		}

        [Fact]
		public void should_deserialize_a_commit_which_contains_the_same_headers_as_the_serialized_commit()
		{
			foreach (var header in deserialized.Headers)
				header.Value.ShouldBe(Message.Headers[header.Key]);

			deserialized.Headers.Values.SequenceEqual(Message.Headers.Values);
		}

        [Fact]
        public void should_deserialize_a_commit_which_contains_the_same_number_of_events_as_the_serialized_commit()
		{
		    deserialized.Events.Count.ShouldBe(Message.Events.Count);
		}
	}

	public class when_serializing_an_untyped_payload_on_a_snapshot : SerializationConcern
	{
		IDictionary<string, List<int>> payload;
	    Snapshot snapshot;
		byte[] serialized;
		Snapshot deserialized;

		protected override void Context()
	    {
            payload = new Dictionary<string, List<int>>();
            snapshot = new Snapshot(Guid.NewGuid(), 42, payload);
	        serialized = Serializer.Serialize(snapshot);
	    }
        
        protected override void Because()
        {
            deserialized = Serializer.Deserialize<Snapshot>(serialized);
        }

        [Fact]
        public void should_correctly_deserialize_the_untyped_payload_contents()
        {
            deserialized.Payload.ShouldBe(snapshot.Payload);
        }

        [Fact]
        public void should_correctly_deserialize_the_untyped_payload_type()
        {
            deserialized.Payload.ShouldBeInstanceOf(snapshot.Payload.GetType());
        }
    }


    public class SerializationConcern : SpecificationBase, IUseFixture<SerializerFixture>
    {
        SerializerFixture data;

        public ISerialize Serializer { get { return data.Serializer; } }

        public virtual int ConfiguredPageSizeForTesting
        {
            get { return int.Parse("pageSize".GetSetting() ?? "0"); }
        }

        public void SetFixture(SerializerFixture data)
        {
            this.data = data;
        }
    }

    public partial class SerializerFixture
    {
        ISerialize serializer;
        Func<ISerialize> createSerializer;

        public ISerialize Serializer
        {
            get { return serializer ?? (serializer = createSerializer()); }
        }
    }
}