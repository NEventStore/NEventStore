using FluentAssertions;
using NEventStore.Persistence.AcceptanceTests;
using NEventStore.Persistence.AcceptanceTests.BDD;
using NEventStore.Serialization.SystemTextJson;

namespace NEventStore.Serialization.Json.Tests
{
    public class when_newtonsoft_deserializes_system_text_json_event_messages : SpecificationBase
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
            var serialized = new SystemTextJsonSerializer().Serialize(_messages);
            _deserialized = new JsonSerializer(null).Deserialize<List<EventMessage>>(serialized);
        }

        [Fact]
        public void should_deserialize_complex_event_body_types()
        {
            _deserialized!.Last().Body.Should().BeOfType<SimpleMessage>();
        }
    }

    public class when_newtonsoft_deserializes_system_text_json_headers : SpecificationBase
    {
        private readonly Dictionary<string, object> _headers = new()
        {
            ["HeaderKey"] = "SomeValue",
            ["NumericKey"] = 42,
            ["ComplexKey"] = new SimpleMessage().Populate(),
        };

        private Dictionary<string, object>? _deserialized;

        protected override void Because()
        {
            var serialized = new SystemTextJsonSerializer().Serialize(_headers);
            _deserialized = new JsonSerializer(null).Deserialize<Dictionary<string, object>>(serialized);
        }

        [Fact]
        public void should_deserialize_complex_header_types()
        {
            _deserialized!["ComplexKey"].Should().BeOfType<SimpleMessage>();
        }
    }

    public class when_newtonsoft_deserializes_system_text_json_snapshot_payloads : SpecificationBase
    {
        private readonly Snapshot _snapshot = new Snapshot(Guid.NewGuid().ToString(), 42, new Dictionary<string, List<int>>
        {
            ["values"] = [1, 2, 3],
        });

        private Snapshot? _deserialized;

        protected override void Because()
        {
            var serialized = new SystemTextJsonSerializer().Serialize(_snapshot);
            _deserialized = new JsonSerializer(null).Deserialize<Snapshot>(serialized);
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
