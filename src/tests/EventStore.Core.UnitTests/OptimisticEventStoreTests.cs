#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using Moq;
	using Persistence;
	using It = Machine.Specifications.It;
    using System.Threading;

	[Subject("OptimisticEventStore")]
	public class when_creating_a_new_stream : using_persistence
	{
		static IEventStream stream;

		Because of = () =>
			stream = store.CreateStream(streamId);

		It should_return_a_new_stream = () =>
			stream.ShouldNotBeNull();

		It should_return_a_stream_with_the_correct_stream_identifier = () =>
			stream.StreamId.ShouldEqual(streamId);

		It should_return_a_stream_with_a_zero_stream_revision = () =>
			stream.StreamRevision.ShouldEqual(0);

		It should_return_a_stream_with_a_zero_commit_sequence = () =>
			stream.CommitSequence.ShouldEqual(0);

		It should_return_a_stream_with_no_uncommitted_events = () =>
			stream.UncommittedEvents.ShouldBeEmpty();

		It should_return_a_stream_with_no_committed_events = () =>
			stream.CommittedEvents.ShouldBeEmpty();

		It should_return_a_stream_with_empty_headers = () =>
			stream.UncommittedHeaders.ShouldBeEmpty();
	}

	[Subject("OptimisticEventStore")]
	public class when_opening_an_empty_stream_starting_at_revision_zero : using_persistence
	{
		static IEventStream stream;

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, 0, 0)).Returns(new Commit[0]);

		Because of = () =>
			stream = store.OpenStream(streamId, 0, 0);

		It should_return_a_new_stream = () =>
			stream.ShouldNotBeNull();

		It should_return_a_stream_with_the_correct_stream_identifier = () =>
			stream.StreamId.ShouldEqual(streamId);

		It should_return_a_stream_with_a_zero_stream_revision = () =>
			stream.StreamRevision.ShouldEqual(0);

		It should_return_a_stream_with_a_zero_commit_sequence = () =>
			stream.CommitSequence.ShouldEqual(0);

		It should_return_a_stream_with_no_uncommitted_events = () =>
			stream.UncommittedEvents.ShouldBeEmpty();

		It should_return_a_stream_with_no_committed_events = () =>
			stream.CommittedEvents.ShouldBeEmpty();

		It should_return_a_stream_with_empty_headers = () =>
			stream.UncommittedHeaders.ShouldBeEmpty();
	}

	[Subject("OptimisticEventStore")]
	public class when_opening_an_empty_stream_starting_above_revision_zero : using_persistence
	{
		const int MinRevision = 1;
		static Exception thrown;

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, MinRevision, int.MaxValue)).Returns(new Commit[0]);

		Because of = () =>
			thrown = Catch.Exception(() => store.OpenStream(streamId, MinRevision, int.MaxValue));

		It should_throw_a_StreamNotFoundException = () =>
			thrown.ShouldBeOfType<StreamNotFoundException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_opening_a_populated_stream : using_persistence
	{
		const int MinRevision = 17;
		const int MaxRevision = 42;
		static readonly Commit Committed = BuildCommitStub(MinRevision, 1);
		static IEventStream stream;

		Establish context = () =>
		{
			persistence.Setup(x => x.GetFrom(streamId, MinRevision, MaxRevision)).Returns(new[] { Committed });
			pipelineHooks.Add(new Mock<IPipelineHook>());
			pipelineHooks[0].Setup(x => x.Select(Committed)).Returns(Committed);
		};

		Because of = () =>
			stream = store.OpenStream(streamId, MinRevision, MaxRevision);

		It should_invoke_the_underlying_infrastructure_with_the_values_provided = () =>
			persistence.Verify(x => x.GetFrom(streamId, MinRevision, MaxRevision), Times.Once());

		It should_provide_the_commits_to_the_selection_hooks = () =>
			pipelineHooks.ForEach(x => x.Verify(hook => hook.Select(Committed), Times.Once()));

		It should_return_an_event_stream_containing_the_correct_stream_identifer = () =>
			stream.StreamId.ShouldEqual(streamId);
	}

	[Subject("OptimisticEventStore")]
	public class when_opening_a_populated_stream_from_a_snapshot : using_persistence
	{
		const int MaxRevision = int.MaxValue;
		static readonly Snapshot snapshot = new Snapshot(streamId, 42, "snapshot");
		static readonly Commit[] Committed = new[] { BuildCommitStub(42, 0) };

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, 42, MaxRevision)).Returns(Committed);

		Because of = () =>
			store.OpenStream(snapshot, MaxRevision);

		It should_query_the_underlying_storage_using_the_revision_of_the_snapshot = () =>
			persistence.Verify(x => x.GetFrom(streamId, 42, MaxRevision), Times.Once());
	}

	[Subject("OptimisticEventStore")]
	public class when_opening_a_stream_from_a_snapshot_that_is_at_the_revision_of_the_stream_head : using_persistence
	{
		const int HeadStreamRevision = 42;
		const int HeadCommitSequence = 15;
		static readonly Snapshot snapshot = new Snapshot(streamId, HeadStreamRevision, "snapshot");
		static readonly EnumerableCounter Committed = new EnumerableCounter(
			new[] { BuildCommitStub(HeadStreamRevision, HeadCommitSequence) });
		static IEventStream stream;

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, HeadStreamRevision, int.MaxValue)).Returns(Committed);

		Because of = () =>
			stream = store.OpenStream(snapshot, int.MaxValue);

		It should_return_a_stream_with_the_correct_stream_identifier = () =>
			stream.StreamId.ShouldEqual(streamId);

		It should_return_a_stream_with_revision_of_the_stream_head = () =>
			stream.StreamRevision.ShouldEqual(HeadStreamRevision);

		It should_return_a_stream_with_a_commit_sequence_of_the_stream_head = () =>
			stream.CommitSequence.ShouldEqual(HeadCommitSequence);

		It should_return_a_stream_with_no_committed_events = () =>
			stream.CommittedEvents.Count.ShouldEqual(0);

		It should_return_a_stream_with_no_uncommitted_events = () =>
			stream.UncommittedEvents.Count.ShouldEqual(0);

		It should_only_enumerate_the_set_of_commits_once = () =>
			Committed.GetEnumeratorCallCount.ShouldEqual(1);
	}

	[Subject("OptimisticEventStore")]
	public class when_reading_from_revision_zero : using_persistence
	{
		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, 0, int.MaxValue)).Returns(new Commit[] { });

		Because of = () =>
			((ICommitEvents)store).GetFrom(streamId, 0, int.MaxValue).ToList();

		It should_pass_a_revision_range_to_the_persistence_infrastructure = () =>
			persistence.Verify(x => x.GetFrom(streamId, 0, int.MaxValue), Times.Once());
	}

	[Subject("OptimisticEventStore")]
	public class when_reading_up_to_revision_revision_zero : using_persistence
	{
		static readonly Commit Committed = BuildCommitStub(1, 1);

		Establish context = () => persistence
			.Setup(x => x.GetFrom(streamId, 0, int.MaxValue))
			.Returns(new[] { Committed });

		Because of = () =>
			store.OpenStream(streamId, 0, 0);

		It should_pass_the_maximum_possible_revision_to_the_persistence_infrastructure = () =>
			persistence.Verify(x => x.GetFrom(streamId, 0, int.MaxValue), Times.Once());
	}

	[Subject("OptimisticEventStore")]
	public class when_reading_from_a_null_snapshot : using_persistence
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => store.OpenStream(null, int.MaxValue));

		It should_throw_an_ArgumentNullException = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_reading_from_a_snapshot_up_to_revision_revision_zero : using_persistence
	{
		static readonly Snapshot snapshot = new Snapshot(streamId, 1, "snapshot");
		static readonly Commit Committed = BuildCommitStub(1, 1);

		Establish context = () => persistence
			.Setup(x => x.GetFrom(streamId, snapshot.StreamRevision, int.MaxValue))
			.Returns(new[] { Committed });

		Because of = () =>
			store.OpenStream(snapshot, 0);

		It should_pass_the_maximum_possible_revision_to_the_persistence_infrastructure = () =>
			persistence.Verify(x => x.GetFrom(streamId, snapshot.StreamRevision, int.MaxValue), Times.Once());
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_a_null_attempt_back_to_the_stream : using_persistence
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => ((ICommitEvents)store).Commit(null));

		It should_throw_an_ArgumentNullException = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_with_an_unidentified_attempt_back_to_the_stream : using_persistence
	{
		static readonly Guid emptyIdentifier = Guid.Empty;
		static readonly Commit unidentified = BuildCommitStub(emptyIdentifier);
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => ((ICommitEvents)store).Commit(unidentified));

		It should_throw_an_ArgumentException = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_the_number_of_commits_is_greater_than_the_number_of_revisions : using_persistence
	{
		const int StreamRevision = 1;
		const int CommitSequence = 2; // should never be greater than StreamRevision.
		static readonly Commit corrupt = BuildCommitStub(StreamRevision, CommitSequence);
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => ((ICommitEvents)store).Commit(corrupt));

		It should_throw_a_StorageException = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_with_a_nonpositive_commit_sequence_back_to_the_stream : using_persistence
	{
		const int StreamRevision = 1;
		const int InvalidCommitSequence = 0;
		static readonly Commit invalidCommitSequence = BuildCommitStub(StreamRevision, InvalidCommitSequence);
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => ((ICommitEvents)store).Commit(invalidCommitSequence));

		It should_throw_an_ArgumentException = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_with_a_non_positive_stream_revision_back_to_the_stream : using_persistence
	{
		const int InvalidStreamRevision = 0;
		const int CommitSequence = 1;
		static readonly Commit invalidStreamRevision = BuildCommitStub(InvalidStreamRevision, CommitSequence);
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => ((ICommitEvents)store).Commit(invalidStreamRevision));

		It should_throw_an_ArgumentException = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_an_empty_attempt_to_a_stream : using_persistence
	{
		static readonly Commit attemptWithNoEvents = BuildCommitStub(Guid.NewGuid());

		Establish context = () =>
			persistence.Setup(x => x.Commit(attemptWithNoEvents));

		Because of = () =>
			((ICommitEvents)store).Commit(attemptWithNoEvents);

		It should_drop_the_commit_provided = () =>
			persistence.Verify(x => x.Commit(attemptWithNoEvents), Times.AtMost(0));
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_with_a_valid_and_populated_attempt_to_a_stream : using_persistence
	{
		static readonly Commit populatedAttempt = BuildCommitStub(1, 1);

		Establish context = () =>
		{
			persistence.Setup(x => x.Commit(populatedAttempt));

			pipelineHooks.Add(new Mock<IPipelineHook>());
			pipelineHooks[0].Setup(x => x.PreCommit(populatedAttempt)).Returns(true);
			pipelineHooks[0].Setup(x => x.PostCommit(populatedAttempt));
		};

		Because of = () =>
			((ICommitEvents)store).Commit(populatedAttempt);

		It should_provide_the_commit_to_the_precommit_hooks = () =>
			pipelineHooks.ForEach(x => x.Verify(hook => hook.PreCommit(populatedAttempt), Times.Once()));

		It should_provide_the_commit_attempt_to_the_configured_persistence_mechanism = () =>
			persistence.Verify(x => x.Commit(populatedAttempt), Times.Once());

		It should_provide_the_commit_to_the_postcommit_hooks = () =>
			pipelineHooks.ForEach(x => x.Verify(hook => hook.PostCommit(populatedAttempt), Times.Once()));
	}

	[Subject("OptimisticEventStore")]
	public class when_a_precommit_hook_rejects_a_commit : using_persistence
	{
		static readonly Commit attempt = BuildCommitStub(1, 1);

		Establish context = () =>
		{
			pipelineHooks.Add(new Mock<IPipelineHook>());
			pipelineHooks[0].Setup(x => x.PreCommit(attempt)).Returns(false);
		};

		Because of = () =>
			((ICommitEvents)store).Commit(attempt);

		It should_not_call_the_underlying_infrastructure = () =>
			persistence.Verify(x => x.Commit(attempt), Times.Never());

		It should_not_provide_the_commit_to_the_postcommit_hooks = () =>
			pipelineHooks.ForEach(x => x.Verify(y => y.PostCommit(attempt), Times.Never()));
	}

	[Subject("OptimisticEventStore")]
	public class when_accessing_the_underlying_persistence : using_persistence
	{
		It should_return_a_reference_to_the_underlying_persistence_infrastructure = () =>
			store.Advanced.ShouldBeTheSameAs(persistence.Object);
	}

	[Subject("OptimisticEventStore")]
	public class when_disposing_the_event_store : using_persistence
	{
		Because of = () =>
			store.Dispose();

		It should_dispose_the_underlying_persistence = () =>
			persistence.Verify(x => x.Dispose(), Times.Once());
	}

    [Subject("OptimisticEventstore")]
    public class when_one_thread_persists_a_commit_whose_commitsequence_or_revision_was_made_stale_by_another_thread :
        using_persistence
    {
        const int MostRecentStreamRevision = 1;
        const int CommitSequence = 1;

        static readonly Commit[] Commits = new[]
            {
                new Commit(streamId, MostRecentStreamRevision, Guid.NewGuid(), CommitSequence, null, null, null),
            };

        static readonly Countdown countdown = new Countdown(2);
        static readonly AutoResetEvent gate = new AutoResetEvent(false);
        static int ReadCount, WriteCount;
        static readonly object readLock = new object();

        static Exception thrown;

        Establish context = () =>
        {
            persistence.Setup(x => x.GetUntil(streamId, int.MaxValue)).Returns(Commits)
                .Callback(() =>
                {
                    bool wait = false;
                    lock (readLock)
                    {
                        ReadCount++;
                        if (ReadCount == 1) wait = true; //whoever gets here first wait
                    }
                    if (wait)
                    {
                        gate.WaitOne();
                    }
                });

            dispatcher.Setup(x => x.Dispatch(Moq.It.IsAny<Commit>()))
                .Callback(() =>
                {
                    WriteCount++;
                    //release the other thread once we have persisted the first commit
                    if (WriteCount == 1) gate.Set();
                    countdown.Signal();
                });
        };

        static void ConcurrencyTest()
        {
            //both threads share a consistent view of the data after reading
            //as such they submit conflicting commits
            try
            {
                store.ReadUntil(streamId, 0);

                var attempt = new CommitAttempt() //create inline to avoid DuplicateCommitException
                {
                    StreamId = streamId,
                    CommitId = Guid.NewGuid(),
                    PreviousCommitSequence = CommitSequence,
                    StreamRevision = MostRecentStreamRevision + 1,
                    Events = { new EventMessage() }
                };
                store.Write(attempt);
            }
            catch (Exception e)
            {
                thrown = e;
                countdown.Signal();
            }
        }

        Because of = () =>
        {
            var threadOne = new Thread(ConcurrencyTest);
            var threadTwo = new Thread(ConcurrencyTest);

            threadOne.Start();
            threadTwo.Start();
            countdown.Wait();
        };

        It should_throw_a_ConcurrencyException = () => thrown.ShouldBeOfType<ConcurrencyException>();

        Cleanup thisFunction = () =>
        {
            gate.Dispose();
        };

        public class Countdown
        {
            object _locker = new object();
            int _value;

            public Countdown() { }
            public Countdown(int initialCount) { _value = initialCount; }

            public void Signal() { AddCount(-1); }

            public void AddCount(int amount)
            {
                lock (_locker)
                {
                    _value += amount;
                    if (_value <= 0) Monitor.PulseAll(_locker);
                }
            }

            public void Wait()
            {
                lock (_locker)
                    while (_value > 0)
                        Monitor.Wait(_locker);
            }
        }

    }

	public abstract class using_persistence
	{
		protected static Guid streamId = Guid.NewGuid();
		protected static Mock<IPersistStreams> persistence;
		protected static OptimisticEventStore store;
		protected static List<Mock<IPipelineHook>> pipelineHooks;

		Establish context = () =>
		{
			persistence = new Mock<IPersistStreams>();
			pipelineHooks = new List<Mock<IPipelineHook>>();

			store = new OptimisticEventStore(persistence.Object, pipelineHooks.Select(x => x.Object));
		};

		Cleanup everything = () =>
			streamId = Guid.NewGuid();

		protected static Commit BuildCommitStub(Guid commitId)
		{
			return new Commit(streamId, 1, commitId, 1, SystemTime.UtcNow, null, null);
		}
		protected static Commit BuildCommitStub(int streamRevision, int commitSequence)
		{
			var events = new[] { new EventMessage() }.ToList();
			return new Commit(streamId, streamRevision, Guid.NewGuid(), commitSequence, SystemTime.UtcNow, null, events);
		}
		protected static Commit BuildCommitStub(Guid commitId, int streamRevision, int commitSequence)
		{
			var events = new[] { new EventMessage() }.ToList();
			return new Commit(streamId, streamRevision, commitId, commitSequence, SystemTime.UtcNow, null, events);
		}
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169