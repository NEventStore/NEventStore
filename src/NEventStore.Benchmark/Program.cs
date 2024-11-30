using BenchmarkDotNet.Running;
using NEventStore.Benchmark.Benchmarks;

namespace NEventStore.Benchmark;

public static class Program
{
    public static void Main(string[] _)
    {
        BenchmarkRunner.Run<PersistenceBenchmarks>();
    }
}