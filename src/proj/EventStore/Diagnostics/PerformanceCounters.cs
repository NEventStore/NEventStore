namespace EventStore.Diagnostics
{
    using System;
    using System.Diagnostics;

    internal class PerformanceCounters : IDisposable
	{
		public void CountCommit(int eventsCount, long elapsedMilliseconds)
		{
			this.totalCommits.Increment();
			this.commitsRate.Increment();
			this.avgCommitDuration.IncrementBy(elapsedMilliseconds);
			this.avgCommitDurationBase.Increment();
			this.totalEvents.IncrementBy(eventsCount);
			this.eventsRate.IncrementBy(eventsCount);
			this.undispatchedCommits.Increment();
		}
		public void CountSnapshot()
		{
			this.totalSnapshots.Increment();
			this.snapshotsRate.Increment();
		}
		public void CountCommitDispatched()
		{
			this.undispatchedCommits.Decrement();
		}

		static PerformanceCounters()
		{
			if (PerformanceCounterCategory.Exists(CategoryName))
				return;

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
		public PerformanceCounters(string instanceName)
		{
			this.totalCommits = new PerformanceCounter(CategoryName, TotalCommitsName, instanceName, false);
			this.commitsRate = new PerformanceCounter(CategoryName, CommitsRateName, instanceName, false);
			this.avgCommitDuration = new PerformanceCounter(CategoryName, AvgCommitDuration, instanceName, false);
			this.avgCommitDurationBase = new PerformanceCounter(CategoryName, AvgCommitDurationBase, instanceName, false);
			this.totalEvents = new PerformanceCounter(CategoryName, TotalEventsName, instanceName, false);
			this.eventsRate = new PerformanceCounter(CategoryName, EventsRateName, instanceName, false);
			this.totalSnapshots = new PerformanceCounter(CategoryName, TotalSnapshotsName, instanceName, false);
			this.snapshotsRate = new PerformanceCounter(CategoryName, SnapshotsRateName, instanceName, false);
			this.undispatchedCommits = new PerformanceCounter(CategoryName, UndispatchedQueue, instanceName, false);
		}
		~PerformanceCounters()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			this.totalCommits.Dispose();
			this.commitsRate.Dispose();
			this.avgCommitDuration.Dispose();
			this.avgCommitDurationBase.Dispose();
			this.totalEvents.Dispose();
			this.eventsRate.Dispose();
			this.totalSnapshots.Dispose();
			this.snapshotsRate.Dispose();
			this.undispatchedCommits.Dispose();
		}

		private const string CategoryName = "EventStore";
		private const string TotalCommitsName = "Total Commits";
		private const string CommitsRateName = "Commits/Sec";
		private const string AvgCommitDuration = "Average Commit Duration";
		private const string AvgCommitDurationBase = "Average Commit Duration Base";
		private const string TotalEventsName = "Total Events";
		private const string EventsRateName = "Events/Sec";
		private const string TotalSnapshotsName = "Total Snapshots";
		private const string SnapshotsRateName = "Snapshots/Sec";
		private const string UndispatchedQueue = "Undispatched Queue Length";
		private readonly PerformanceCounter totalCommits;
		private readonly PerformanceCounter commitsRate;
		private readonly PerformanceCounter avgCommitDuration;
		private readonly PerformanceCounter avgCommitDurationBase;
		private readonly PerformanceCounter totalEvents;
		private readonly PerformanceCounter eventsRate;
		private readonly PerformanceCounter totalSnapshots;
		private readonly PerformanceCounter snapshotsRate;
		private readonly PerformanceCounter undispatchedCommits;
	}
}
