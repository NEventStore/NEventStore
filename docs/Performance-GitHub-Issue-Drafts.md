# Performance GitHub Issue Drafts

These issue drafts are derived from [Performance-SPEC-Input.md](C:/Work/NEventStore/NEventStore/docs/Performance-SPEC-Input.md).

GitHub issue creation was attempted with `gh issue create`, but the authenticated token does not currently have permission to create issues for `NEventStore/NEventStore`. The exact error was:

`GraphQL: Resource not accessible by personal access token (createIssue)`

Once the token has issue-creation permission, the issues below can be created directly.

## 1. Refactor performance benchmarks into focused, low-noise scenarios

### Title
`Refactor performance benchmarks into focused, low-noise scenarios`

### Body
## Objective
Refactor the benchmark suite so it provides focused, low-noise measurements that can be used to validate each performance change in the program.

## Scope
- Split the current broad persistence benchmark into focused scenarios for:
  - stream open/read
  - commit attempt construction
  - in-memory global checkpoint reads
  - in-memory stream revision reads
  - polling catch-up and idle behavior
  - serializer-only round trips
- Pre-generate commit IDs and event payloads during setup to remove benchmark-side noise.
- Define the benchmark scenarios that will be used as the regression set for later performance work.
- Save baseline benchmark outputs in a repeatable location.

## Out of Scope
- Changing library behavior.
- Adding CI benchmark gating in this issue.
- Changing compatibility targets for core library projects.

## Required Validation
- Existing benchmark project must still run successfully.
- Baseline reports must exist for the scenarios listed above.

## Completion Criteria
- Benchmark results isolate subsystem cost well enough to support the rest of the performance plan.
- Baseline reports are available for future before/after comparisons.

---

## 2. Define test-validation matrix for each performance workstream

### Title
`Define test-validation matrix for each performance workstream`

### Body
## Objective
Define the correctness validation plan for every performance workstream so no optimization lands without explicit test coverage.

## Scope
- Identify the existing tests that already validate:
  - ordering
  - stream revision and commit sequence invariants
  - duplicate detection
  - partial stream behavior
  - snapshot behavior
  - polling semantics
  - serializer round trips
- Identify missing tests that must be added with each implementation issue.
- Produce a test-validation matrix that later issues can reference directly.

## Out of Scope
- Implementing the performance changes.
- Reworking the benchmark suite beyond what is needed to map validation.

## Completion Criteria
- Every planned implementation issue has an explicit correctness-validation path.
- No later issue remains ambiguous about whether existing tests are sufficient.

---

## 3. Reduce hot-path LINQ and commit construction allocations without behavior changes

### Title
`Reduce hot-path LINQ and commit construction allocations without behavior changes`

### Body
## Objective
Land the safest internal allocation and enumeration improvements first, without changing externally observable behavior.

## Scope
- Replace clearly hot LINQ/materialization paths with explicit loops where behavior is straightforward and testable.
- Optimize commit-attempt construction in `OptimisticEventStream` to reduce unnecessary `ToArray` and `ToDictionary` costs.
- Pre-size internal collections where counts are already known.
- Preserve public APIs and current behavior exactly.

## Out of Scope
- Structural re-indexing of the in-memory persistence engine.
- Replacing `LinkedList<EventMessage>` in `OptimisticEventStream`.
- Changing duplicate commit tracking or `EventMessage.Headers` semantics.

## Dependencies
- Benchmark foundation issue
- Validation matrix issue

## Required Validation
- Run the existing tests that cover the affected code paths.
- Add tests if loop-based replacements or copy reductions affect logic not already explicitly covered.
- Record before/after results for the relevant benchmark micro-scenarios.

## Completion Criteria
- At least one targeted benchmark improves measurably.
- No correctness regression is introduced.

---

## 4. Rework in-memory persistence reads to use direct indexes instead of full-store scans

### Title
`Rework in-memory persistence reads to use direct indexes instead of full-store scans`

### Body
## Objective
Eliminate repeated full-store flatten/filter/order/materialize behavior from in-memory checkpoint and stream read paths.

## Scope
- Add direct indexing for:
  - global checkpoint reads
  - bucket checkpoint reads
  - per-stream revision reads
- Rework in-memory read paths to avoid repeated full-store scans and repeated sorting on hot paths.
- Preserve checkpoint ordering and range-boundary semantics exactly.

