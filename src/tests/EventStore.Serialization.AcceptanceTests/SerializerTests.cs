#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Serialization.AcceptanceTests
{
	using System.Linq;
	using Machine.Specifications;
	using Persistence;

	[Subject("Serialization")]
	public class when_serializing_a_simple_message : using_serialization
	{
		static readonly SimpleMessage Message = new SimpleMessage().Populate();
		static byte[] serialized;
		static SimpleMessage deserialized;

		Establish context = () =>
			serialized = Serializer.Serialize(Message);

		Because of = () =>
			deserialized = Serializer.Deserialize(serialized) as SimpleMessage;

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
	public class when_serializing_a_commit_message : using_serialization
	{
		static readonly Commit Message = new CommitAttempt().Populate();
		static byte[] serialized;
		static Commit deserialized;

		Establish context = () =>
			serialized = Serializer.Serialize(Message);

		Because of = () =>
			deserialized = Serializer.Deserialize(serialized) as Commit;

		It should_deserialize_a_commit_which_contains_the_same_StreamId_as_the_serialized_commit = () =>
			deserialized.StreamId.ShouldEqual(Message.StreamId);

		It should_deserialize_a_commit_which_contains_the_same_CommitId_as_the_serialized_commit = () =>
			deserialized.CommitId.ShouldEqual(Message.CommitId);

		It should_deserialize_a_commit_which_contains_the_same_StreamRevision_as_the_serialized_commit = () =>
			deserialized.StreamRevision.ShouldEqual(Message.StreamRevision);

		It should_deserialize_a_commit_which_contains_the_same_CommitSequence_as_the_serialized_commit = () =>
			deserialized.CommitSequence.ShouldEqual(Message.CommitSequence);

		It should_deserialize_a_commit_which_contains_the_same_Snapshot_as_the_serialized_commit = () =>
			deserialized.Snapshot.ShouldEqual(Message.Snapshot);

		It should_deserialize_a_commit_which_contains_the_same_number_of_headers_as_the_serialized_commit = () =>
			deserialized.Headers.Count.ShouldEqual(Message.Headers.Count);

		It should_deserialize_a_commit_which_contains_the_same_headers_as_the_serialized_commit = () =>
		{
			foreach (var header in deserialized.Headers)
				header.Value.ShouldEqual(Message.Headers[header.Key]);
		};

		It should_deserialize_a_commit_which_contains_the_same_number_of_events_as_the_serialized_commit = () =>
			deserialized.Events.Count.ShouldEqual(Message.Events.Count);
	}

	public abstract class using_serialization
	{
		protected static readonly ISerialize Serializer = new SerializationFactory().Build();
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169