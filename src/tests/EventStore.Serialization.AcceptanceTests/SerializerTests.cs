#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Serialization.AcceptanceTests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;

	[Subject("Serialization")]
	public class when_serializing_a_simple_message : using_serialization
	{
		static readonly SimpleMessage Message = new SimpleMessage().Populate();
		static byte[] serialized;
		static SimpleMessage deserialized;

		Establish context = () =>
			serialized = Serializer.Serialize(Message);

		Because of = () =>
			deserialized = Serializer.Deserialize<SimpleMessage>(serialized);

		It should_deserialize_a_message_which_contains_the_same_Id_as_the_serialized_message = () =>
			deserialized.Id.ShouldEqual(Message.Id);

		It should_deserialize_a_message_which_contains_the_same_Value_as_the_serialized_message = () =>
			deserialized.Value.ShouldEqual(Message.Value);

		It should_deserialize_a_message_which_contains_the_same_Created_value_as_the_serialized_message = () =>
			deserialized.Created.ShouldEqual(Message.Created);

		It should_deserialize_a_message_which_contains_the_same_Count_as_the_serialized_message = () =>
			deserialized.Count.ShouldEqual(Message.Count);

		It should_deserialize_a_message_which_contains_the_number_of_elements_as_the_serialized_message = () =>
			deserialized.Contents.Count.ShouldEqual(Message.Contents.Count);

		It should_deserialize_a_message_which_contains_the_same_Contents_as_the_serialized_message = () =>
			deserialized.Contents.SequenceEqual(Message.Contents).ShouldBeTrue();
	}

	[Subject("Serialization")]
	public class when_serializing_a_list_of_event_messages : using_serialization
	{
		private static readonly List<EventMessage> Messages = new List<EventMessage>
		{
			new EventMessage { Body = "some value" },
			new EventMessage { Body = 42 },
			new EventMessage { Body = new SimpleMessage() }
		};
		static byte[] serialized;
		static List<EventMessage> deserialized;

		Establish context = () =>
			serialized = Serializer.Serialize(Messages);

		Because of = () =>
			deserialized = Serializer.Deserialize<List<EventMessage>>(serialized);

		It should_deserialize_the_same_number_of_event_messages_as_it_serialized = () =>
			Messages.Count.ShouldEqual(deserialized.Count);

		It should_deserialize_the_the_complex_types_within_the_event_messages = () =>
			deserialized.Last().Body.ShouldBeOfType<SimpleMessage>();
	}

	[Subject("Serialization")]
	public class when_serializing_a_list_of_commit_headers : using_serialization
	{
		private static readonly Dictionary<string, object> Headers = new Dictionary<string, object>
		{
			{ "HeaderKey", "SomeValue" },
			{ "AnotherKey", 42 },
			{ "AndAnotherKey", Guid.NewGuid() },
			{ "LastKey", new SimpleMessage() }
		};
		static byte[] serialized;
		static Dictionary<string, object> deserialized;

		Establish context = () =>
			serialized = Serializer.Serialize(Headers);

		Because of = () =>
			deserialized = Serializer.Deserialize<Dictionary<string, object>>(serialized);

		It should_deserialize_the_same_number_of_event_messages_as_it_serialized = () =>
			Headers.Count.ShouldEqual(deserialized.Count);

		It should_deserialize_the_the_complex_types_within_the_event_messages = () =>
			deserialized.Last().Value.ShouldBeOfType<SimpleMessage>();
	}

	[Subject("Serialization")]
	public class when_serializing_a_commit_message : using_serialization
	{
		static readonly Commit Message = Guid.NewGuid().BuildCommit();
		static byte[] serialized;
		static Commit deserialized;

		Establish context = () =>
			serialized = Serializer.Serialize(Message);

		Because of = () =>
			deserialized = Serializer.Deserialize<Commit>(serialized);

		It should_deserialize_a_commit_which_contains_the_same_StreamId_as_the_serialized_commit = () =>
			deserialized.StreamId.ShouldEqual(Message.StreamId);

		It should_deserialize_a_commit_which_contains_the_same_CommitId_as_the_serialized_commit = () =>
			deserialized.CommitId.ShouldEqual(Message.CommitId);

		It should_deserialize_a_commit_which_contains_the_same_StreamRevision_as_the_serialized_commit = () =>
			deserialized.StreamRevision.ShouldEqual(Message.StreamRevision);

		It should_deserialize_a_commit_which_contains_the_same_CommitSequence_as_the_serialized_commit = () =>
			deserialized.CommitSequence.ShouldEqual(Message.CommitSequence);

		It should_deserialize_a_commit_which_contains_the_same_number_of_headers_as_the_serialized_commit = () =>
			deserialized.Headers.Count.ShouldEqual(Message.Headers.Count);

		It should_deserialize_a_commit_which_contains_the_same_headers_as_the_serialized_commit = () =>
		{
			foreach (var header in deserialized.Headers)
				header.Value.ShouldEqual(Message.Headers[header.Key]);

			deserialized.Headers.Values.SequenceEqual(Message.Headers.Values);
		};

		It should_deserialize_a_commit_which_contains_the_same_number_of_events_as_the_serialized_commit = () =>
			deserialized.Events.Count.ShouldEqual(Message.Events.Count);
	}

	[Subject("Serialization")]
	public class when_serializing_an_untyped_payload_on_a_snapshot : using_serialization
	{
		static readonly IDictionary<string, List<int>> Payload = new Dictionary<string, List<int>>();
		static readonly Snapshot Snapshot = new Snapshot(Guid.NewGuid(), 42, Payload);
		static byte[] serialized;
		static Snapshot deserialized;

		Establish context = () =>
			serialized = Serializer.Serialize(Snapshot);

		Because of = () =>
			deserialized = Serializer.Deserialize<Snapshot>(serialized);

		It should_correctly_deserialize_the_untyped_payload_contents = () =>
			deserialized.Payload.ShouldEqual(Snapshot.Payload);

		It should_correctly_deserialize_the_untyped_payload_type = () =>
			deserialized.Payload.ShouldBeOfType(Snapshot.Payload.GetType());
	}

	public abstract class using_serialization
	{
		protected static readonly ISerialize Serializer = new SerializationFactory().Build();
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169