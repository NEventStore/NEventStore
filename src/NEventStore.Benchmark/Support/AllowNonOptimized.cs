using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Validators;
using System.Linq;

namespace NEventStore.Benchmark.Support
{
    public class AllowNonOptimized : ManualConfig
    {
        public AllowNonOptimized()
        {
            ArtifactsPath = BenchmarkPaths.ResolveArtifactsPath();
            AddValidator(JitOptimizationsValidator.DontFailOnError); // ALLOW NON-OPTIMIZED DLLS

            AddLogger(DefaultConfig.Instance.GetLoggers().ToArray()); // manual config has no loggers by default
            AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray()); // manual config has no columns by default
        }
    }
}