## Out of Scope
- Polling client changes.
- Serializer changes.
- Public API changes.

## Dependencies
- Benchmark foundation issue
- Validation matrix issue
- Low-risk hot-path cleanup issue

## Required Validation
- Existing acceptance and unit tests must pass.
- Add or confirm tests for ordering, range boundaries, and per-stream revision filtering.
- Record before/after benchmarks for global checkpoint reads, bucket checkpoint reads, and stream revision reads.

## Completion Criteria
- Large-commit-count checkpoint and stream read scenarios show clear improvement.
- Behavior remains identical under tests.

---

## 5. Replace scan-heavy stream-head and snapshot lookups in the in-memory engine

### Title
`Replace scan-heavy stream-head and snapshot lookups in the in-memory engine`

### Body
## Objective
Finish the in-memory engine performance work by removing scan-heavy supporting structures for stream heads, snapshots, delete, and purge.

## Scope
- Replace scan-heavy stream-head lookup structures with direct lookup structures.
- Replace scan-heavy snapshot lookup structures with direct lookup structures.
- Improve delete/purge behavior to avoid repeated array creation and repeated collection scans where safe.
- Preserve snapshot selection and stream-head behavior exactly.

## Out of Scope
- Global checkpoint indexing beyond the main read-path issue.
- Changes to core stream internals.

## Dependencies
- Benchmark foundation issue
- Validation matrix issue
- In-memory read-path rework issue

## Required Validation
- Add or confirm tests for snapshot selection, duplicate snapshot handling, stream head updates, delete stream, purge bucket, and purge store.
- Record before/after benchmarks for scenarios influenced by these supporting structures where measurable.

## Completion Criteria
- Supporting lookup paths are no longer dominated by linear scans.
- Snapshot and delete/purge semantics remain correct under tests.

---

## 6. Replace linked-list event storage in OptimisticEventStream with allocation-efficient list storage

### Title
`Replace linked-list event storage in OptimisticEventStream with allocation-efficient list storage`

### Body
## Objective
Reduce stream materialization and write-path overhead by replacing linked-list-based internal event storage with allocation-efficient list storage.

## Scope
- Replace internal `LinkedList<EventMessage>` usage in `OptimisticEventStream` with `List<EventMessage>`.
- Preserve the public `ICollection<EventMessage>` contract for committed and uncommitted event exposure.
- Pre-size internal collections when counts are known.
- Reduce unnecessary internal copies where safe.
- Preserve stream revision, commit sequence, partial-stream, and sync/async behavior exactly.

## Out of Scope
- Removing duplicate commit-ID tracking.
- Changing `EventMessage.Headers` semantics.
- Public API changes.

## Dependencies
- Benchmark foundation issue
- Validation matrix issue
- Low-risk hot-path cleanup issue

## Required Validation
- Add or confirm tests for open empty stream, open populated stream, append and clear behavior, commit with one and many events, partial stream reads, and revision/commit sequence invariants.
- Record before/after benchmarks for `ReadFromStream`, `ReadFromStreamAsync`, and the relevant write scenarios.

## Completion Criteria
- Stream-read allocations and latency improve measurably.
- Stream semantics remain unchanged under tests.

---

## 7. Audit duplicate commit-id enforcement before changing stream-level identifier tracking

### Title
`Audit duplicate commit-id enforcement before changing stream-level identifier tracking`

### Body
## Objective
Audit whether duplicate commit IDs are already enforced by persistence implementations before changing stream-level identifier tracking.

## Scope
- Review all supported persistence implementations for duplicate commit ID guarantees.
- Decide one of:
  - keep current `_identifiers` tracking in `OptimisticEventStream`
  - remove it universally
  - make it provider-driven or delayed
- Identify the exact tests required for any follow-up implementation.

## Out of Scope
- Implementing the change before the audit is complete.

## Dependencies
- Validation matrix issue

## Required Validation
- Provider-by-provider evidence is documented.
- Follow-up tests are identified for the chosen direction.

## Completion Criteria
- The issue ends with a decision-complete recommendation or an explicit decision not to proceed.

---

## 8. Review lazy header allocation for EventMessage without breaking compatibility

### Title
`Review lazy header allocation for EventMessage without breaking compatibility`

### Body
## Objective
Determine whether `EventMessage.Headers` can be made lazy without breaking caller expectations, serializer behavior, or compatibility.

