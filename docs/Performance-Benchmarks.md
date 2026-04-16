# Performance Benchmarks

This document defines the focused benchmark regression set introduced for performance foundation issue `#523`.

## Regression Set

The benchmark project now covers these scenario groups:

- `StreamOpenBenchmarks`: empty stream open, populated stream open, sync and async.
- `StreamWriteBenchmarks`: batched append-and-commit loops for smaller single-event commit scenarios, sync and async.
- `StreamWriteLargeBenchmarks`: batched append-and-commit loops for larger single-event commit scenarios, sync and async.
- `CommitAttemptBenchmarks`: commit-attempt construction cost across event and header counts.
- `InMemoryReadBenchmarks`: global checkpoint reads, bucket checkpoint reads, and per-stream revision reads.
- `AsyncPollingBenchmarks`: idle poll cost and catch-up poll cost through `AsyncPollingClient`.
- `SerializerRoundTripBenchmarks`: serializer-only round trips for JSON and gzip-wrapped JSON payloads.

## Running

Build the benchmark project from the solution root:

```powershell
dotnet build .\src\NEventStore.Benchmark\NEventStore.Benchmark.csproj -c Release
```

List the available benchmarks:

```powershell
dotnet .\src\NEventStore.Benchmark\bin\Release\net8.0\NEventStore.Benchmark.dll --list flat
```

Run the full regression set:

```powershell
dotnet .\src\NEventStore.Benchmark\bin\Release\net8.0\NEventStore.Benchmark.dll
```

Run a focused subset:

```powershell
dotnet .\src\NEventStore.Benchmark\bin\Release\net8.0\NEventStore.Benchmark.dll --filter *InMemoryReadBenchmarks*
```

## Baseline Outputs

BenchmarkDotNet artifacts are written to:

`artifacts/benchmarks/`

The generated baseline reports are exported under:

`artifacts/benchmarks/results/`

The Markdown exporter is intended to be the review-friendly baseline for before/after comparisons, while the CSV and HTML exporters provide machine-readable and richer local inspection formats.
