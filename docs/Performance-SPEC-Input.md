# Performance Optimization SPEC Input

This document collects the current performance observations, the most promising optimization opportunities, and a phased implementation strategy for NEventStore.

The goal is not to jump directly into code changes. The goal is to produce a practical input document for a formal SPEC, with enough detail to decide scope, ordering, compatibility constraints, validation criteria, and benchmark coverage.

## Goals

- Improve throughput and reduce allocations in the most frequently used paths.
- Reduce latency for stream reads, commit writes, and polling/catch-up reads.
- Improve the quality of the benchmark suite so it measures library cost more accurately.
- Modernize the implementation where useful without sacrificing maximum compatibility.
- Require every implementation change to be validated by tests, using existing coverage when sufficient and adding new tests when it is not.
- Preserve the current public programming model unless a change has a strong payoff and a low migration cost.

## Non-Goals

- Rewriting the entire library around a different storage abstraction.
- Removing support for existing target frameworks.
- Introducing large public API changes as part of the first optimization wave.
- Optimizing obscure code paths before the main event-stream, persistence, and polling hot paths.

## Compatibility Guardrails

The optimization plan should assume that compatibility is a hard requirement.

- Keep the existing compatibility targets for the core package.
  Current core targets are `netstandard2.0` and `net462`.
- Prefer internal implementation changes over public API changes.
- When modern runtime-specific optimizations are valuable, add them as optional fast paths behind multi-targeting rather than replacing the compatibility implementation.
- Do not make benchmark-only runtime upgrades a prerequisite for library consumers.
- Avoid changes that force downstream projects to switch serializers, persistence implementations, or target frameworks.
- Keep serialized shapes and persistence behavior stable unless an explicit compatibility review says otherwise.
- Treat test validation as mandatory for every change. A performance improvement is not complete until it is covered by existing tests or by new tests added with the change.

## Test Validation Requirement

Every implementation change in the future SPEC must include an explicit test validation plan.

- No performance-oriented code change should be merged on benchmark evidence alone.
- If existing automated tests already validate the affected behavior, the implementation plan should name those tests explicitly.
- If existing tests do not cover the affected behavior closely enough, the implementation plan should add new unit, integration, acceptance, or regression tests as appropriate.
- Every phase should define both:
  - performance validation through benchmarks or measurements
  - correctness validation through tests
- When a change is intentionally internal and should not alter behavior, tests should prove behavioral equivalence at the public API or persistence-contract level.
- When a change affects concurrency, ordering, duplicate detection, serialization, polling, or snapshot behavior, new tests should generally be assumed necessary unless equivalent coverage already exists.

## Current Evidence

The repository already contains checked-in BenchmarkDotNet results for the in-memory persistence path.

Observed shape from the existing `PersistenceBenchmarks` results on .NET 9:

- `WriteToStream` at `100000` commits: about `385.6 ms` and `383 MB` allocated.
- `WriteToStreamAsync` at `100000` commits: about `404.9 ms` and `397 MB` allocated.
- `ReadFromStream` at `100000` commits: about `23.6 ms` and `18.8 MB` allocated.
- `ReadFromEventStore` at `100000` commits: about `2.08 ms` and `248 B` allocated.

This already tells us two important things:

- Stream materialization adds a large amount of extra work beyond raw commit enumeration.
- The write path is allocation-heavy and scales poorly as commit volume grows.

The current benchmark harness also adds avoidable benchmark-side work:

- It creates a new `Guid` per commit.
- It converts the loop index to string per event.
- It only exercises in-memory persistence, so serializer costs and other persistence implementations are not represented.

That means the existing results are still useful, but they are not clean enough to support fine-grained optimization decisions on their own.

## Main Optimization Workstreams

### 1. Re-index the In-Memory Persistence Engine

Affected area:

- `src/NEventStore/Persistence/InMemory/InMemoryPersistenceEngine.cs`

Current issue:

- Global checkpoint queries flatten all buckets, clone bucket commit arrays, filter, sort, and materialize a new array on every call.
- Per-stream queries also rely on repeated LINQ scans and materialization.
- Stream-head and snapshot lookups use linear searches over linked collections.

Why this matters:

