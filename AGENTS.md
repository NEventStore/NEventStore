# Project Guidelines

## Scope
- NEventStore is an event sourcing persistence library for DDD and CQRS-style systems. Treat stream semantics, commit ordering, optimistic concurrency, snapshots, and checkpoint handling as behavioral contracts, not implementation details.
- Prefer changes that preserve the existing public API shape across synchronous and asynchronous paths unless the task explicitly requires a breaking change.

## Architecture
- Core event store behavior lives in `src/NEventStore/`. Start with `IStoreEvents`, `IEventStream`, `OptimisticEventStore`, and `OptimisticEventStream` when changing stream or commit behavior.
- Persistence implementations sit behind `IPersistStreams`; pipeline interception is implemented through `IPipelineHook` and `IPipelineHookAsync`.
- Composition uses fluent wireup extensions such as `PersistenceWireupExtensions`, `SerializationWireupExtensions`, `LoggingWireupExtensions`, and `EventUpconverterWireupExtensions`. Follow that extension pattern when adding new configuration surfaces.
- Serialization, polling, examples, and benchmarks live in separate projects under `src/` and should stay decoupled from core stream semantics.

## Build And Test
- Build from the solution root with `dotnet restore ./src/NEventStore.Core.sln --verbosity m` and `dotnet build ./src/NEventStore.Core.sln -c Release --no-restore`.
- Run tests with `dotnet test ./src/NEventStore.Core.sln -c Release --no-build`.
- On Windows, `build.ps1` is the canonical packaging flow: restore tools, run GitVersion, build, optionally test, then pack NuGet artifacts.
- Do not manually edit assembly version metadata. Versioning is updated from Git tags by GitVersion during the build.

## Conventions
- `IStoreEvents` implementations are intended to be multi-thread safe; `IEventStream` instances are explicitly single-threaded. Do not introduce shared-stream usage patterns.
- Preserve optimistic concurrency behavior. Revisions, commit sequences, duplicate commit detection, and commit rejection by pipeline hooks are central domain guarantees.
- If you add or modify async behavior, keep cancellation-token support and avoid changing sync/async behavioral parity unless required.
- This repo enables nullable reference types, implicit usings, and C# 13 via `src/Directory.Build.props`; match the existing style.
- Guard clauses commonly use the local `Guard` helper with expression-based parameter names. Follow existing patterns in nearby code instead of introducing a new validation style.

## Testing Notes
- The repository carries compatibility layers for NUnit, xUnit, and MSTest, but the active configuration is NUnit through `DefineConstants=NUNIT` in test projects.
- Acceptance tests use a BDD-style `SpecificationBase` with `Context`, `Because`, and assertion methods. Preserve that structure when expanding behavioral coverage.
- Avoid changing test framework selection unless the task explicitly requires it; doing so requires coordinated `.csproj` and conditional-compilation updates across multiple test projects.

## Documentation
- Link to existing docs instead of duplicating them in code or instructions.
- Use `Readme.md` for repository overview and local build guidance.
- Use `docs/Testing.md` for the test-framework strategy and its constraints.
- Use `Changelog.md` to understand versioned behavior changes before altering compatibility-sensitive code.

## Supplementary Agent Instructions
Before making changes, check for additional rules that override the general conventions above when in conflict:

- **Scoped instructions** live in [`.github/instructions/`](.github/instructions/). Each `*.instructions.md` file declares the file glob it applies to in its `applyTo` front-matter field. Read any file whose glob matches the area you are changing.
- **Reusable skills** (step-by-step workflows for recurring tasks) live in [`.github/skills/`](.github/skills/). Each skill is a subdirectory containing a `SKILL.md`. Read the relevant skill before performing the task it describes.