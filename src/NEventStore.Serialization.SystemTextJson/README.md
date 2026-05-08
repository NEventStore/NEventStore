# NEventStore.Serialization.SystemTextJson

`NEventStore.Serialization.SystemTextJson` is the System.Text.Json serializer for NEventStore. It is implemented as a drop-in JSON replacement for `NEventStore.Serialization.Json`, including Newtonsoft-compatible type metadata for polymorphic NEventStore payloads.

## Wireup

```csharp
var store = Wireup.Init()
    .UsingInMemoryPersistence()
    .UsingJsonSerialization()
    .Build();
```

Custom System.Text.Json options can be supplied, and root known types can be overridden:

```csharp
var serializer = new SystemTextJsonSerializer(
    new JsonSerializerOptions { WriteIndented = true },
    typeof(List<EventMessage>),
    typeof(Dictionary<string, object>));
```

The default known types are:

- `List<EventMessage>`
- `Dictionary<string, object>`

Passing `null` or an empty known-type array keeps those defaults. Passing a non-empty array replaces them.

## Implementation Model

System.Text.Json does not support Newtonsoft.Json `TypeNameHandling`. This serializer implements the NEventStore-compatible subset explicitly:

- Known root types are serialized with normal System.Text.Json object handling and no root `$type`.
- Other root types are wrapped with a `$type` property before their regular JSON properties.
- Polymorphic `object` values are handled by a custom `JsonConverter<object>`.
- `Snapshot` is handled by a custom converter because its key properties have private setters and its payload is declared as `object`.
- `JsonObjectCreationHandling.Populate` is enabled so get-only collection properties, such as `SimpleMessage.Contents`, round-trip like Newtonsoft.

The serializer copies user-provided `JsonSerializerOptions` before adding its compatibility converters. It always sets `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` to match the Newtonsoft serializer's null omission behavior.

## Compatibility Layer

The compatibility layer writes and reads Newtonsoft-style metadata:

- `$type` stores an assembly-qualified CLR type name.
- `$values` stores array or collection contents when the typed value is represented as a metadata object.
- Primitive `object` values are written as JSON primitives without `$type`, matching Newtonsoft behavior for strings and numbers.
- Complex `object` values are written with `$type` metadata.
- Typed dictionaries are normalized on read so Newtonsoft payloads such as `{ "$type": "...List...", "$values": [...] }` can be deserialized by System.Text.Json into the declared dictionary value type.

The type resolver first uses `Type.GetType`. If the assembly-qualified name cannot be resolved directly, it falls back to loaded assemblies by full type name. That keeps deserialization tolerant of normal assembly-load ordering.

The serializers are wire-compatible; they are not required to produce byte-for-byte identical JSON when static property types already provide enough type information. For example, System.Text.Json can write a typed dictionary value list as a normal JSON array while still reading Newtonsoft's `$type` plus `$values` representation for the same list.

## Serialized Data Examples

Assembly versions are shortened in these examples. Real payloads contain full assembly-qualified names.

### Event Messages

Source object:

```csharp
var messages = new List<EventMessage>
{
    new EventMessage { Body = "some value" },
    new EventMessage { Body = 42 },
    new EventMessage { Body = new SimpleMessage { Count = 1234, Value = "Hello" } }
};
```

JSON shape:

```json
[
  {
    "Headers": {},
    "Body": "some value"
  },
  {
    "Headers": {},
    "Body": 42
  },
  {
    "Headers": {},
    "Body": {
      "$type": "NEventStore.Persistence.AcceptanceTests.SimpleMessage, NEventStore.Persistence.AcceptanceTests",
      "Id": "00000000-0000-0000-0000-000000000000",
      "Created": "0001-01-01T00:00:00",
      "Value": "Hello",
      "Count": 1234,
      "Contents": []
    }
  }
]
```

`List<EventMessage>` is a known root type, so the root array has no `$type`. The complex event body is declared as `object`, so the compatibility converter writes `$type` metadata.

### Commit Headers

Source object:

```csharp
var headers = new Dictionary<string, object>
{
    ["HeaderKey"] = "SomeValue",
    ["NumericKey"] = 42,
    ["ComplexKey"] = new SimpleMessage { Count = 1234 }
};
```

JSON shape:

```json
{
  "HeaderKey": "SomeValue",
  "NumericKey": 42,
  "ComplexKey": {
    "$type": "NEventStore.Persistence.AcceptanceTests.SimpleMessage, NEventStore.Persistence.AcceptanceTests",
    "Id": "00000000-0000-0000-0000-000000000000",
    "Created": "0001-01-01T00:00:00",
    "Count": 1234,
    "Contents": []
  }
}
```

`Dictionary<string, object>` is a known root type. The complex value is typed; primitive values remain primitive.

### Snapshot Payloads

Source object:

```csharp
var snapshot = new Snapshot("stream-1", 42, new Dictionary<string, List<int>>
{
    ["values"] = [1, 2, 3]
});
```

JSON shape:

```json
{
  "$type": "NEventStore.Snapshot, NEventStore",
  "BucketId": "default",
  "StreamId": "stream-1",
  "StreamRevision": 42,
  "Payload": {
    "$type": "System.Collections.Generic.Dictionary`2[[System.String,...],[System.Collections.Generic.List`1[[System.Int32,...]],...]], ...",
    "values": {
      "$type": "System.Collections.Generic.List`1[[System.Int32,...]], ...",
      "$values": [1, 2, 3]
    }
  }
}
```

The `Snapshot` converter preserves private-set properties and passes `Payload` through the metadata compatibility layer. The shape is intentionally readable by Newtonsoft.Json. When the declared payload type already contains the nested list type, System.Text.Json may write the nested `values` member as a plain array; it still reads the Newtonsoft `$type` plus `$values` shape shown above.

## Swap Verification

The test suite verifies:

- System.Text.Json round-trips through the shared serialization acceptance tests.
- Newtonsoft output can be read by System.Text.Json for event messages, headers, and snapshot payloads.
- System.Text.Json output can be read by Newtonsoft for event messages, headers, and snapshot payloads.

The mirrored Newtonsoft tests verify the opposite package direction from that project as well.
