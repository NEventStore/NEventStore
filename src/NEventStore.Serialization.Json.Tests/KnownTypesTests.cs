using FluentAssertions;
using NEventStore.Persistence.AcceptanceTests;
using NEventStore.Persistence.AcceptanceTests.BDD;
using Newtonsoft.Json;
using SerializerUnderTest = NEventStore.Serialization.Json.JsonSerializer;
#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

#pragma warning disable IDE1006 // Naming Styles

namespace NEventStore.Serialization.Json.Tests
{
#if MSTEST
    [TestClass]
#endif
    public class when_using_the_default_json_known_types
    {
        [Fact]
        public void should_keep_event_message_lists_on_the_untyped_serializer_path()
        {
            var serializer = new InspectableJsonSerializer(null);

            serializer.GetTypeNameHandlingFor(typeof(List<EventMessage>)).Should().Be(TypeNameHandling.Auto);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_using_an_empty_json_known_type_override
    {
        [Fact]
        public void should_keep_the_default_known_types()
        {
            var serializer = new InspectableJsonSerializer(null, []);

            serializer.GetTypeNameHandlingFor(typeof(List<EventMessage>)).Should().Be(TypeNameHandling.Auto);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_using_a_custom_json_known_type_override
    {
        [Fact]
        public void should_use_the_custom_known_type_set()
        {
            var serializer = new InspectableJsonSerializer(null, typeof(SimpleMessage));

            serializer.GetTypeNameHandlingFor(typeof(SimpleMessage)).Should().Be(TypeNameHandling.Auto);
            serializer.GetTypeNameHandlingFor(typeof(List<EventMessage>)).Should().Be(TypeNameHandling.All);
        }
    }

    internal sealed class InspectableJsonSerializer(JsonSerializerSettings? settings, params Type[]? knownTypes)
        : SerializerUnderTest(settings, knownTypes)
    {
        public TypeNameHandling GetTypeNameHandlingFor(Type type)
        {
            return GetSerializer(type).TypeNameHandling;
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles
