using System.Text.Json;

namespace NEventStore.Serialization.SystemTextJson.Converters;

/// <summary>
/// Converts a <see cref="Snapshot"/> to or from JSON
/// </summary>
public class SnapshotJsonConverter : NullSafeTypedJsonConverter<Snapshot>
{
    /// <inheritdoc />
    protected override Snapshot DeserializeFrom(JsonElement jsonElement, JsonSerializerOptions options)
    {
        return new Snapshot(
            jsonElement.GetProperty(nameof(Snapshot.BucketId)).GetString()!,
            jsonElement.GetProperty(nameof(Snapshot.StreamId)).GetString()!,
            jsonElement.GetProperty(nameof(Snapshot.StreamRevision)).GetInt32(),
            jsonElement.GetProperty(nameof(Snapshot.Payload)).DeserializeUntyped(options)!
        );
    }

    /// <inheritdoc />
    protected override void SerializeFrom(Snapshot value, Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(SystemTextJsonConstants.TypeDiscriminatorPropertyName);
        writer.WriteStringValue(typeof(Snapshot).AssemblyQualifiedName);
        writer.WriteString(nameof(Snapshot.BucketId), value.BucketId);
        writer.WriteString(nameof(Snapshot.StreamId), value.StreamId);
        writer.WriteNumber(nameof(Snapshot.StreamRevision), value.StreamRevision);
        writer.WritePropertyName(nameof(Snapshot.Payload));
        writer.WriteRawValue(JsonSerializer.Serialize(value.Payload, value.Payload.GetType(), options));
        writer.WriteEndObject();
    }
}