- The in-memory engine is used by the benchmark suite, examples, tests, and likely some production-like scenarios.
- Polling and catch-up reads can become `O(total_commits)` per query even when the caller only needs data after one checkpoint.
- The current implementation allocates aggressively during reads because it repeatedly builds arrays and enumerates intermediate LINQ pipelines.

Suggested direction:

- Maintain an append-only global checkpoint index for commits.
- Maintain per-bucket and per-stream indexes for direct range scans.
- Replace linked-list-based stream-head and snapshot lookup structures with dictionaries keyed by stream identity.
- Use direct loops instead of repeated `Where`, `OrderBy`, `SelectMany`, and `ToArray` on hot paths.

Expected impact:

- Significant reduction in CPU time and allocation rate for `GetFrom(checkpoint)` and `GetFromTo`.
- Better scaling for polling clients and catch-up readers.
- More representative benchmark results for higher commit counts.

Compatibility risk:

- Low if behavior remains identical.
- Medium if ordering, duplicate detection, or snapshot selection semantics change unintentionally.

Validation requirements:

- Existing acceptance and unit tests must still pass.
- Changes in this area should ship with targeted tests for ordering, range boundaries, snapshot selection, and delete/purge behavior unless existing coverage is already explicit and sufficient.
- Add targeted benchmarks for:
  - global checkpoint reads
  - bucket checkpoint reads
  - stream revision reads
  - snapshot lookup
  - delete/purge operations

### 2. Replace LinkedList-Based Event Storage in OptimisticEventStream

Affected area:

- `src/NEventStore/OptimisticEventStream.cs`

Current issue:

- Committed and uncommitted events are stored in `LinkedList<EventMessage>`.
- The write path copies uncommitted headers and events into new collections on each commit attempt.
- Stream population walks commits and events one item at a time with limited use of count-based pre-sizing.

Why this matters:

- `LinkedList<T>` has poor cache locality and higher per-node overhead than `List<T>`.
- The benchmark data strongly suggests that stream materialization is a major source of read overhead.
- Commit creation currently performs extra allocations that could be reduced substantially.

Suggested direction:

- Replace internal `LinkedList<EventMessage>` storage with `List<EventMessage>`.
- Keep the public `ICollection<EventMessage>` contract unchanged.
- Optimize `BuildCommitAttempt` to avoid generic LINQ conversions on every commit.
- Pre-size lists and dictionaries where the sizes are already known.
- Review whether committed events need to be copied at all in some internal transitions, or whether references can be reused safely.

Expected impact:

- Lower per-event memory overhead.
- Faster iteration during stream population and reads.
- Reduced allocation rate in the write path.

Compatibility risk:

- Low if the public interfaces remain unchanged and observable behavior stays the same.
- Must verify no tests depend on linked-list-specific enumeration behavior.

Validation requirements:

- Add focused benchmarks for:
  - open empty stream
  - open populated stream
  - append N events to an already-open stream
  - commit with 1 event vs many events
  - read committed events through the public stream API
- Add or identify tests that cover stream open, append, commit, clear, revision tracking, and partial-stream behavior.

### 3. Remove or Reduce Duplicate Commit-ID Tracking in Stream Instances

Affected areas:

- `src/NEventStore/OptimisticEventStream.cs`
- persistence implementations that already enforce duplicate commit detection

Current issue:

- Each opened stream accumulates all prior commit IDs in a `HashSet<Guid>`.
- This duplicates data that persistence engines may already validate.

Why this matters:

- The memory cost grows with stream history length.
- Stream opening pays an additional cost proportional to historical commit count.

Suggested direction:

- Verify which persistence implementations already guarantee duplicate commit ID detection.
- If the guarantee is universal, remove the stream-level `_identifiers` cache entirely.
- If the guarantee is not universal, consider making the stream-level cache optional, delayed, or provider-driven.

Expected impact:

- Reduced memory footprint for large streams.
- Faster stream initialization.

Compatibility risk:

- Medium because duplicate commit behavior is correctness-sensitive.
- This should only be changed after a provider-by-provider behavior audit.

Validation requirements:

- Duplicate commit tests across all supported persistence implementations.
- Benchmarks for stream initialization before and after the change.

### 4. Make EventMessage Header Allocation Lazy

Affected area:

- `src/NEventStore/EventMessage.cs`

Current issue:

- Every `EventMessage` constructs an empty `Dictionary<string, object>` even when no headers are used.

