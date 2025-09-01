using System.Text.Json;
using System.Text.Json.Serialization;

namespace NEventStore.Serialization.SystemTextJson.Converters;

/// <summary>
/// A convenience class that handles the case where a custom converter receives JSON that indicates a
/// <see langword="null"/> or undefined value
/// </summary>
/// <typeparam name="T">The type this Converter handles</typeparam>
public abstract class NullSafeTypedJsonConverter<T> : JsonConverter<T>
{
    /// <inheritdoc />
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

        return jsonElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            ? default
            : DeserializeFrom(jsonElement, options);
    }

    /// <summary>
    /// This method is called after it has been determined that the JSON being deserialized does not
    /// indicate a <see langword="null"/> or undefined value
    /// </summary>
    /// <param name="jsonElement">The <see cref="JsonElement"/> that was deserialized from the provided JSON</param>
    /// <param name="options"></param>
    /// <returns></returns>
    protected abstract T DeserializeFrom(JsonElement jsonElement, JsonSerializerOptions options);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            SerializeFrom(value, writer, options);
        }
    }

    /// <summary>
    /// This method is called after it has been determined that the object being serialized is not <see langword="null"/>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="writer"></param>
    /// <param name="options"></param>
    protected abstract void SerializeFrom(T value, Utf8JsonWriter writer, JsonSerializerOptions options);
}