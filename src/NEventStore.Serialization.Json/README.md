# NEventStore.Serialization.Json

`NEventStore.Serialization.Json` is the Newtonsoft.Json serializer for NEventStore. It is the compatibility contract that the System.Text.Json serializer follows so either package can be used for the same persisted JSON.

## Wireup

```csharp
var store = Wireup.Init()
    .UsingInMemoryPersistence()
    .UsingJsonSerialization()
    .Build();
```

Custom Newtonsoft settings can be supplied, and root known types can be overridden:

```csharp
var serializer = new JsonSerializer(
    new JsonSerializerSettings(),
    typeof(List<EventMessage>),
    typeof(Dictionary<string, object>));
```

The default known types are:

- `List<EventMessage>`
- `Dictionary<string, object>`

Passing `null` or an empty known-type array keeps those defaults. Passing a non-empty array replaces them.

## Implementation Model

The serializer has two internal Newtonsoft serializers:

- Untyped serializer for known root types: `TypeNameHandling.Auto`, `DefaultValueHandling.Ignore`, `NullValueHandling.Ignore`.
- Typed serializer for every other root type: `TypeNameHandling.All`, `DefaultValueHandling.Ignore`, `NullValueHandling.Ignore`.

Known root types are intentionally written without a root `$type` wrapper. Their polymorphic `object` members still receive `$type` metadata when the runtime value is not assignable from the declared type. This is what preserves event body, header, and snapshot payload types.

Unknown root types are written with root `$type` metadata. This keeps snapshots and custom serialized root objects self-describing.

## Compatibility Contract

The JSON contract is based on Newtonsoft.Json metadata names:

- `$type` stores an assembly-qualified CLR type name.
- `$values` stores array contents when the typed value itself is an array or collection.
- Null properties and default values are omitted.
- Known root types are read and written without root `$type` metadata.
- Polymorphic `object` values are read and written with `$type` metadata when needed.

The System.Text.Json serializer emits and reads the same metadata contract for the NEventStore cases that require polymorphism. The two serializers are wire-compatible; they are not required to produce byte-for-byte identical JSON when static property types already provide enough type information.

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

`List<EventMessage>` is a known root type, so the array has no root `$type`. The third `Body` is declared as `object`, so it has `$type` metadata.

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

`Dictionary<string, object>` is a known root type, so the dictionary itself has no root `$type`. Complex values are typed.

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

`Snapshot` is not a known root type, so the root has `$type`. Its `Payload` property is declared as `object`, so the dictionary payload is typed. The nested list uses `$values` because typed collections are represented as objects with metadata plus values.

## Swap Verification

The test suite verifies:

- Newtonsoft round-trips through the shared serialization acceptance tests.
- Default and custom known-type selection.
- System.Text.Json output can be read by Newtonsoft for event messages, headers, and snapshot payloads.

The mirrored System.Text.Json tests verify the reverse direction.
