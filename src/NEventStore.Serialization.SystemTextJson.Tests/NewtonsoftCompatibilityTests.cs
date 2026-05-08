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
            _deserialized = new SystemTextJsonSerializer().Deserialize<List<EventMessage>>(serialized);
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

    public class when_deserializing_newtonsoft_headers_with_system_text_json : SpecificationBase
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
            var serialized = new NewtonsoftJsonSerializer(null).Serialize(_headers);
            _deserialized = new SystemTextJson.SystemTextJsonSerializer().Deserialize<Dictionary<string, object>>(serialized);
        }

        [Fact]
        public void should_deserialize_complex_header_types()
        {
            _deserialized!["ComplexKey"].Should().BeOfType<SimpleMessage>();
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

    public class when_roundtripping_snapshot_with_polymorphic_object_dictionary_values : SpecificationBase
    {
        private readonly Snapshot _snapshot = new Snapshot(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            1,
            new Dictionary<string, object>
            {
                ["text"] = "hello",
                ["number"] = 42L,
                ["complex"] = new SimpleMessage { Value = "test", Count = 7 },
            });

        private Snapshot? _deserialized;

        protected override void Because()
        {
            var serializer = new SystemTextJson.SystemTextJsonSerializer();
            var serialized = serializer.Serialize(_snapshot);
            _deserialized = serializer.Deserialize<Snapshot>(serialized);
        }

        [Fact]
        public void should_preserve_payload_type()
        {
            _deserialized!.Payload.Should().BeOfType<Dictionary<string, object>>();
        }

        [Fact]
        public void should_preserve_complex_value_type()
        {
            var payload = (Dictionary<string, object>)_deserialized!.Payload!;
            payload["complex"].Should().BeOfType<SimpleMessage>();
        }

        [Fact]
        public void should_preserve_complex_value_data()
        {
            var payload = (Dictionary<string, object>)_deserialized!.Payload!;
            payload["complex"].Should().BeEquivalentTo(new SimpleMessage { Value = "test", Count = 7 });
        }
    }

    public class when_deserializing_newtonsoft_snapshot_with_polymorphic_object_dictionary_values_using_system_text_json : SpecificationBase
    {
        private readonly Snapshot _snapshot = new Snapshot(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            1,
            new Dictionary<string, object>
            {
                ["text"] = "hello",
                ["complex"] = new SimpleMessage { Value = "compat", Count = 3 },
            });

        private Snapshot? _deserialized;

        protected override void Because()
        {
            var serialized = new NewtonsoftJsonSerializer(null).Serialize(_snapshot);
            _deserialized = new SystemTextJson.SystemTextJsonSerializer().Deserialize<Snapshot>(serialized);
        }

        [Fact]
        public void should_preserve_complex_value_type()
        {
            var payload = (Dictionary<string, object>)_deserialized!.Payload!;
            payload["complex"].Should().BeOfType<SimpleMessage>();
        }

        [Fact]
        public void should_preserve_complex_value_data()
        {
            var payload = (Dictionary<string, object>)_deserialized!.Payload!;
            payload["complex"].Should().BeEquivalentTo(new SimpleMessage { Value = "compat", Count = 3 });
        }
    }

    public class when_deserializing_nested_generic_type_metadata_with_mismatched_outer_assembly_version : SpecificationBase
    {
        private readonly Dictionary<string, List<int>> _payload = new()
        {
            ["values"] = [1, 2, 3],
        };

        private Dictionary<string, List<int>>? _deserialized;

        protected override void Because()
        {
            var serializer = new SystemTextJson.SystemTextJsonSerializer();
            var serializedBytes = serializer.Serialize(_payload);
            var serialized = System.Text.Encoding.UTF8.GetString(serializedBytes);
            var typeName = typeof(Dictionary<string, List<int>>).AssemblyQualifiedName!;
            serialized = serialized.Replace(typeName, GetTypeNameWithMismatchedOuterAssemblyVersion(typeName));
            _deserialized = serializer.Deserialize<Dictionary<string, List<int>>>(System.Text.Encoding.UTF8.GetBytes(serialized));
        }

        [Fact]
        public void should_deserialize_nested_generic_dictionary()
        {
            _deserialized!.Should().BeEquivalentTo(_payload);
        }

        private static string GetTypeNameWithMismatchedOuterAssemblyVersion(string typeName)
        {
            var assemblySeparatorIndex = typeName.LastIndexOf("]], ", StringComparison.Ordinal);
            if (assemblySeparatorIndex < 0)
            {
                throw new InvalidOperationException("Expected a generic assembly-qualified type name.");
            }

            var outerAssemblyName = typeof(Dictionary<string, List<int>>).Assembly.GetName().Name;
            return typeName.Substring(0, assemblySeparatorIndex + 4) + outerAssemblyName + ", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        }
    }
}
