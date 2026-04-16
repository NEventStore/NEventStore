using System.IO;

namespace NEventStore.Benchmark.Support
{
    internal static class BenchmarkPaths
    {
        internal static string ResolveArtifactsPath()
        {
            for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            {
                if (!File.Exists(Path.Combine(current.FullName, "src", "NEventStore.Core.sln")))
                {
                    continue;
                }

                return Path.Combine(current.FullName, "artifacts", "benchmarks");
            }

            return Path.Combine(AppContext.BaseDirectory, "BenchmarkDotNet.Artifacts");
        }
    }
}
