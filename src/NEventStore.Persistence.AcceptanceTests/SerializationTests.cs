namespace NEventStore.Serialization.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using Xunit.Should;

    public class when_serializing_a_simple_message : SerializationConcern
    {
        private readonly SimpleMessage _message = new SimpleMessage().Populate();
        private SimpleMessage _deserialized;
        private byte[] _serialized;

        protected override void Context()
        {
            _serialized = Serializer.Serialize(_message);
        }

        protected override void Because()
        {
            _deserialized = Serializer.Deserialize<SimpleMessage>(_serialized);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Id_as_the_serialized_message()
        {
            _deserialized.Id.ShouldBe(_message.Id);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Value_as_the_serialized_message()
        {
            _deserialized.Value.ShouldBe(_message.Value);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Created_value_as_the_serialized_message()
        {
            _deserialized.Created.ShouldBe(_message.Created);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Count_as_the_serialized_message()
        {
            _deserialized.Count.ShouldBe(_message.Count);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_number_of_elements_as_the_serialized_message()
        {
            _deserialized.Contents.Count.ShouldBe(_message.Contents.Count);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Contents_as_the_serialized_message()
        {
            _deserialized.Contents.SequenceEqual(_message.Contents).ShouldBeTrue();
        }
    }

    public class when_serializing_a_list_of_event_messages : SerializationConcern
    {
        private readonly List<EventMessage> Messages = new List<EventMessage>
        {
            new EventMessage {Body = "some value"},
            new EventMessage {Body = 42},
            new EventMessage {Body = new SimpleMessage()}
        };

        private List<EventMessage> _deserialized;
        private byte[] _serialized;

        protected override void Context()
        {
            _serialized = Serializer.Serialize(Messages);
        }

        protected override void Because()
        {
            _deserialized = Serializer.Deserialize<List<EventMessage>>(_serialized);
        }

        [Fact]
        public void should_deserialize_the_same_number_of_event_messages_as_it_serialized()
        {
            Messages.Count.ShouldBe(_deserialized.Count);
        }

        [Fact]
        public void should_deserialize_the_the_complex_types_within_the_event_messages()
        {
            _deserialized.Last().Body.ShouldBeInstanceOf<SimpleMessage>();
        }
    }

    public class when_serializing_a_list_of_commit_headers : SerializationConcern
    {
        private readonly Dictionary<string, object> _headers = new Dictionary<string, object>
        {
            {"HeaderKey", "SomeValue"},
            {"AnotherKey", 42},
            {"AndAnotherKey", Guid.NewGuid()},
            {"LastKey", new SimpleMessage()}
        };

        private Dictionary<string, object> _deserialized;
        private byte[] _serialized;

        protected override void Context()
        {
            _serialized = Serializer.Serialize(_headers);
        }

        protected override void Because()
        {
            _deserialized = Serializer.Deserialize<Dictionary<string, object>>(_serialized);
        }

        [Fact]
        public void should_deserialize_the_same_number_of_event_messages_as_it_serialized()
        {
            _headers.Count.ShouldBe(_deserialized.Count);
        }

        [Fact]
        public void should_deserialize_the_the_complex_types_within_the_event_messages()
        {
            _deserialized.Last().Value.ShouldBeInstanceOf<SimpleMessage>();
        }
    }

    public class when_serializing_a_commit_message : SerializationConcern
    {
        private readonly Commit _message = Guid.NewGuid().BuildCommit();
        private Commit _deserialized;
        private byte[] _serialized;

        protected override void Context()
        {
            _serialized = Serializer.Serialize(_message);
        }

        protected override void Because()
        {
            _deserialized = Serializer.Deserialize<Commit>(_serialized);
        }

        [Fact]
        public void should_deserialize_a_commit_which_contains_the_same_StreamId_as_the_serialized_commit()
        {
            _deserialized.StreamId.ShouldBe(_message.StreamId);
        }

        [Fact]
        public void should_deserialize_a_commit_which_contains_the_same_CommitId_as_the_serialized_commit()
        {
            _deserialized.CommitId.ShouldBe(_message.CommitId);
        }

        [Fact]
        public void should_deserialize_a_commit_which_contains_the_same_StreamRevision_as_the_serialized_commit()
        {
            _deserialized.StreamRevision.ShouldBe(_message.StreamRevision);
        }

        [Fact]
        public void should_deserialize_a_commit_which_contains_the_same_CommitSequence_as_the_serialized_commit()
        {
            _deserialized.CommitSequence.ShouldBe(_message.CommitSequence);
        }

        [Fact]
        public void should_deserialize_a_commit_which_contains_the_same_number_of_headers_as_the_serialized_commit()
        {
            _deserialized.Headers.Count.ShouldBe(_message.Headers.Count);
        }

        [Fact]
        public void should_deserialize_a_commit_which_contains_the_same_headers_as_the_serialized_commit()
        {
            foreach (var header in _deserialized.Headers)
            {
                header.Value.ShouldBe(_message.Headers[header.Key]);
            }

            _deserialized.Headers.Values.SequenceEqual(_message.Headers.Values);
        }

        [Fact]
        public void should_deserialize_a_commit_which_contains_the_same_number_of_events_as_the_serialized_commit()
        {
            _deserialized.Events.Count.ShouldBe(_message.Events.Count);
        }
    }

    public class when_serializing_an_untyped_payload_on_a_snapshot : SerializationConcern
    {
        private Snapshot deserialized;
        private IDictionary<string, List<int>> payload;
        private byte[] serialized;
        private Snapshot snapshot;

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
        private SerializerFixture data;

        public ISerialize Serializer
        {
            get { return data.Serializer; }
        }

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
        private readonly Func<ISerialize> createSerializer;
        private ISerialize serializer;

        public ISerialize Serializer
        {
            get { return serializer ?? (serializer = createSerializer()); }
        }
    }
}