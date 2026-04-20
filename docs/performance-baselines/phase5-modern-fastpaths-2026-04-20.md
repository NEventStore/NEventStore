# Phase 5 Modern Fast Paths Baseline - 2026-04-20

This is the checked-in seed baseline for issue `#537`. It summarizes the representative gate rows from a full joined BenchmarkDotNet run after the Phase 5 modern target and serializer work through `#536`.

## Source Run

- Command: `dotnet .\src\NEventStore.Benchmark\bin\Release\net8.0\NEventStore.Benchmark.dll --filter * --join`
- Executed benchmarks: 66
- Total benchmark time: 00:10:29
- Raw Markdown: `artifacts/benchmarks/results/BenchmarkRun-joined-2026-04-20-18-26-00-report-github.md`
- Raw CSV: `artifacts/benchmarks/results/BenchmarkRun-joined-2026-04-20-18-26-00-report.csv`
- Raw HTML: `artifacts/benchmarks/results/BenchmarkRun-joined-2026-04-20-18-26-00-report.html`

## Environment

- BenchmarkDotNet: 0.15.8
- OS: Windows 11 10.0.26200.8246
- CPU: 13th Gen Intel Core i7-13700K, 24 logical cores, 16 physical cores
- .NET SDK: 10.0.300-preview.0.26177.108
- Runtime: .NET 8.0.26, X64 RyuJIT x86-64-v3

## Gate Rows

| Type | Method | Parameters | Mean | Allocated |
| --- | --- | --- | ---: | ---: |
| `CommitAttemptBenchmarks` | `ConstructCommitAttempt` | `HeaderCount=5`, `EventsPerCommit=100` | 803.0 ns | 2408 B |
| `SerializerRoundTripBenchmarks` | `BinaryRoundTrip` | `EventCount=100`, `PayloadSizeBytes=4096` | 2,039,007.4 ns | 3328031 B |
| `SerializerRoundTripBenchmarks` | `BsonRoundTrip` | `EventCount=100`, `PayloadSizeBytes=4096` | 1,607,547.3 ns | 2973492 B |
| `SerializerRoundTripBenchmarks` | `JsonRoundTrip` | `EventCount=100`, `PayloadSizeBytes=4096` | 821,249.1 ns | 1722646 B |
| `SerializerRoundTripBenchmarks` | `GzipJsonRoundTrip` | `EventCount=100`, `PayloadSizeBytes=4096` | 1,727,243.3 ns | 1156481 B |
| `SerializerRoundTripBenchmarks` | `GzipBinaryRoundTrip` | `EventCount=100`, `PayloadSizeBytes=4096` | 80,861,367.9 ns | 2734469 B |
| `SerializerRoundTripBenchmarks` | `MsgPackRoundTrip` | `EventCount=100`, `PayloadSizeBytes=4096` | 308,341.7 ns | 1941400 B |
| `SerializerRoundTripBenchmarks` | `RijndaelBinaryRoundTrip` | `EventCount=100`, `PayloadSizeBytes=4096` | 2,471,770.7 ns | 3343284 B |
| `AsyncPollingBenchmarks` | `IdlePollAsync` | `CommitCount=1000` | 167.7 ns | 440 B |
| `AsyncPollingBenchmarks` | `CatchUpPollAsync` | `CommitCount=1000` | 6,369.9 ns | 8496 B |
| `StreamOpenBenchmarks` | `OpenPopulatedStream` | `CommitCount=100000` | 7,695,744.4 ns | 5849320 B |
| `StreamOpenBenchmarks` | `OpenPopulatedStreamAsync` | `CommitCount=100000` | 7,671,966.7 ns | 6898512 B |
| `StreamWriteLargeBenchmarks` | `AppendSingleEventCommits` | `CommitCount=100000` | 404,506,038.2 ns | 213625295 B |
| `StreamWriteLargeBenchmarks` | `AppendSingleEventCommitsAsync` | `CommitCount=100000` | 356,413,684.0 ns | 242424458 B |
| `InMemoryReadBenchmarks` | `ReadGlobalCheckpointRange` | `CommitCount=100000` | 762,465.3 ns | 400052 B |
| `InMemoryReadBenchmarks` | `ReadBucketCheckpointRange` | `CommitCount=100000` | 843,143.7 ns | 400052 B |
| `InMemoryReadBenchmarks` | `ReadStreamRevisionRange` | `CommitCount=100000` | 4,795.3 ns | 12640 B |

## Notes

BenchmarkDotNet reported minimum-iteration warnings for several stream-open and write scenarios. Treat those rows as directional gate signals, not hard blockers, until repeated scheduled runs establish stable variance.
