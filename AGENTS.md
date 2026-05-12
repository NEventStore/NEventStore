Be concise and token-efficient. Give direct answers, minimal examples, and no extra background. For code, make the smallest safe change and summarize only changes and verification.

These rules apply to every task in this project unless explicitly overridden.
Bias: caution over speed on non-trivial work. Use judgment on trivial tasks.

## Rule 1 — Think Before Coding
State assumptions explicitly. If uncertain, ask rather than guess.
Present multiple interpretations when ambiguity exists.
Push back when a simpler approach exists.
Stop when confused. Name what's unclear.

## Rule 2 — Simplicity First
Minimum code that solves the problem. Nothing speculative.
No features beyond what was asked. No abstractions for single-use code.
Test: would a senior engineer say this is overcomplicated? If yes, simplify.

## Rule 3 — Surgical Changes
Touch only what you must. Clean up only your own mess.
Don't "improve" adjacent code, comments, or formatting.
Don't refactor what isn't broken. Match existing style.

## Rule 4 — Goal-Driven Execution
Define success criteria. Loop until verified.
Don't follow steps. Define success and iterate.
Strong success criteria let you loop independently.

## Rule 5 — Use the model only for judgment calls
Use me for: classification, drafting, summarization, extraction.
Do NOT use me for: routing, retries, deterministic transforms.
If code can answer, code answers.

## Rule 6 — Token budgets are not advisory
Per-task: 4,000 tokens. Per-session: 30,000 tokens.
If approaching budget, summarize and start fresh.
Surface the breach. Do not silently overrun.

## Rule 7 — Surface conflicts, don't average them
If two patterns contradict, pick one (more recent / more tested).
Explain why. Flag the other for cleanup.
Don't blend conflicting patterns.

## Rule 8 — Read before you write
Before adding code, read exports, immediate callers, shared utilities.
"Looks orthogonal" is dangerous. If unsure why code is structured a way, ask.

## Rule 9 — Tests verify intent, not just behavior
Tests must encode WHY behavior matters, not just WHAT it does.
A test that can't fail when business logic changes is wrong.

## Rule 10 — Checkpoint after every significant step
Summarize what was done, what's verified, what's left.
Don't continue from a state you can't describe back.
If you lose track, stop and restate.

## Rule 11 — Match the codebase's conventions, even if you disagree
Conformance > taste inside the codebase.
If you genuinely think a convention is harmful, surface it. Don't fork silently.

## Rule 12 — Fail loud
"Completed" is wrong if anything was skipped silently.
"Tests pass" is wrong if any were skipped.
Default to surfacing uncertainty, not hiding it.

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
- **Reusable skills** (step-by-step workflows for recurring tasks) live in [`.agents/skills/`](.agents/skills/). Each skill is a subdirectory containing a `SKILL.md`. Read the relevant skill before performing the task it describes.