Why this matters:

- Event messages are a core unit of data.
- Empty dictionary allocation is pure overhead in the common case where only `Body` is set.

Suggested direction:

- Use lazy header initialization.
- Preserve the current public shape and semantics as much as possible.
- If the property cannot safely become nullable from a compatibility perspective, use an internal shared empty instance or a lazy backing field with accessor logic.

Expected impact:

- Lower allocation rate on write-heavy workloads.
- Reduced memory footprint across all serializers and persistence engines.

Compatibility risk:

- Medium because callers may assume `Headers` is always a mutable non-null dictionary.
- This must be designed carefully to avoid subtle behavior changes.

Validation requirements:

- Unit tests for mutation semantics.
- Serialization tests for events with and without headers.
- Microbenchmarks around event creation.

### 5. Reduce LINQ and Materialization in Hot Paths

Affected areas:

- `src/NEventStore/Persistence/InMemory/InMemoryPersistenceEngine.cs`
- `src/NEventStore/OptimisticPipelineHook.cs`
- `src/NEventStore/OptimisticEventStore.cs`
- `src/NEventStore/Serialization/SerializationExtensions.cs`

Current issue:

- Several hot paths use LINQ plus immediate materialization (`ToArray`, `ToDictionary`, `OrderBy`, `SelectMany`) where straightforward loops would be cheaper.
- Some startup/configuration paths use `Any()` over enumerables that could be materialized once.

Why this matters:

- Individually these are moderate costs, but together they produce steady overhead in read and write loops.
- This is especially relevant for `netstandard2.0`, where modern JIT/runtime optimizations are less available than in `net8.0`.

Suggested direction:

- Replace LINQ on hot paths with explicit loops.
- Avoid `MemoryStream.ToArray()` when a buffer can be exposed or pre-sized safely.
- Replace repeated enumeration of hook lists with cached arrays where appropriate.

Expected impact:

- Moderate but widespread reductions in allocation pressure and CPU time.

Compatibility risk:

- Low if behavior remains the same.

Validation requirements:

- Benchmark before/after on the affected scenarios.
- Keep the code readable; do not trade away maintainability for micro-optimizations with negligible effect.
- Ensure the affected behavior is covered by tests, especially where loops replace LINQ and internal iteration logic changes.

### 6. Improve Polling Client Efficiency

Affected areas:

- `src/NEventStore.PollingClient/AsyncPollingClient.cs`
- `src/NEventStore.PollingClient/PollingClient2.cs`
- `src/NEventStore.PollingClient/CommitSequencer.cs`

Current issue:

- The async polling client sleeps after every polling cycle even if progress was made.
- `StopAsync` uses a polling wait loop instead of awaiting the worker task directly.
- The synchronous polling client uses older thread/timer coordination primitives that are functional but not especially efficient.

Why this matters:

- Polling paths directly affect catch-up speed and idle overhead.
- These clients are often used for long-running projection/subscription workloads where wakeup patterns matter.

Suggested direction:

- Delay only when no commits were processed.
- Track the polling task and await it directly during shutdown.
- Review whether the synchronous client should keep its current design for compatibility while the async client becomes the primary optimized path.
- Add benchmarks or stress tests for idle polling, burst polling, and sustained catch-up.

Expected impact:

- Better catch-up latency.
- Lower idle CPU and timer churn.
- Cleaner shutdown behavior.

Compatibility risk:

- Low if public behavior is preserved.
- Medium if stop/retry sequencing semantics change.

Validation requirements:

- Integration tests around cancellation, stop, retry-on-hole, and backpressure.
- Benchmarks or load tests that measure:
  - idle polling overhead
  - catch-up throughput
  - latency to process a new commit after it arrives

### 7. Serializer and Byte-Handling Review

Affected areas:

- `src/NEventStore/Serialization/*`
- serializer packages under `src/NEventStore.Serialization.*`

Current issue:

- Some serializer utilities use `MemoryStream` and `ToArray`, which can force additional copies.
- The benchmark suite currently does not isolate serializer cost, so changes here are hard to evaluate.

Why this matters:

- In non-in-memory persistence engines, serialization can become a large part of end-to-end latency and allocation.

Suggested direction:

