using System.Text.Json;

namespace NEventStore.Serialization.SystemTextJson.Converters;

/// <summary>
/// Converts a <see cref="Dictionary{string,object}"/> from - <b>but not to</b> - JSON
/// <br/>
/// <br/>
/// <b>This converter should only be used for deserializing into <see cref="Dictionary{string,object}"/>
/// instances, not for serializing from them, as the built-in converter already handles serialization correctly</b>
/// </summary>
/// <remarks>
/// System.Text.Json intentionally makes as few assumptions as possible about the contents of a
/// <see cref="Dictionary{string,object}"/>, even when there is type information included in the
/// serialized JSON, so we have to handle it explicitly
/// </remarks>
public class DictionaryOfStringAndObjectJsonConverter : NullSafeTypedJsonConverter<Dictionary<string, object?>>
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(Dictionary<string, object>)
               || typeToConvert == typeof(Dictionary<string, object?>);
    }

    /// <inheritdoc />
    protected override Dictionary<string, object?> DeserializeFrom(JsonElement jsonElement, JsonSerializerOptions options)
    {
        return jsonElement.EnumerateObject()
            .Where(item => !item.NameEquals(SystemTextJsonConstants.TypeDiscriminatorPropertyName))
            .ToDictionary(item => item.Name, item => item.Value.DeserializeUntyped(options));
    }

    /// <summary>
    /// This method is minimally implemented because the standard System.Text.Json serialization code handles
    /// serialization of <see cref="Dictionary{string,object}"/> correctly - only the deserialization operation
    /// needs to be overridden
    /// <br/>
    /// <br/>
    /// Invoking this method will result in a performance hit because of the cast to
    /// <see cref="IDictionary{string,object}"/> (which is required to prevent an infinite recursive loop)
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, Dictionary<string, object?> value, JsonSerializerOptions options)
    {
        // casting avoids infinite recursive loop: https://github.com/dotnet/docs/issues/19268
        JsonSerializer.Serialize<IDictionary<string, object?>>(writer, value, options);
    }

    /// <summary>
    /// This is not implemented because <see cref="Write"/> overrides the base class implementation
    /// </summary>
    /// <param name="value"></param>
    /// <param name="writer"></param>
    /// <param name="options"></param>
    /// <exception cref="NotImplementedException"></exception>
    protected override void SerializeFrom(Dictionary<string, object?> value, Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}