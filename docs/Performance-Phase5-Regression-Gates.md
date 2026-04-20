# Phase 5 Benchmark Regression Gates

This document defines the benchmark governance for Phase 5 optional modern fast paths in umbrella issue `#546`, starting with issue `#537`.

The goal is to keep benchmark evidence reviewable without making noisy microbenchmarks hard CI blockers before the project has enough repeated-run data.

## Baseline Artifacts

Checked-in baseline artifacts live under:

`docs/performance-baselines/`

The current Phase 5 seed baseline is:

- [phase5-modern-fastpaths-2026-04-20.md](performance-baselines/phase5-modern-fastpaths-2026-04-20.md)

Raw BenchmarkDotNet outputs remain under `artifacts/benchmarks/results/` and are intentionally ignored by git. The checked-in baseline records the source report names so a reviewer can reproduce the raw Markdown, CSV, and HTML files locally.

## Reproduction Command

Run from the solution root after building the benchmark project in Release:

```powershell
dotnet build .\src\NEventStore.Benchmark\NEventStore.Benchmark.csproj -c Release
dotnet .\src\NEventStore.Benchmark\bin\Release\net8.0\NEventStore.Benchmark.dll --filter * --join
```

Use the joined report for gate comparisons because it preserves one machine/environment header and one comparable table across all benchmark classes.

## Gate Scenarios

These scenarios are the minimal Phase 5 gate set. They intentionally cover the code paths affected by modern target additions and current fast-path work rather than every benchmark parameter combination.

- `SerializerRoundTripBenchmarks` with `EventCount=100` and `PayloadSizeBytes=4096`: representative allocation-heavy serializer payloads across binary, BSON, JSON, MessagePack, gzip, and Rijndael wrappers.
- `AsyncPollingBenchmarks.IdlePollAsync` and `CatchUpPollAsync` with `CommitCount=1000`: modern async dispatch and catch-up behavior.
- `CommitAttemptBenchmarks.ConstructCommitAttempt` with `HeaderCount=5` and `EventsPerCommit=100`: commit construction allocation pressure.
- `StreamOpenBenchmarks.OpenPopulatedStream` and `OpenPopulatedStreamAsync` with `CommitCount=100000`: read/materialization hot paths where modern runtime changes can affect iteration and collection overhead.
- `StreamWriteLargeBenchmarks.AppendSingleEventCommits` and `AppendSingleEventCommitsAsync` with `CommitCount=100000`: large write-path allocation and latency pressure.
- `InMemoryReadBenchmarks` with `CommitCount=100000`: read index and checkpoint traversal paths, used as a reference signal even though in-memory persistence is not the primary production provider.

## Threshold Policy

Until repeated CI or scheduled benchmark runs establish stable variance, do not fail PRs automatically on a single benchmark run. Use these review thresholds instead:

- Treat a mean latency increase above 10% as a regression candidate when it is outside normal run noise or repeated in a second local run.
- Treat an allocation increase above 5% as a regression candidate because allocation measurements are usually less noisy than wall-clock timings.
- Treat any new Gen2 collection in a gate scenario that previously had none as a regression candidate unless the change is expected and documented.
- Ignore timing-only changes below 5% unless the same direction repeats across several related scenarios.
- Prefer the CSV exporter for calculations and the GitHub Markdown exporter for review comments.

## Review Report Format

Every Phase 5 performance PR should include:

- benchmark command and commit SHA for before and after runs
- machine/runtime summary copied from the BenchmarkDotNet header
- changed gate scenarios with before mean, after mean, percent delta, before allocation, after allocation, and percent delta
- explicit note for any benchmark with high variance, outliers, or minimum-iteration warnings
- correctness validation command, usually `dotnet test .\src\NEventStore.Core.sln -c Release --no-build`

## Exception Workflow

A regression candidate can be accepted only when the PR explains why the tradeoff is intentional. Common acceptable reasons are compatibility fixes, correctness fixes, or improved behavior in a more important paired scenario. The explanation must name the affected gate row and link to the supporting test or benchmark evidence.
