# Performance Validation Matrix

This document defines the correctness and measurement contract for performance foundation issue `#524` and the remaining implementation workstreams in umbrella issue `#533`.

Use [Performance-Benchmarks.md](Performance-Benchmarks.md) for benchmark commands, scenario names, and artifact locations.

Every performance change must satisfy both of these requirements:

- The relevant correctness tests are run for the changed behavior.
- Before/after benchmark or measurement evidence is recorded from the focused benchmark suite introduced in `#523`.

## Validation Anchors

These existing suites are the baseline reference points for all later performance issues:

- `src/NEventStore.Tests/OptimisticEventStoreTests.cs` and `src/NEventStore.Tests/OptimisticEventStoreTests.Async.cs`: stream open paths, snapshot-based open, and revision-range reads.
- `src/NEventStore.Tests/OptimisticEventStreamTests.cs` and `src/NEventStore.Tests/OptimisticEventStreamTests.Async.cs`: stream materialization, append and clear behavior, commit construction, duplicate commit identifiers, partial stream reads, and revision/commit sequence behavior.
- `src/NEventStore.Tests/OptimisticPipelineHookTests.cs`: commit sequence and stream revision invariants, plus delete/purge side effects on optimistic tracking.
- `src/NEventStore.Persistence.AcceptanceTests/PersistenceTests.cs` and `src/NEventStore.Persistence.AcceptanceTests/PersistenceTests.Async.cs`: persistence ordering, checkpoint paging, duplicate detection, snapshot behavior, stream-to-snapshot behavior, and purge semantics.
- `src/NEventStore.Tests/Persistence/InMemory/InMemoryPersistenceTests.cs`: in-memory checkpoint range-boundary regressions.
- `src/NEventStore.Tests/Client/AsyncPollingClientTests.cs`, `src/NEventStore.Tests/Client/PollingClient2Tests.cs`, and `src/NEventStore.Tests/Client/PollingClientRxTests.cs`: polling dispatch, stop/retry/manual polling, and subscription semantics.
- `src/NEventStore.Persistence.AcceptanceTests/SerializationTests.cs` plus the fixture files under `src/NEventStore.Serialization.*.Tests/`: serializer round trips and wrapper compatibility.

## Workstream Matrix

### Issue #525: Reduce hot-path LINQ and commit construction allocations without behavior changes

- Existing correctness suites: `OptimisticEventStreamTests(.Async)` for commit construction, header copying, append/clear behavior, duplicate commit identifiers, and revision/commit sequence updates; `OptimisticPipelineHookTests` for optimistic concurrency invariants.
- Mandatory additions: No new suite is required up front. If loop-based replacements change copy behavior, add sync and async unit tests that assert zero-count handling and copy independence for commit headers and committed events.
- Benchmark evidence: `CommitAttemptBenchmarks`, `StreamWriteBenchmarks`, and `StreamWriteLargeBenchmarks`.
- Completion gate: targeted stream tests remain green and at least one affected write-path benchmark shows a measurable improvement with no correctness regression.

### Issue #526: Rework in-memory persistence reads to use direct indexes instead of full-store scans

- Existing correctness suites: `PersistenceTests(.Async)` scenarios for reading from revision ranges, checkpoint paging, checkpoint-to-checkpoint paging, bucket-specific checkpoint reads, and checkpoint ordering across multiple buckets; `InMemoryPersistenceTests` for checkpoint range bounds.
- Mandatory additions: If direct indexes are introduced through new helper types or caches, add focused `InMemoryPersistenceTests` coverage for inclusive lower bounds, exclusive upper bounds, and empty-range results. If the refactor only replaces the internal implementation of the existing methods, the current acceptance coverage is the minimum required baseline.
- Benchmark evidence: `InMemoryReadBenchmarks`.
- Completion gate: ordering and range-boundary tests pass, and before/after results are recorded for global checkpoint reads, bucket checkpoint reads, and stream revision reads.

### Issue #527: Replace scan-heavy stream-head and snapshot lookups in the in-memory engine

- Existing correctness suites: `PersistenceTests(.Async)` scenarios for saving snapshots, retrieving the most recent prior snapshot, duplicate snapshot handling, streams-to-snapshot behavior after commits, bucket-specific snapshot isolation, purge store, and purge bucket; `OptimisticPipelineHookTests` for delete stream, purge bucket, and purge store tracking.
- Mandatory additions: Add in-memory-specific tests that directly verify stream-head and snapshot lookup structures stay correct after commit, snapshot add/update, delete stream, purge bucket, and purge store operations. The current acceptance tests validate behavior at the API boundary; this issue also needs direct regression coverage for index maintenance.
- Benchmark evidence: `StreamOpenBenchmarks` for stream-head lookup impact and `InMemoryReadBenchmarks` where supporting lookup structures affect visible read paths. If delete/purge changes are not measurable through the current benchmark set, record focused local measurements in the implementation issue.
- Completion gate: snapshot and purge semantics remain green under acceptance and unit tests, and measurable lookup-path changes are backed by benchmark or focused measurement data.

