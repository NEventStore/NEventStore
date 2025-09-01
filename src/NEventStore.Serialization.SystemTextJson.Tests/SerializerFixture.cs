// ReSharper disable CheckNamespace

using System.Text.Json;
using System.Text.Json.Serialization;
using NEventStore.Persistence.AcceptanceTests;
using NEventStore.Serialization.SystemTextJson;
using NEventStore.Serialization.SystemTextJson.Converters;

namespace NEventStore.Serialization.AcceptanceTests
// ReSharper restore CheckNamespace
{
    public partial class SerializerFixture
    {
        public SerializerFixture()
        {
            _createSerializer = () => new SystemTextJsonSerializer(
                new JsonSerializerOptions
                {
                    Converters = { new SimpleMessageConverter() }
                }
            );
        }

        /// <summary>
        /// Converter for <see cref="SimpleMessage"/> instances
        /// </summary>
        /// <remarks>
        /// System.Text.Json expects public setters for serialized properties by default, and does not take advantage
        /// of the fact that a collection can be populated through the <see cref="ICollection{T}.Add(T)"/> method or
        /// similar APIs. Because <see cref="SimpleMessage.Contents"/> has a private setter, this converter is a
        /// required component for ensuring the list is populated.
        /// </remarks>
        private class SimpleMessageConverter : NullSafeTypedJsonConverter<SimpleMessage>
        {
            protected override SimpleMessage DeserializeFrom(JsonElement jsonElement, JsonSerializerOptions options)
            {
                var result = new SimpleMessage
                {
                    Id = jsonElement.GetProperty(nameof(SimpleMessage.Id)).Deserialize<Guid>(),
                    Created = jsonElement.GetProperty(nameof(SimpleMessage.Created)).Deserialize<DateTime>(),
                    Value = jsonElement.GetProperty(nameof(SimpleMessage.Value)).Deserialize<string>()!,
                    Count = jsonElement.GetProperty(nameof(SimpleMessage.Count)).Deserialize<int>(),
                };
                var contents = jsonElement.GetProperty(nameof(SimpleMessage.Contents)).Deserialize<List<string>>();
                result.Contents.AddRange(contents!);
                return result;
            }

            protected override void SerializeFrom(SimpleMessage value, Utf8JsonWriter writer, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString(SystemTextJsonConstants.TypeDiscriminatorPropertyName, typeof(SimpleMessage).AssemblyQualifiedName);
                writer.WriteString(nameof(SimpleMessage.Id), value.Id);
                writer.WriteString(nameof(SimpleMessage.Created), value.Created);
                writer.WriteString(nameof(SimpleMessage.Value),  value.Value);
                writer.WriteNumber(nameof(SimpleMessage.Count),  value.Count);
                writer.WritePropertyName(nameof(SimpleMessage.Contents));
                writer.WriteRawValue(JsonSerializer.Serialize(value.Contents, value.Contents.GetType(), options));
                writer.WriteEndObject();
            }
        }
    }
}