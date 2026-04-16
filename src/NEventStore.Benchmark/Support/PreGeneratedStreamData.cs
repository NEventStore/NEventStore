using System.Globalization;

namespace NEventStore.Benchmark.Support
{
    internal sealed class PreGeneratedStreamData
    {
        private PreGeneratedStreamData(Guid[] commitIds, SomeDomainEvent[] eventBodies)
        {
            CommitIds = commitIds;
            EventBodies = eventBodies;
        }

        internal Guid[] CommitIds { get; }

        internal SomeDomainEvent[] EventBodies { get; }

        internal static PreGeneratedStreamData Create(int commitCount)
        {
            var commitIds = new Guid[commitCount];
            var eventBodies = new SomeDomainEvent[commitCount];

            for (var i = 0; i < commitCount; i++)
            {
                commitIds[i] = Guid.NewGuid();
                eventBodies[i] = new SomeDomainEvent { Value = i.ToString(CultureInfo.InvariantCulture) };
            }

            return new PreGeneratedStreamData(commitIds, eventBodies);
        }
    }
}
