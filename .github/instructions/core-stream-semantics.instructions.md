---
applyTo: "src/NEventStore/**"
description: "Strict rules for editing NEventStore core stream semantics, pipeline hooks, and event store composition."
---

# Core Stream Semantics

## Thread-Safety Contract
- `IStoreEvents` and `IPersistStreams` **must** remain multi-thread safe. Never introduce instance state that is mutated during read or commit operations without synchronisation.
- `IEventStream` (`OptimisticEventStream`) is **single-threaded**. Each unit of work must call `IStoreEvents.OpenStream` or `CreateStream` independently; do not pass streams between threads or re-use them across scopes.

## Stream Revision Invariants
- `StreamRevision` tracks the highest event-level counter. It must always equal the sum of event counts across all commits on a stream.
- `CommitSequence` tracks the number of commits. It must increment by exactly 1 per successful commit.
- A gap in either counter is a `StorageException` (beyond-end-of-stream). A collision is a `ConcurrencyException`. Both are enforced in `OptimisticPipelineHook.PreCommit`; do not bypass those checks.
- When opening a partial stream (`minRevision > 0`), `CommittedEvents` will only contain events within the requested range. Do not assume the full history is loaded.

## Pipeline Hooks
- Hooks are called in registration order for `PreCommit` and `PostCommit`. Order matters for features like deduplication and concurrency checking.
- `PreCommit` returning `false` silently cancels the commit and skips `PostCommit`. Hooks must be idempotent and must not assume `PostCommit` will always run after a `PreCommit` approval.
- `OptimisticEventStore.Commit` calls async hooks via `.GetAwaiter().GetResult()` on the synchronous path — this is intentional for backward compat. Do not introduce additional blocking `.GetAwaiter().GetResult()` calls on the hot path.
- New hooks should extend `PipelineHookBase` (sync) or `PipelineHookAsyncBase` (async) and only override the methods that need custom behaviour. Both base classes follow the virtual/override pattern.

## Wireup / Composition
- All configuration surfaces go through the fluent wireup chain: `Wireup → PersistenceWireup → SerializationWireup`. New extension points must return the appropriate `Wireup` subclass so the chain remains composable.
- Register services via `wireup.Register<TInterface>(implementation)` into `NanoContainer`; never bypass the container.
- Logging uses `LogFactory.BuildLogger(typeof(MyClass))` backed by `Microsoft.Extensions.Logging`. Do not use `Console.Write*` or introduce a new logging abstraction.

## Naming & Null Safety
- Nullable reference types are enabled. Use `?` for optional returns (e.g., `ICommit?` from `CommitChanges`). Annotate method contracts correctly — do not suppress `#nullable` directives.
- Guard clauses use expression-based parameter names: `Guard.NotNull(() => attempt, attempt)`. Follow the same pattern for any new public boundary checks.
