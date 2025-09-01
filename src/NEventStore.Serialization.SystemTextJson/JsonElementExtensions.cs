using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace NEventStore.Serialization.SystemTextJson;

internal static class JsonElementExtensions
{
    private static readonly ConcurrentDictionary<string, Type> TypeCache = [];
    private static readonly ConcurrentDictionary<Type, MethodInfo> MethodCache = [];
    private static readonly MethodInfo DeserializeTypedValueMethod =
        (
            (MethodCallExpression)(
                (Expression<Action>)(() => DeserializeTyped<object>(default, null!))
            )
            .Body
        )
        .Method
        .GetGenericMethodDefinition();

    public static object? DeserializeUntyped(this JsonElement jsonElement, JsonSerializerOptions options)
    {
        JsonElement typeNameElement = default;
        switch (jsonElement.ValueKind)
        {
            case JsonValueKind.Number:
                return jsonElement.GetDecimal();
            case JsonValueKind.String:
                return jsonElement.GetString();
            case JsonValueKind.True:
            case JsonValueKind.False:
                return jsonElement.GetBoolean();
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            case JsonValueKind.Array:
                return jsonElement.EnumerateArray().Select(je => je.DeserializeUntyped(options)).ToArray();
            case JsonValueKind.Object when !jsonElement.TryGetProperty(SystemTextJsonConstants.TypeDiscriminatorPropertyName, out typeNameElement):
                throw new JsonException($"Cannot deserialize '{jsonElement}' because it does not include a type name.");
            case JsonValueKind.Object:
                break;
            default:
                throw new JsonException($"Cannot deserialize '{jsonElement}' of unrecognized kind {jsonElement.ValueKind}.");
        }

        var propertyType = typeNameElement.GetString()!;
        var returnType = Type.GetType(propertyType);

        if (returnType == null && !TypeCache.TryGetValue(propertyType, out returnType))
        {
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .ToList()
                .ForEach(type => TypeCache.TryAdd(type.FullName, type));
            if (!TypeCache.TryGetValue(propertyType, out returnType))
            {
                throw new JsonException($"Cannot locate type '{propertyType}' for deserialization.");
            }
        }

        // Instantiate the top-level object
        var result = jsonElement.Deserialize(returnType, options)!;

        // then recursively populate its properties
        var resultType = result.GetType();
        foreach (var property in jsonElement.EnumerateObject())
        {
            var propertyInfo = resultType.GetProperty(property.Name);
            if (propertyInfo == null)
            {
                continue;
            }

            var deserializationMethod = MethodCache.GetOrAdd(
                propertyInfo.PropertyType,
                type => DeserializeTypedValueMethod.MakeGenericMethod(type)
            );

            var value = deserializationMethod.Invoke(null, [property.Value, options]);
            propertyInfo.SetValue(result, value);
        }

        return result;
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private static T? DeserializeTyped<T>(this JsonElement jsonElement, JsonSerializerOptions options)
    {
        return jsonElement.Deserialize<T>(options);
    }
}