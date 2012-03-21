using System.Diagnostics;
using EventStore.Persistence;

namespace EventStore.Diagnostics
{
	public class PerformanceTrackingPersistenceDecorator	: PersistenceEngineDecoratorBase
	{
		private readonly PerformanceCounters _performanceCounters;

		public PerformanceTrackingPersistenceDecorator(IPersistStreams persistence, string instanceName) : base(persistence)
		{
			_performanceCounters = new PerformanceCounters(instanceName);
		}

		public override void Commit(Commit attempt)
		{
			var sw = Stopwatch.StartNew();
			base.Commit(attempt);
			sw.Stop();

			_performanceCounters.CountCommit(attempt.Events.Count, sw.ElapsedMilliseconds);
		}

		public override bool AddSnapshot(Snapshot snapshot)
		{
			var result = base.AddSnapshot(snapshot);
			if (result)
			{
				_performanceCounters.CountSnapshot();
			}	
			return result;
		}

		public override void MarkCommitAsDispatched(Commit commit)
		{
			base.MarkCommitAsDispatched(commit);
			_performanceCounters.CountCommitDispatched();
		}
	}
}