- Keep serializer interfaces stable.
- Add serializer-specific benchmarks for JSON, MessagePack, binary, and compression wrappers.
- Where safe, reduce intermediate copies and improve buffer sizing.
- Treat serializer modernization separately from the core event-store optimization work.

Expected impact:

- Moderate to high, depending on the persistence engine and serializer combination.

Compatibility risk:

- Medium because serializer changes can affect payload shape, metadata handling, and backward compatibility.

Validation requirements:

- Cross-version compatibility tests for stored payloads.
- Dedicated serializer benchmarks.
- Separate approval gate before changing serializer defaults.

## Benchmark Suite Improvements

The benchmark project needs improvement before it can serve as the main decision tool for the optimization effort.

### Problems in the Current Benchmark Harness

- It mixes benchmark overhead with library overhead.
- It only measures the in-memory persistence path.
- It does not separate stream materialization cost from raw persistence iteration cost cleanly enough.
- It does not include focused microbenchmarks for the specific hot methods identified above.

### Benchmark Improvement Goals

- Make benchmarks precise enough to support implementation tradeoffs.
- Separate allocation cost of the benchmark fixture from allocation cost of the library.
- Add coverage for low-level components and end-to-end flows.
- Keep benchmarks runnable on modern runtimes without changing the compatibility promise of the library itself.

### Concrete Benchmark Work

#### 1. Split the Current Persistence Benchmark into Smaller Scenarios

Add separate benchmark classes for:

- stream open/read
- commit attempt construction
- in-memory global checkpoint read
- in-memory stream revision read
- polling client catch-up
- polling client idle behavior
- serializer-only scenarios

#### 2. Remove Benchmark-Side Noise

Current benchmark code creates work that is not part of the library:

- `Guid.NewGuid()` per commit
- `i.ToString()` per event

Suggested improvement:

- Pre-generate commit IDs and event payloads during setup.
- Reuse prebuilt event bodies where the benchmark goal is store overhead rather than object creation cost.
- If event payload creation is intentionally part of the scenario, isolate it in a separate benchmark and label it clearly.

#### 3. Add Baselines and Parameterized Dimensions

Useful dimensions:

- commit count
- events per commit
- headers per event
- headers per commit
- empty vs populated stream
- sync vs async APIs
- with vs without optimistic pipeline hook
- serializer type

Useful baselines:

- raw in-memory commit enumeration
- stream open over same data
- direct serializer benchmark without store interaction

#### 4. Add More Diagnosers and Exporters

Useful additions:

- memory diagnoser
- disassembly diagnoser for selected microbenchmarks
- markdown and csv exporters checked into artifacts
- explicit runtime jobs for comparison

The benchmark project can target newer runtimes even if the core package remains compatible with older ones.

#### 5. Separate Compatibility Targets from Benchmark Targets

Recommended approach:

- Keep the core package compatibility targets intact.
- Allow the benchmark project to target modern runtimes such as `net8.0` and `net9.0`.
- Optionally add benchmark runs against multiple runtimes to understand which gains come from code changes and which come from runtime improvements.

#### 6. Add Regression Gates

Once the suite is stable enough:

- define a small set of representative benchmark scenarios
- track them in CI or scheduled runs
- compare against saved baselines before merging major internal changes

This should be done after the benchmark suite is cleaned up, not before.

## Modernization Strategy Without Losing Compatibility

Modernization is still useful, but it should be layered.

### Recommended Strategy

#### 1. Preserve Current Public Targets

- Keep `netstandard2.0` and `net462` for the core package.

#### 2. Add Optional Modern Targets for Internal Fast Paths

Where the payoff is clear, consider adding `net8.0` to selected packages:

- core package
- polling client
- serializer packages that benefit from newer APIs

This should be done only if:

- the build/test matrix remains manageable
- package behavior stays consistent
- conditional code is kept contained and readable

#### 3. Use Partial Classes or Conditional Compilation for Runtime-Specific Optimizations

Examples of modern-only implementation choices:

- pooled buffers
- runtime-specific collection helpers
- improved timers and async coordination
- lower-overhead serialization helpers

The compatibility implementation should remain the default fallback.

#### 4. Do Not Let Modernization Block the First Performance Wins

The first wave should focus on structural improvements that help all supported runtimes:

- better indexing
- fewer allocations
- fewer copies
- fewer linear scans

