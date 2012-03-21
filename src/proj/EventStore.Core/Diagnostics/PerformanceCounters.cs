using System.Diagnostics;

namespace EventStore.Diagnostics
{
	internal class PerformanceCounters
	{
		private const string CategoryName = "EventStore";
		private const string TotalCommitsName = "Total Commits";
		private const string CommitsRateName = "Commits/Sec";
		private const string AvgCommitDuration = "Avg Commit Duration";
		private const string AvgCommitDurationBase = "Avg Commit Duration Base";
		private const string TotalEventsName = "Total Events";
		private const string EventsRateName = "Events/Sec";
		private const string TotalSnapshotsName = "Total Snapshots";
		private const string SnapshotsRateName = "Snapshots/Sec";
		private const string UndispatchedQueue = "Undispatched Queue Length";

		private readonly PerformanceCounter _totalCommits;
		private readonly PerformanceCounter _commitsRate;
		private readonly PerformanceCounter _avgCommitDuration;
		private readonly PerformanceCounter _avgCommitDurationBase;
		private readonly PerformanceCounter _totalEvents;
		private readonly PerformanceCounter _eventsRate;
		private readonly PerformanceCounter _totalSnapshots;
		private readonly PerformanceCounter _snapshotsRate;
		private readonly PerformanceCounter _undispatchedCommits;

		static PerformanceCounters()
		{
			if (!PerformanceCounterCategory.Exists(CategoryName))
			{
				var counters = new CounterCreationDataCollection
				{
					new CounterCreationData(TotalCommitsName, "Total number of commits persisted", PerformanceCounterType.NumberOfItems32),
					new CounterCreationData(CommitsRateName, "Rate of commits persisted per second", PerformanceCounterType.RateOfCountsPerSecond32),
					new CounterCreationData(AvgCommitDuration, "Average duration for each commit", PerformanceCounterType.AverageTimer32),
					new CounterCreationData(AvgCommitDurationBase, "Average duration base for each commit", PerformanceCounterType.AverageBase),
					new CounterCreationData(TotalEventsName, "Total number of events persisted", PerformanceCounterType.NumberOfItems32),
					new CounterCreationData(EventsRateName, "Rate of events persisted per second", PerformanceCounterType.RateOfCountsPerSecond32),
					new CounterCreationData(TotalSnapshotsName, "Total number of snapshots persisted", PerformanceCounterType.NumberOfItems32),
					new CounterCreationData(SnapshotsRateName, "Rate of snapshots persisted per second", PerformanceCounterType.RateOfCountsPerSecond32),
					new CounterCreationData(UndispatchedQueue, "Undispatched commit queue length", PerformanceCounterType.CountPerTimeInterval32)
				};

				// TODO: add other useful counts such as:
				//
				//	* Total Commit Bytes
				//  * Average Commit Bytes
				//  * Total Queries
				//  * Queries Per Second
				//  * Average Query Duration
				//  * Commits per Query (Total / average / per second)
				//  * Events per Query (Total / average / per second)
				//
				// Some of these will involve hooking into other parts of the EventStore

				PerformanceCounterCategory.Create(CategoryName, "EventStore Event-Sourcing Persistence", PerformanceCounterCategoryType.MultiInstance, counters);
			}
		}

		public PerformanceCounters(string instanceName)
		{
			_totalCommits = new PerformanceCounter(CategoryName, TotalCommitsName, instanceName, false);
			_commitsRate = new PerformanceCounter(CategoryName, CommitsRateName, instanceName, false);
			_avgCommitDuration = new PerformanceCounter(CategoryName, AvgCommitDuration, instanceName, false);
			_avgCommitDurationBase = new PerformanceCounter(CategoryName, AvgCommitDurationBase, instanceName, false);
			_totalEvents = new PerformanceCounter(CategoryName, TotalEventsName, instanceName, false);
			_eventsRate = new PerformanceCounter(CategoryName, EventsRateName, instanceName, false);
			_totalSnapshots = new PerformanceCounter(CategoryName, TotalSnapshotsName, instanceName, false);
			_snapshotsRate = new PerformanceCounter(CategoryName, SnapshotsRateName, instanceName, false);
			_undispatchedCommits = new PerformanceCounter(UndispatchedQueue, SnapshotsRateName, instanceName, false);
		}

		public void CountCommit(int eventsCount, long elapsedMilliseconds)
		{
			_totalCommits.Increment();
			_commitsRate.Increment();
			_avgCommitDuration.IncrementBy(elapsedMilliseconds);
			_avgCommitDurationBase.Increment();
			_totalEvents.IncrementBy(eventsCount);
			_eventsRate.IncrementBy(eventsCount);
			_undispatchedCommits.Increment();
		}

		public void CountSnapshot()
		{
			_totalSnapshots.Increment();
			_snapshotsRate.Increment();
		}

		public void CountCommitDispatched()
		{
			_undispatchedCommits.Decrement();
		}
	}
}
