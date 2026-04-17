# Duplicate Commit-Id Audit

This note closes the audit scope for issue `#529`.

## Objective

Determine whether duplicate commit IDs are already enforced by the persistence layer before changing the stream-level `_identifiers` tracking in `OptimisticEventStream`.

## Finding Summary

All currently supported persistence providers audited here enforce duplicate commit IDs at the persistence boundary with the same effective scope: `BucketId + StreamId + CommitId`.

That means `_identifiers` is not the correctness boundary for duplicate detection. It is only an in-memory fast-fail cache for commit IDs that happened to be loaded into the current `OptimisticEventStream` instance.

## Provider Evidence

### Core in-memory provider

- [InMemoryPersistenceEngine.cs](C:/Work/NEventStore/NEventStore/src/NEventStore/Persistence/InMemory/InMemoryPersistenceEngine.cs) stores duplicate identities in `_potentialDuplicates`.
- The duplicate identity comparer includes `BucketId`, `StreamId`, and `CommitId`, so duplicate detection is scoped per stream inside a bucket.
- `DetectDuplicate(CommitAttempt attempt)` throws `DuplicateCommitException` before the commit is inserted.
- The core acceptance suite already asserts duplicate rejection for:
  - persisting the same commit twice
  - reusing a commit ID on the same stream

### MongoDB provider

- `NEventStore.Persistence.MongoDB` creates a unique `CommitId_Index` on `BucketId + StreamId + CommitId` in `MongoPersistenceEngine`.
- Both sync and async persistence engines convert that index conflict into `DuplicateCommitException`.
- The MongoDB test project links the shared `PersistenceTests.cs` and `PersistenceTests.Async.cs` acceptance suites from the core repo, so the duplicate-commit acceptance scenarios are part of the provider test surface.

### SQL provider family

- `NEventStore.Persistence.SQL` creates a unique commit-ID index on `BucketId + StreamId + CommitId` across every shipped dialect:
  - MsSql
  - MySql
  - PostgreSql
  - Sqlite
  - Oracle
- Both sync and async SQL persistence engines catch `UniqueKeyViolationException`, run an explicit duplicate check by `BucketId + StreamId + CommitId`, and then throw `DuplicateCommitException`.
- The SQL provider test projects link the shared `PersistenceTests.cs` and `PersistenceTests.Async.cs` acceptance suites, so duplicate-commit acceptance scenarios are exercised across the supported SQL backends.

## Stream-Level Implication

`OptimisticEventStream._identifiers` is incomplete by design:

- it only knows about commit IDs for commits that were loaded into the current stream instance
- it does not represent the full stream history after reopening from the middle of a stream
- it does not help at all once a duplicate commit ID exists outside the loaded history window

So the persistence layer is already the real source of truth for duplicate commit detection. The stream-level cache can only provide an earlier exception for the subset of duplicate IDs that were previously read into memory.

## Decision

The recommended follow-up direction is:

- remove `_identifiers` from `OptimisticEventStream` in a separate implementation issue

Reasoning:

- duplicate commit-ID correctness is already enforced by the audited persistence providers
- the current cache is not a complete duplicate detector after partial-history reads
- removing it aligns the implementation with where correctness already lives and removes per-stream memory growth proportional to loaded commit count

This audit issue does not make that change. It only establishes that the follow-up change is defensible.

## Required Follow-Up Tests Before Removal

If `_identifiers` is removed, add these tests before or with that implementation:

### Core stream tests

- Sync: reopen a fully loaded stream and attempt `CommitChanges` with a previously persisted commit ID; assert `DuplicateCommitException` is still surfaced when the persistence layer rejects it.
- Async: same scenario for `CommitChangesAsync`.
- Sync: reopen from a later revision so the original duplicate commit ID is not in the loaded history window, then attempt `CommitChanges` with that earlier commit ID; assert `DuplicateCommitException`.
- Async: same scenario for `CommitChangesAsync`.

### Provider-backed acceptance tests

- Sync: exercise duplicate commit IDs through `IStoreEvents.OpenStream` after reopening a stream from the beginning.
- Async: same scenario through `OpenStreamAsync`.
- Sync: exercise duplicate commit IDs after reopening from a later revision to prove the persistence layer, not the stream cache, is preserving the behavior.
- Async: same scenario through `OpenStreamAsync`.

## Recommendation Status

- Issue `#529`: complete
- `_identifiers` removal: approved as a follow-up optimization, not part of this audit issue
