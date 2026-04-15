---
name: add-persistence-or-serializer
description: "Use when: adding a new persistence provider, adding a new serializer, reviewing an existing serializer implementation, wiring up a new storage backend, or checking whether a persistence/serialization component follows NEventStore conventions."
---

# Add or Review a Persistence Provider / Serializer

## When to invoke
- User asks to create a new persistence backend (e.g., MongoDB, PostgreSQL).
- User asks to create a new serializer (e.g., MessagePack, Protobuf).
- Code review of an existing provider or serializer against repository conventions.

## Checklist

### For a new **Serializer**

1. **Implement `ISerialize`** (`src/NEventStore/Serialization/ISerialize.cs`):
   ```csharp
   public interface ISerialize
   {
       void Serialize<T>(Stream output, T graph) where T : notnull;
       T? Deserialize<T>(Stream input);
   }
   ```

2. **Create a dedicated project** under `src/NEventStore.Serialization.<Name>/`:
   - Target `netstandard2.0` to stay consistent with the core library.
   - Set `GenerateAssemblyInfo=false` (version is managed by GitVersion).
   - Add `PackageId`, `Authors`, `Description`, `PackageTags` matching the existing `.csproj` files.
   - Include `icon.png`, `license.txt`, and `Readme.md` as pack items pointing to the root files.
   - Enable `GenerateDocumentationFile`, `IncludeSymbols`, `SymbolPackageFormat=snupkg`.
   - Reference `NEventStore.Core.csproj`.

3. **Write the wireup extension** in a class `<Name>SerializationWireupExtension`:
   ```csharp
   public static class <Name>SerializationWireupExtension
   {
       public static SerializationWireup Using<Name>Serialization(
           this PersistenceWireup wireup,
           /* optional settings */)
       {
           return wireup.UsingCustomSerialization(new <Name>Serializer(/* ... */));
       }
   }
   ```
   - Return `SerializationWireup`, not `PersistenceWireup`, so `.Compress()` and `.EncryptWith()` remain available.

4. **Known-types pattern**: if your serializer needs explicit type registration (like the JSON serializer does for `List<EventMessage>` and `Dictionary<string, object>`), accept them via a `params Type[]? knownTypes` parameter.

5. **Logging**: inject `LogFactory.BuildLogger(typeof(MySerializer))` and guard every log call with `Logger.IsEnabled(LogLevel.X)` before formatting the message. Never use `Console.Write*`.

6. **Tests**: create `src/NEventStore.Serialization.<Name>.Tests/` using the same `DefineConstants=NUNIT` pattern. Run `NEventStore.Persistence.AcceptanceTests.SerializationTests` against the new serializer to verify round-trip correctness.

---

### For a new **Persistence Provider**

1. **Implement `IPersistStreams`** (`src/NEventStore/Persistence/IPersistStreams.cs`), which composes `IPersistStreamsSync` and `IPersistStreamsAsync`. Both sides must be implemented.

2. **Create a dedicated project** (same conventions as a serializer project above).

3. **Wireup extension** on `Wireup`, returning `PersistenceWireup`:
   ```csharp
   public static PersistenceWireup Using<Name>Persistence(this Wireup wireup, /* connection options */)
   {
       wireup.Register<IPersistStreams>(new <Name>PersistenceEngine(/* ... */));
       return new PersistenceWireup(wireup);
   }
   ```

4. **Acceptance tests**: reference `NEventStore.Persistence.AcceptanceTests` and provide a `PersistenceEngineFixture` that wires up your engine. All `PersistenceEngineConcern`-based tests must pass green against your implementation.

5. **Thread safety**: `IPersistStreams` must be multi-thread safe. Use thread-safe collections or locking; document any remaining single-threaded constraints in XML doc comments.

6. **`Initialize()` must be synchronous** and complete before the method returns. The wireup calls it during `Build()`.

7. **Checkpoint tokens**: must be monotonically increasing and comparable. Do not use random GUIDs as checkpoint values.

---

## Review Checklist
- [ ] `ISerialize` / `IPersistStreams` fully implemented (no `NotImplementedException` stubs left).
- [ ] Wireup extension returns correct `Wireup` subclass (`SerializationWireup` or `PersistenceWireup`).
- [ ] `GenerateAssemblyInfo=false` in `.csproj`.
- [ ] `icon.png`, `license.txt`, `Readme.md` included as pack items.
- [ ] All public types have XML doc comments.
- [ ] Nullable reference types respected (`?` annotations correct).
- [ ] `Guard.NotNull` used at public boundaries.
- [ ] Logging uses `LogFactory.BuildLogger` with `IsEnabled` guards.
- [ ] Acceptance tests pass (serialization round-trip or persistence contract).
- [ ] `dotnet build ./src/NEventStore.Core.sln -c Release --no-restore` succeeds.
- [ ] `dotnet test ./src/NEventStore.Core.sln -c Release --no-build` passes.