### Issue #528: Replace linked-list event storage in OptimisticEventStream with allocation-efficient list storage

- Existing correctness suites: `OptimisticEventStoreTests(.Async)` for opening empty streams, opening populated streams, and snapshot-based opens; `OptimisticEventStreamTests(.Async)` for partial reads, append one/many, clear behavior, commit behavior, collection mutation guards, duplicate identifiers, and the `issue_420` regression scenarios.
- Mandatory additions: No new suite is required if the public `ICollection<EventMessage>` behavior remains unchanged. If the storage swap affects collection exposure semantics, add sync and async tests that explicitly assert committed/uncommitted event order is preserved and that external callers still cannot mutate the exposed committed event collections.
- Benchmark evidence: `StreamOpenBenchmarks`, `StreamWriteBenchmarks`, and `StreamWriteLargeBenchmarks`.
- Completion gate: open/read and write benchmarks show a measurable gain, and stream revision, commit sequence, partial-stream, and collection-behavior tests remain unchanged.

### Issue #529: Audit duplicate commit-id enforcement before changing stream-level identifier tracking

- Existing correctness suites: `PersistenceTests(.Async)` scenarios for persisting the same commit twice and reusing a commit ID on the same stream; `OptimisticEventStreamTests(.Async)` scenarios for committing with an identifier that was previously read.
- Mandatory additions: No implementation change is allowed until an audit note documents duplicate-commit-ID guarantees for the in-repo in-memory persistence engine and any supported external providers. If the chosen direction changes `_identifiers` behavior, add sync and async tests for duplicate commit IDs after reopening a stream and after partial-stream reads.
- Benchmark evidence: none for the audit-only issue. Any later implementation should reuse the write benchmarks from `#525`/`#528`.
- Completion gate: provider evidence is documented, the follow-up direction is explicit, and the required follow-up tests are named before any code change lands.

### Issue #530: Review lazy header allocation for EventMessage without breaking compatibility

- Existing correctness suites: `PersistenceTests(.Async)` for header persistence, `OptimisticEventStreamTests(.Async)` for commit-header copy behavior, and `SerializationTests` for header dictionary round trips.
- Mandatory additions: Add explicit compatibility tests before changing `EventMessage.Headers`. At minimum, assert that a new `EventMessage` exposes a non-null writable `Headers` dictionary, header mutations survive commit construction, and empty/populated headers round-trip through every affected serializer or wrapper.
- Benchmark evidence: `CommitAttemptBenchmarks`; if serializer behavior is touched, also use `SerializerRoundTripBenchmarks`.
- Completion gate: compatibility-sensitive tests are added first, the semantics decision is explicit, and any approved implementation is benchmark-backed.

### Issue #531: Improve polling client efficiency for catch-up, idle waits, and shutdown

- Existing correctness suites: `AsyncPollingClientTests` for dispatch, stop behavior, and manual polling; `PollingClient2Tests` for stop, retry, retry-then-move-next, and manual polling; `PollingClientRxTests` for subscription and bucket-filtering semantics.
- Mandatory additions: Add tests for cancellation-token shutdown, worker completion during dispose/stop, and the no-extra-delay-after-progress behavior in the polling implementation being changed. The current suites do not explicitly prove cancellation and shutdown completion semantics.
- Benchmark evidence: `AsyncPollingBenchmarks`. If shutdown improvements cannot be demonstrated through the microbenchmark harness, record a focused stress measurement in the implementation issue.
- Completion gate: polling semantics remain correct under tests, and idle or catch-up measurements show a measurable operational improvement.

### Issue #532: Add serializer benchmarks and review byte-handling copies in serialization utilities

- Existing correctness suites: `SerializationTests` for object, event-message, header, and snapshot payload round trips, plus the serializer-specific fixture files under `src/NEventStore.Serialization.*.Tests/`.
- Mandatory additions: If copy-heavy utilities or wrappers change, add focused tests in each affected serializer project for large payload round trips and wrapper equivalence. The current benchmark suite only establishes JSON and gzip-wrapped JSON baselines, so extend serializer benchmarks to cover the serializer or wrapper being optimized before claiming a performance win.
- Benchmark evidence: `SerializerRoundTripBenchmarks`, expanded as needed for the serializer/wrapper under change.
- Completion gate: compatibility-sensitive serializer tests remain green, benchmark baselines exist for the changed serializer path, and before/after numbers are recorded with the implementation.
