
namespace NEventStore.Persistence.MongoDB.Tests.AcceptanceTests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;

	using System.Diagnostics;
	using NEventStore.Client;
	using NEventStore.Diagnostics;
	using NEventStore.Persistence.AcceptanceTests;
	using NEventStore.Persistence.AcceptanceTests.BDD;
	using Xunit;
	using Xunit.Should;

	public class Observer : IObserver<ICommit>
	{
		private int _counter;

		public int Counter
		{
			get { return _counter; }
		}

		private string _lastCommit;

		public void OnNext(ICommit value)
		{
			if (value.CheckpointToken != _lastCommit)
				_counter++;

			_lastCommit = value.CheckpointToken;
		}

		public void OnError(Exception error)
		{
		}

		public void OnCompleted()
		{
		}
	}

	public class when_a_reader_observe_commits_from_multiple_parallel_writers : SpecificationBase
	{
		private const int Iterations = 10000;
		private const int ParallelWriters = 4;
		private const int PollingInterval = 1;
		readonly IList<IPersistStreams> _writers = new List<IPersistStreams>();
		private PollingClient _client;
		private Observer _observer;
		private IObserveCommits _observeCommits;

		protected override void Context()
		{
			for (int c = 1; c <= ParallelWriters; c++)
			{
				var client = new AcceptanceTestMongoPersistenceFactory().Build();

				if (c == 1)
				{
					client.Drop();
					client.Initialize();
				}

				_writers.Add(client);
			}

			_observer = new Observer();

			var reader = new AcceptanceTestMongoPersistenceFactory().Build();
			_client = new PollingClient(reader, PollingInterval);

			_observeCommits = _client.ObserveFrom(null);
			_observeCommits.Subscribe(_observer);
			_observeCommits.Start();
		}

		protected override void Because()
		{
			Parallel.ForEach(
				Enumerable.Range(1, Iterations),
				new ParallelOptions() { MaxDegreeOfParallelism = ParallelWriters },
				i => _writers[i % ParallelWriters].Commit(Guid.NewGuid().ToString().BuildAttempt())
			);

			Thread.Sleep(1000);
			_observeCommits.Dispose();
		}

		[Fact]
		public void should_never_miss_a_commit()
		{
			_observer.Counter.ShouldBe(Iterations);
		}

		protected override void Cleanup()
		{
			for (int c = 0; c < ParallelWriters; c++)
			{
				if (c == ParallelWriters - 1)
					_writers[c].Drop();

				_writers[c].Dispose();
			}
		}
	}

	public class when_a_reader_observe_commits_from_a_lot_of_writers : SpecificationBase
	{
		private const int IterationPerWriter = 5;
		private const int ParallelWriters = 200;
		private const int PollingInterval = 1;
		readonly IList<IPersistStreams> _writers = new List<IPersistStreams>();
		private PollingClient _client;
		private Observer _observer;
		private IObserveCommits _observeCommits;

		protected override void Context()
		{
			for (int c = 1; c <= ParallelWriters; c++)
			{
				var client = new AcceptanceTestMongoPersistenceFactory().Build();

				if (c == 1)
				{
					client.Drop();
					client.Initialize();
				}

				_writers.Add(client);
			}

			_observer = new Observer();

			var reader = new AcceptanceTestMongoPersistenceFactory().Build();
			_client = new PollingClient(reader, PollingInterval);

			_observeCommits = _client.ObserveFrom(null);
			_observeCommits.Subscribe(_observer);
			_observeCommits.Start();
		}

		protected override void Because()
		{
			var start = new ManualResetEventSlim(false);
			var stop = new ManualResetEventSlim(false);
			long counter = 0;
			for (int t = 0; t < ParallelWriters; t++)
			{
				int t1 = t;
				var runner = new Thread(() =>{
					start.Wait();
					for (int c = 0; c < IterationPerWriter; c++)
					{
						_writers[t1].Commit(Guid.NewGuid().ToString().BuildAttempt());
					}
					Interlocked.Increment(ref counter);
					if (counter == ParallelWriters)
					{
						stop.Set();
					}
				});

				runner.Start();
			}
			start.Set();
			stop.Wait();

			Thread.Sleep(500);
			_observeCommits.Dispose();
		}

		[Fact]
		public void should_never_miss_a_commit()
		{
			_observer.Counter.ShouldBe(IterationPerWriter * ParallelWriters);
		}

		protected override void Cleanup()
		{
			for (int c = 0; c < ParallelWriters; c++)
			{
				if (c == ParallelWriters - 1)
					_writers[c].Drop();

				_writers[c].Dispose();
			}
		}
	}
}
