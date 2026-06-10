using NEventStore.Persistence.AcceptanceTests;
using NEventStore.Persistence.AcceptanceTests.BDD;
using FluentAssertions;
#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if NUNIT
#endif
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

#pragma warning disable 169 // ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable S101 // Types should be named in PascalCase

namespace NEventStore.Serialization.AcceptanceTests
{
#if MSTEST
    [TestClass]
#endif
    public class when_serializing_a_simple_message : SerializationConcern
    {
        private readonly SimpleMessage _message = new SimpleMessage().Populate();
        private SimpleMessage? _deserialized;
        private byte[]? _serialized;

        protected override void Context()
        {
            _serialized = Serializer.Serialize(_message);
        }

        protected override void Because()
        {
            _deserialized = Serializer.Deserialize<SimpleMessage>(_serialized!);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Id_as_the_serialized_message()
        {
            _deserialized!.Id.Should().Be(_message.Id);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Value_as_the_serialized_message()
        {
            _deserialized!.Value.Should().Be(_message.Value);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Created_value_as_the_serialized_message()
        {
            _deserialized!.Created.Should().Be(_message.Created);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Count_as_the_serialized_message()
        {
            _deserialized!.Count.Should().Be(_message.Count);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_number_of_elements_as_the_serialized_message()
        {
            _deserialized!.Contents.Count.Should().Be(_message.Contents.Count);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Contents_as_the_serialized_message()
        {
            _deserialized!.Contents.SequenceEqual(_message.Contents).Should().BeTrue();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_serializing_a_list_of_event_messages : SerializationConcern
    {
        private readonly List<EventMessage> Messages = new List<EventMessage>
        {
            new EventMessage {Body = "some value"},
            new EventMessage {Body = 42},
            new EventMessage {Body = new SimpleMessage()}
        };

        private List<EventMessage>? _deserialized;
        private byte[]? _serialized;

        protected override void Context()
        {
            _serialized = Serializer.Serialize(Messages);
        }

        protected override void Because()
        {
            _deserialized = Serializer.Deserialize<List<EventMessage>>(_serialized!);
        }

        [Fact]
        public void should_deserialize_the_same_number_of_event_messages_as_it_serialized()
        {
            Messages.Count.Should().Be(_deserialized!.Count);
        }

        [Fact]
        public void should_deserialize_the_complex_types_within_the_event_messages()
        {
            _deserialized!.Last().Body.Should().BeOfType<SimpleMessage>();
        }

        [Fact]
        public void should_deserialize_event_messages_with_a_non_null_headers_dictionary()
        {
            _deserialized!.Should().OnlyContain(message => message.Headers != null);
        }

        [Fact]
        public void should_deserialize_event_messages_with_empty_headers_when_none_were_serialized()
        {
            _deserialized!.Should().OnlyContain(message => message.Headers.Count == 0);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_serializing_an_event_message_with_headers : SerializationConcern
    {
        private readonly EventMessage _message = new EventMessage { Body = "some value" };
        private EventMessage? _deserialized;
        private byte[]? _serialized;

        protected override void Context()
        {
            _message.Headers["HeaderKey"] = "SomeValue";
            _message.Headers["AnotherHeader"] = "AnotherValue";
            _serialized = Serializer.Serialize(_message);
        }

        protected override void Because()
        {
            _deserialized = Serializer.Deserialize<EventMessage>(_serialized!);
        }

        [Fact]
        public void should_deserialize_the_headers_dictionary()
        {
            _deserialized!.Headers.Should().NotBeNull();
        }

        [Fact]
        public void should_deserialize_the_same_number_of_headers()
        {
            _deserialized!.Headers.Count.Should().Be(_message.Headers.Count);
        }

        [Fact]
        public void should_deserialize_string_header_values()
        {
            _deserialized!.Headers["HeaderKey"].Should().Be("SomeValue");
            _deserialized.Headers["AnotherHeader"].Should().Be("AnotherValue");
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_serializing_a_list_of_commit_headers : SerializationConcern
    {
        private readonly Dictionary<string, object> _headers = new Dictionary<string, object>
        {
            {"HeaderKey", "SomeValue"},
            {"AnotherKey", 42},
            {"AndAnotherKey", Guid.NewGuid()},
            {"LastKey", new SimpleMessage()}
        };

        private Dictionary<string, object>? _deserialized;
        private byte[]? _serialized;

        protected override void Context()
        {
            _serialized = Serializer.Serialize(_headers);
        }

        protected override void Because()
        {
            _deserialized = Serializer.Deserialize<Dictionary<string, object>>(_serialized!);
        }

        [Fact]
        public void should_deserialize_the_same_number_of_event_messages_as_it_serialized()
        {
            _headers.Count.Should().Be(_deserialized!.Count);
        }

        [Fact]
        public void should_deserialize_the_the_complex_types_within_the_event_messages()
        {
            _deserialized!.Last().Value.Should().BeOfType<SimpleMessage>();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_serializing_an_untyped_payload_on_a_snapshot : SerializationConcern
    {
        private Snapshot? _deserialized;
        private IDictionary<string, List<int>>? _payload;
        private byte[]? _serialized;
        private Snapshot? _snapshot;

        protected override void Context()
        {
            _payload = new Dictionary<string, List<int>>();
            _snapshot = new Snapshot(Guid.NewGuid().ToString(), 42, _payload);
            _serialized = Serializer.Serialize(_snapshot);
        }

        protected override void Because()
        {
            _deserialized = Serializer.Deserialize<Snapshot>(_serialized!);
        }

        [Fact]
        public void should_correctly_deserialize_the_untyped_payload_contents()
        {
            _deserialized!.Payload.Should().BeEquivalentTo(_snapshot!.Payload);
        }

        [Fact]
        public void should_correctly_deserialize_the_untyped_payload_type()
        {
            _deserialized!.Payload.Should().BeOfType(_snapshot!.Payload.GetType());
        }
    }

    public abstract class SerializationConcern : SpecificationBase
    {
        private readonly SerializerFixture _data;

        public ISerialize Serializer
        {
            get { return _data.Serializer; }
        }

        protected SerializationConcern()
        {
            _data = new SerializerFixture();
        }
    }

    public partial class SerializerFixture
    {
        private readonly Func<ISerialize> _createSerializer;
        private ISerialize? _serializer;

        public ISerialize Serializer
        {
            get { return _serializer ??= _createSerializer(); }
        }
    }
}

#pragma warning restore S101 // Types should be named in PascalCase
#pragma warning restore 169 // ReSharper disable InconsistentNaming
#pragma warning restore IDE1006 // Naming Styles