## Scope
- Audit how `EventMessage.Headers` is used across core code, tests, and serializers.
- Decide whether a safe lazy-allocation design exists while preserving current public semantics.
- Identify the exact tests required for any follow-up implementation.

## Out of Scope
- Shipping the change without an explicit compatibility-safe design.

## Dependencies
- Validation matrix issue

## Required Validation
- Evidence is collected for mutability expectations, non-null expectations, and serializer round-trip behavior.
- Follow-up tests are identified for the chosen direction.

## Completion Criteria
- The issue ends with either an implementation-ready recommendation or an explicit rejection due to compatibility risk.

---

## 9. Improve polling client efficiency for catch-up, idle waits, and shutdown

### Title
`Improve polling client efficiency for catch-up, idle waits, and shutdown`

### Body
## Objective
Improve polling client operational efficiency after the core stream and in-memory work is complete.

## Scope
- Reduce unnecessary delay after successful progress.
- Track and await worker task completion during shutdown instead of polling.
- Preserve stop, retry-on-hole, cancellation, and observer semantics.
- Coordinate with existing related issues `#446` and `#425` rather than ignoring their prior context.

## Out of Scope
- Changing polling client public surface as part of this issue.
- Folding serializer work into the same issue.

## Dependencies
- Benchmark foundation issue
- Validation matrix issue
- Low-risk hot-path cleanup issue
- In-memory read-path rework issue
- Stream internal storage rework issue

## Required Validation
- Add or confirm tests for cancellation, shutdown, stop behavior, retry-on-hole, and backpressure.
- Record benchmarks or stress measurements for idle polling overhead and catch-up throughput.

## Completion Criteria
- Polling overhead is reduced or catch-up latency improves measurably.
- Existing polling semantics remain correct under tests.

---

## 10. Add serializer benchmarks and review byte-handling copies in serialization utilities

### Title
`Add serializer benchmarks and review byte-handling copies in serialization utilities`

### Body
## Objective
Add serializer-specific measurements and then review byte-handling/copy-heavy utilities with compatibility preserved.

## Scope
- Add serializer-only benchmarks for supported serializers and wrappers.
- Review `MemoryStream` and copy-heavy utility paths after serializer baselines exist.
- Preserve serializer interfaces, wire formats, and defaults unless a separate approval is taken.

## Out of Scope
- Changing serializer defaults.
- Bundling unrelated serializer feature work into this issue.

## Dependencies
- Benchmark foundation issue
- Validation matrix issue
- Low-risk hot-path cleanup issue
- In-memory read-path rework issue
- Stream internal storage rework issue

## Required Validation
- Add or confirm serializer round-trip and compatibility-sensitive tests.
- Record before/after serializer benchmark results for the affected utilities.

## Completion Criteria
- Serializer baselines exist and any optimization is benchmark-backed.
- No compatibility regression is introduced.

---

## 11. Performance optimization program: benchmarks, core hot paths, and validation

### Title
`Performance optimization program: benchmarks, core hot paths, and validation`

### Body
## Objective
Track the full NEventStore performance optimization program from benchmark cleanup through compatibility-safe core optimizations and follow-up investigations.

## Milestone Order
1. Benchmark foundation and validation matrix.
2. Low-risk hot-path cleanup.
3. In-memory persistence indexing and supporting-structure rework.
4. `OptimisticEventStream` internal storage rework.
5. Investigation-gated correctness-sensitive changes.
6. Polling and serializer follow-up.
7. Optional modern fast paths are deferred and not part of the first milestone.

## Child Issues
- [ ] Benchmark foundation
- [ ] Validation matrix
- [ ] Low-risk hot-path cleanup
- [ ] In-memory read-path rework
- [ ] In-memory supporting-structure rework
- [ ] `OptimisticEventStream` internal storage rework
- [ ] Duplicate commit-ID tracking audit
- [ ] Lazy header-allocation audit
- [ ] Polling client efficiency follow-up
- [ ] Serializer benchmark and byte-handling follow-up

## Program Rules
- Core compatibility remains `netstandard2.0` and `net462`.
- No public API break is allowed in the main performance program.
- Every implementation change must be validated by tests and by before/after benchmarks from the cleaned benchmark suite.
- Investigation issues do not automatically become implementation work until their recommendations are decision-complete.

## Completion Criteria
- All child issues are resolved or explicitly closed with documented decisions.
- Program summary captures benchmark deltas and correctness-validation outcomes for each implemented workstream.
