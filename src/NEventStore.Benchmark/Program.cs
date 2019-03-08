using BenchmarkDotNet.Running;
using NEventStore.Benchmark.Benchmarks;
using System;

namespace NEventStore.Benchmark
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // var summary = BenchmarkRunner.Run<Md5VsSha256>();
            var summary = BenchmarkRunner.Run<PersistenceBenchmarks>();
        }
    }
}
