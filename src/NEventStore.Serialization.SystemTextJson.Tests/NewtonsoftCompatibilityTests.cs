using FluentAssertions;
using NEventStore.Persistence.AcceptanceTests;
using NEventStore.Persistence.AcceptanceTests.BDD;
using NewtonsoftJsonSerializer = NEventStore.Serialization.Json.JsonSerializer;

namespace NEventStore.Serialization.SystemTextJson.Tests
{
    public class when_deserializing_newtonsoft_event_messages_with_system_text_json : SpecificationBase
    {
        private readonly List<EventMessage> _messages =
        [
            new EventMessage { Body = "some value" },
            new EventMessage { Body = 42 },
            new EventMessage { Body = new SimpleMessage().Populate() },
        ];

        private List<EventMessage>? _deserialized;

        protected override void Because()
        {
            var serialized = new NewtonsoftJsonSerializer(null).Serialize(_messages);
            _deserialized = new SystemTextJson.SystemTextJsonSerializer().Deserialize<List<EventMessage>>(serialized);
        }

        [Fact]
        public void should_deserialize_complex_event_body_types()
        {
            _deserialized!.Last().Body.Should().BeOfType<SimpleMessage>();
        }
    }

    public class when_deserializing_system_text_json_event_messages_with_newtonsoft : SpecificationBase
    {
        private readonly List<EventMessage> _messages =
        [
            new EventMessage { Body = "some value" },
            new EventMessage { Body = 42 },
            new EventMessage { Body = new SimpleMessage().Populate() },
        ];

        private List<EventMessage>? _deserialized;

        protected override void Because()
        {
            var serialized = new SystemTextJson.SystemTextJsonSerializer().Serialize(_messages);
            _deserialized = new NewtonsoftJsonSerializer(null).Deserialize<List<EventMessage>>(serialized);
        }

        [Fact]
        public void should_deserialize_complex_event_body_types()
        {
            _deserialized!.Last().Body.Should().BeOfType<SimpleMessage>();
        }
    }

    public class when_deserializing_newtonsoft_snapshot_payloads_with_system_text_json : SpecificationBase
    {
        private readonly Snapshot _snapshot = new Snapshot(Guid.NewGuid().ToString(), 42, new Dictionary<string, List<int>>
        {
            ["values"] = [1, 2, 3],
        });

        private Snapshot? _deserialized;

        protected override void Because()
        {
            var serialized = new NewtonsoftJsonSerializer(null).Serialize(_snapshot);
            _deserialized = new SystemTextJson.SystemTextJsonSerializer().Deserialize<Snapshot>(serialized);
        }

        [Fact]
        public void should_deserialize_payload_type()
        {
            _deserialized!.Payload.Should().BeOfType(_snapshot.Payload.GetType());
        }

        [Fact]
        public void should_deserialize_payload_contents()
        {
            _deserialized!.Payload.Should().BeEquivalentTo(_snapshot.Payload);
        }
    }

    public class when_deserializing_system_text_json_snapshot_payloads_with_newtonsoft : SpecificationBase
    {
        private readonly Snapshot _snapshot = new Snapshot(Guid.NewGuid().ToString(), 42, new Dictionary<string, List<int>>
        {
            ["values"] = [1, 2, 3],
        });

        private Snapshot? _deserialized;

        protected override void Because()
        {
            var serialized = new SystemTextJson.SystemTextJsonSerializer().Serialize(_snapshot);
            _deserialized = new NewtonsoftJsonSerializer(null).Deserialize<Snapshot>(serialized);
        }

        [Fact]
        public void should_deserialize_payload_type()
        {
            _deserialized!.Payload.Should().BeOfType(_snapshot.Payload.GetType());
        }

        [Fact]
        public void should_deserialize_payload_contents()
        {
            _deserialized!.Payload.Should().BeEquivalentTo(_snapshot.Payload);
        }
    }
}