These changes are valuable even without adding any new target framework.

## Suggested Implementation Phases

### Phase 0. Benchmark Cleanup and Baseline Definition

Deliverables:

- Refactored benchmark suite with smaller scenarios
- Reduced benchmark-generated noise
- Saved baseline reports for current implementation

Why first:

- This creates a reliable measurement framework before invasive changes begin.
- It also establishes the test matrix that later phases must use when validating behavior.

### Phase 1. Low-Risk Internal Allocation Cuts

Candidate work:

- replace LINQ in hot paths
- pre-size collections
- optimize commit attempt building
- reduce copying where behavior is already clear

Why here:

- Low risk
- Immediate measurable wins
- Helps separate cheap improvements from structural ones
- Creates the first repeatable pattern of "change + benchmark + tests" for the rest of the program.

### Phase 2. In-Memory Persistence Rework

Candidate work:

- global checkpoint index
- per-bucket/per-stream indexes
- dictionary-based heads and snapshots

Why here:

- Highest likely impact on existing benchmarks
- Also helps tests, examples, and polling scenarios

### Phase 3. Stream Materialization Rework

Candidate work:

- replace linked lists with lists
- reduce commit/event copying
- review duplicate commit cache strategy

Why here:

- Large impact on `OpenStream` and `ReadFromStream`
- Likely one of the main contributors to the read gap vs raw event-store scans

### Phase 4. Polling Client Optimization

Candidate work:

- more efficient delay/shutdown logic
- benchmark catch-up and idle overhead
- maintain behavior while improving operational efficiency

### Phase 5. Optional Modern Fast Paths

Candidate work:

- add `net8.0` target where justified
- pool buffers
- runtime-specific optimizations

Why last:

- Should be additive, not foundational
- Easier once the compatibility implementation is already improved

## Acceptance Criteria for the Future SPEC

The formal SPEC should define target improvements for both allocations and latency.

Recommended acceptance structure:

- No public API break unless explicitly approved.
- Existing tests remain green across supported targets.
- Every code change is validated by tests, using existing tests when they are demonstrably sufficient and adding new tests when they are not.
- Benchmarks are reproducible and checked in with the SPEC.
- Each optimization phase must show improvement in at least one representative benchmark without unacceptable regressions elsewhere.
- Serialization compatibility must be preserved unless a separate migration plan is approved.

Example benchmark success criteria:

- measurable reduction in `WriteToStream` allocations at `10000` and `100000` commits
- measurable reduction in `ReadFromStream` allocation and latency at the same scales
- measurable reduction in polling catch-up latency
- no regression in correctness or duplicate/concurrency handling

Example test-validation criteria:

- the implementation plan names the existing tests that validate the changed behavior, or
- the implementation plan includes new tests in the same change set, with scope matching the affected behavior
- performance-only benchmark additions do not replace correctness tests

## Open Questions for the SPEC

These should be answered before implementation begins:

- Which persistence implementations, beyond the in-memory engine, are important enough to benchmark in phase 1?
- Is duplicate commit detection guaranteed by every persistence implementation, or only by some of them?
- Is adding `net8.0` to the core package acceptable if `netstandard2.0` and `net462` remain supported?
- Should performance work optimize only the core package first, or should polling client improvements be included in the first milestone?
- Which benchmark scenarios should be treated as release-gating regressions?

## Recommended First Milestone

If the work needs to start with a narrow and high-value milestone, use this:

- Clean up the benchmark suite.
- Rework the in-memory persistence indexing strategy.
- Replace `LinkedList<EventMessage>` with `List<EventMessage>` in `OptimisticEventStream`.
- Optimize commit attempt construction to reduce unnecessary allocations.

This milestone is large enough to matter, but still focused on changes that are:

- internal
- measurable
- broadly beneficial
- compatible with the current public surface

## Summary

The strongest current candidates are:

- in-memory persistence indexing
- stream internal data structure changes
- commit construction allocation reduction
- polling client efficiency improvements
- benchmark harness cleanup

The recommended approach is to improve the benchmark suite first, then implement internal low-risk changes, then move into structural read/write optimizations, and only after that add optional runtime-specific fast paths.

That sequence gives NEventStore a credible path to substantially better performance while preserving the compatibility profile that existing users depend on.
