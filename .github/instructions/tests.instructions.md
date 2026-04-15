---
applyTo: "src/**/*.Tests/**,src/**/Tests/**,src/NEventStore.Persistence.AcceptanceTests/**"
description: "Test conventions for NEventStore: BDD-style SpecificationBase, NUnit active config, FakeItEasy mocks, FluentAssertions."
---

# Test Conventions

## Test Framework
- The active test framework is **NUnit**, selected by the `DefineConstants=NUNIT` compile constant in every test `.csproj`. Do not add `XUNIT` or `MSTEST` test attributes without also adding the corresponding `#if` guards.
- `[Fact]` and `[Then]` are aliased to `[Test]` under the `NUNIT` compilation path. Use `[Fact]` for assertion methods; do not mix in bare `[Test]` attributes.
- See [docs/Testing.md](../../docs/Testing.md) for the full framework-switching procedure.

## BDD Structure
- All scenario classes inherit from `SpecificationBase` (found in `src/NEventStore.Persistence.AcceptanceTests/BDD/NUnit/SpecificationBase.cs`).
- The three phases must be in dedicated overrides:
  - `Context()` / `ContextAsync()` — arrange state
  - `Because()` / `BecauseAsync()` — act (one operation per scenario)
  - `[Fact]` methods — assert one logical outcome each
- Class names describe the scenario: `when_<situation>` or `using_<component>`.
- `[OneTimeSetUp]` and `[OneTimeTearDown]` are owned by `SpecificationBase`; do not re-declare them in subclasses.

## Mocking & Assertions
- Use **FakeItEasy** (`A.Fake<T>()`, `A.CallTo(…).Returns(…)`) for all test doubles. Do not introduce NSubstitute or Moq.
- Use **FluentAssertions** (`.Should().Be(…)`, `.Should().Throw<TException>()`) for all assertions. Do not use bare `Assert.*` calls except where existing tests already do for NUnit compatibility.
- To assert an exception thrown in `Because()`, capture it with `Catch.Exception(() => …)` (see `src/NEventStore.Persistence.AcceptanceTests/Catch.cs`) and assert on the captured variable in a `[Fact]` method.

## Acceptance Tests (Persistence Engine)
- Acceptance tests in `NEventStore.Persistence.AcceptanceTests` test persistence contract compliance. They inherit from `PersistenceEngineConcern`, which wires up a real (or in-memory) `IPersistStreams` via `ConfigurationExtensions`.
- Use `ExtensionMethods.BuildAttempt(streamId)` and `BuildNextAttempt(previous)` to construct `CommitAttempt` objects; do not build them by hand.
- Helper extension methods `CommitSingle`, `CommitNext`, `CommitMany` (and their `Async` variants) provide standard arrange patterns. Prefer them over ad-hoc commit sequences.

## Unit Tests (Core)
- Unit tests for `OptimisticEventStore` and `OptimisticEventStream` live in `src/NEventStore.Tests/`.
- Wire up an isolated store via `TestableWireup` (exposes the internal `NanoContainer`).
- Mock `IPersistStreams` with FakeItEasy instead of using `InMemoryPersistenceEngine` unless the test actually requires real persistence semantics.

## Pitfalls
- `[OneTimeSetUp]` exceptions in NUnit do not report cleanly. `SpecificationBase` works around this by saving the exception and re-throwing it in a per-test `[SetUp]` method (`CheckForTestFixtureFailure`). Preserve this pattern.
- Never mark test scenario classes `static` or `sealed`; NUnit needs to instantiate them.
