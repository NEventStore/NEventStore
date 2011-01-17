#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests
{
	using System;
	using System.Linq;
	using Dispatcher;
	using Machine.Specifications;
	using Moq;
	using Persistence;
	using It = Machine.Specifications.It;

	[Subject("OptimisticEventStore")]
	public class when_initializing_the_event_store : using_persistence
	{
		static readonly Commit Undispatched =
			new Commit(streamId, 1, Guid.NewGuid(), 1, null, null, null);

		Establish context = () =>
		{
			persistence.Setup(x => x.Initialize());

			persistence.Setup(x => x.GetUndispatchedCommits()).Returns(new[] { Undispatched });
			dispatcher.Setup(x => x.Dispatch(Undispatched));
		};

		Because of = () =>
			store.Initialize();

		It should_initialize_the_underlying_storage = () =>
			persistence.Verify(x => x.Initialize(), Times.Exactly(1));

		It should_dispatch_all_undispatched_commits = () =>
			dispatcher.Verify(x => x.Dispatch(Undispatched), Times.Exactly(1));
	}

	[Subject("OptimisticEventStore")]
	public class when_reading_a_stream_up_to_a_maximum_revision : using_persistence
	{
		const int MaxRevision = 1234;
		static readonly Commit[] Commits = new[]
		{
			new Commit(streamId, 1, Guid.NewGuid(), 1, null, null, "ignore this snapshot")
			{
				Events = { new EventMessage { Body = 1 } }
			}, 
			new Commit(streamId, 3, Guid.NewGuid(), 2, null, null, "use this snapshot")
			{
				Events =
				{
					new EventMessage { Body = 2 },
					new EventMessage { Body = 3 }
				}
			}, 
			new Commit(streamId, 5, Guid.NewGuid(), 3, null, null, null)
			{
				Events =
				{
					new EventMessage { Body = 4 },
					new EventMessage { Body = 5 }
				}
			}
		};

		static CommittedEventStream actual;

		Establish context = () =>
			persistence.Setup(x => x.GetUntil(streamId, MaxRevision)).Returns(Commits);

		Because of = () =>
			actual = store.ReadUntil(streamId, MaxRevision);

		It should_query_the_configured_persistence_mechanism = () =>
			persistence.VerifyAll();

		It should_ignore_events_prior_to_the_most_recent_snapshot_retreived = () =>
			actual.StreamRevision.ShouldEqual(Commits.MostRecentRevision());

		It should_populate_the_stream_with_the_most_recent_snapshot = () =>
			actual.Snapshot.ShouldEqual(Commits.MostRecentSnapshot());

		It should_ignore_an_earlier_snapshot = () =>
			actual.Snapshot.ShouldNotEqual(Commits[0].Snapshot);

		It should_only_contain_the_events_from_commits_after_the_most_recent_snapshot = () =>
			actual.Events.Count.ShouldEqual(Commits.Last().Events.Count);

		It should_order_the_events_from_oldest_to_newest = () =>
			actual.Events.Cast<object>().Last().ShouldEqual(Commits.NewestEvent());

		It should_populate_the_stream_with_all_commit_identifiers_retreived = () =>
			actual.CommitIdentifiers.ShouldContain(Commits.Select(x => x.CommitId).ToArray());
	}

	[Subject("OptimisticEventStore")]
	public class when_reading_a_stream_from_a_minimum_revision : using_persistence
	{
		const int MinRevision = 42;
		static readonly Commit[] Commits = new[]
		{
			new Commit(streamId, 1, Guid.NewGuid(), 1, null, null, "ignore this snapshot")
			{
				Events = { new EventMessage { Body = 1 } }
			}, 
			new Commit(streamId, 3, Guid.NewGuid(), 2, null, null, "ignore this snapshot too")
			{
				Events =
				{
					new EventMessage { Body = 2 },
					new EventMessage { Body = 3 }
				}
			}, 
			new Commit(streamId, 6, Guid.NewGuid(), 3, null, null, null)
			{
				Events =
				{
					new EventMessage { Body = 4 },
					new EventMessage { Body = 5 },
					new EventMessage { Body = 6 }
				}
			}
		};

		static CommittedEventStream actual;

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, MinRevision)).Returns(Commits);

		Because of = () =>
			actual = store.ReadFrom(streamId, MinRevision);

		It should_query_the_configured_persistence_mechanism = () =>
			persistence.VerifyAll();

		It should_completely_ignore_snapshots_during_stream_reconstruction = () =>
			actual.Events.Count.ShouldEqual(Commits.CountEvents());

		It should_put_the_oldest_event_as_the_first_event = () =>
			actual.Events.Cast<object>().First().ShouldEqual(Commits.OldestEvent());

		It should_put_the_newest_event_as_the_last_event = () =>
			actual.Events.Cast<object>().Last().ShouldEqual(Commits.NewestEvent());

		It should_populate_the_stream_with_all_commit_identifiers_retreived = () =>
			actual.CommitIdentifiers.ShouldContain(Commits.Select(x => x.CommitId).ToArray());
	}

	[Subject("OptimisticEventStore")]
	public class when_reading_from_reversion_zero : using_persistence
	{
		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, 0));

		Because of = () =>
			store.ReadFrom(streamId, 0);

		It should_pass_a_zero_revision_to_the_persistence_infrastructure = () =>
			persistence.Verify(x => x.GetFrom(streamId, 0), Times.Exactly(1));
	}

	[Subject("OptimisticEventStore")]
	public class when_reading_until_version_zero : using_persistence
	{
		Establish context = () =>
			persistence.Setup(x => x.GetUntil(streamId, int.MaxValue));

		Because of = () =>
			store.ReadUntil(streamId, 0);

		It should_pass_the_maximum_numeric_value_to_persistence = () =>
			persistence.Verify(x => x.GetUntil(streamId, int.MaxValue), Times.Exactly(1));
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_null_commit_attempt_back_to_the_stream : using_persistence
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => store.Write(null));

		It should_throw_an_ArgumentNullException = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_an_unidentified_commit_attempt_back_to_the_stream : using_persistence
	{
		static readonly CommitAttempt unidentified = new CommitAttempt();
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => store.Write(unidentified));

		It should_throw_an_ArgumentException = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_the_number_of_commits_is_greater_than_the_number_of_revisions : using_persistence
	{
		static readonly CommitAttempt unidentified = new CommitAttempt
		{
			StreamId = streamId,
			CommitId = Guid.NewGuid(),
			StreamRevision = 1, // this should always be at least PreviousCommitSequence + 1
			PreviousCommitSequence = 1 // previous = 1 means that current commit sequence = 2
		};
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => store.Write(unidentified));

		It should_throw_a_PersistenceEngineException = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_commit_attempt_with_a_negative_commit_sequence_back_to_the_stream : using_persistence
	{
		static readonly CommitAttempt negativeCommitSequence = new CommitAttempt
		{
			StreamId = streamId,
			CommitId = Guid.NewGuid(),
			StreamRevision = 1,
			PreviousCommitSequence = -1
		};
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => store.Write(negativeCommitSequence));

		It should_throw_an_ArgumentException = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_commit_attempt_with_a_non_positive_stream_revision_back_to_the_stream : using_persistence
	{
		static readonly CommitAttempt negativeStreamRevision = new CommitAttempt
		{
			StreamId = streamId,
			CommitId = Guid.NewGuid(),
			PreviousCommitSequence = 1,
			StreamRevision = 0
		};
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => store.Write(negativeStreamRevision));

		It should_throw_an_ArgumentException = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_attempt_whose_sequence_is_beyond_the_known_end_of_a_stream : using_persistence
	{
		static readonly Commit[] commits = new[]
		{
			new Commit(streamId, 1, Guid.NewGuid(), 1, null, null, null)
			{
				Events = { new EventMessage() }
			}
		};
		static readonly CommitAttempt attempt = new CommitAttempt
		{
			StreamId = streamId,
			CommitId = Guid.NewGuid(),
			PreviousCommitSequence = commits[0].StreamRevision + 1, // *beyond* the end of the known stream
			StreamRevision = commits[0].StreamRevision + 2,
			Events = { new EventMessage() }
		};
		static Exception thrown;

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, 0)).Returns(commits);

		Because of = () =>
		{
			store.ReadFrom(streamId, 0);
			thrown = Catch.Exception(() => store.Write(attempt));
		};

		It should_throw_a_PersistenceException = () =>
			thrown.ShouldBeOfType<PersistenceEngineException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_attempt_whose_revision_is_well_beyond_the_known_end_of_a_stream : using_persistence
	{
		static readonly Commit[] commits = new[]
		{
			new Commit(streamId, 1, Guid.NewGuid(), 1, null, null, null)
			{
				Events = { new EventMessage() }
			}
		};
		static readonly CommitAttempt attempt = new CommitAttempt
		{
			StreamId = streamId,
			CommitId = Guid.NewGuid(),
			PreviousCommitSequence = 1,
			StreamRevision = 3, // *beyond* the end of the known stream, should be *2*
			Events = { new EventMessage() }
		};
		static Exception thrown;

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, 0)).Returns(commits);

		Because of = () =>
		{
			store.ReadFrom(streamId, 0);
			thrown = Catch.Exception(() => store.Write(attempt));
		};

		It should_throw_a_PersistenceException = () =>
			thrown.ShouldBeOfType<PersistenceEngineException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_an_empty_commit_attempt_to_a_stream : using_persistence
	{
		static readonly CommitAttempt attemptWithNoEvents = new CommitAttempt
		{
			StreamId = streamId,
			CommitId = Guid.NewGuid(),
			StreamRevision = 1
		};

		Establish context = () =>
			persistence.Setup(x => x.Persist(attemptWithNoEvents));

		Because of = () =>
			store.Write(attemptWithNoEvents);

		It should_drop_the_commit_provided = () =>
			persistence.Verify(x => x.Persist(attemptWithNoEvents), Times.AtMost(0));
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_valid_and_populated_commit_attempt_to_a_stream : using_persistence
	{
		static readonly CommitAttempt populatedAttempt = new CommitAttempt
		{
			StreamId = streamId,
			CommitId = Guid.NewGuid(),
			StreamRevision = 1,
			Events = { new EventMessage() }
		};

		Establish context = () =>
		{
			persistence.Setup(x => x.Persist(populatedAttempt));
			dispatcher.Setup(x => x.Dispatch(populatedAttempt.ToCommit()));
		};

		Because of = () =>
			store.Write(populatedAttempt);

		It should_provide_the_commit_attempt_to_the_configured_persistence_mechanism = () =>
			persistence.Verify(x => x.Persist(populatedAttempt), Times.Exactly(1));

		It should_provide_the_commit_to_the_dispatcher = () =>
			dispatcher.Verify(x => x.Dispatch(populatedAttempt.ToCommit()), Times.Exactly(1));
	}

	/// <summary>
	/// This behavior is primarily to support a NoSQL storage solution where CommitId is not being used as the "primary key"
	/// in a NoSQL environment, we'll most likely use StreamId + CommitSequence, which also enables optimistic concurrency.
	/// </summary>
	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_attempt_with_an_identifier_that_was_previously_read_up_to_a_max_revision : using_persistence
	{
		const int MaxRevision = 2;
		static readonly Guid DuplicateCommitId = Guid.NewGuid();
		static readonly Commit[] Commits = new[]
		{
			new Commit(streamId, 1, DuplicateCommitId, 1, null, null, null)
			{
				Events = { new EventMessage { Body = 1 } }
			},
			new Commit(streamId, 2, Guid.NewGuid(), 2, null, null, "commit from before this snapshot should be remembered.")
			{
				Events = { new EventMessage { Body = 1 } }
			}
		};
		static readonly CommitAttempt Attempt = new CommitAttempt
		{
			StreamId = streamId,
			CommitId = DuplicateCommitId,
			PreviousCommitSequence = Commits.Last().CommitSequence,
			StreamRevision = Commits.Last().StreamRevision + 1,
			Events = { new EventMessage() }
		};
		static Exception thrown;

		Establish context = () =>
			persistence.Setup(x => x.GetUntil(streamId, MaxRevision)).Returns(Commits);

		Because of = () =>
		{
			store.ReadUntil(streamId, MaxRevision);
			thrown = Catch.Exception(() => store.Write(Attempt));
		};

		It should_throw_a_DuplicateCommitException = () =>
			thrown.ShouldBeOfType<DuplicateCommitException>();
	}

	/// <summary>
	/// This behavior is primarily to support a NoSQL storage solution where CommitId is not being used as the "primary key"
	/// in a NoSQL environment, we'll most likely use StreamId + CommitSequence, which also enables optimistic concurrency.
	/// </summary>
	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_attempt_with_an_identifier_that_was_previously_read_from_a_min_revision : using_persistence
	{
		const int MinRevision = 1;
		static readonly Guid DuplicateCommitId = Guid.NewGuid();
		static readonly Commit[] Commits = new[]
		{
			new Commit(streamId, 1, DuplicateCommitId, 1, null, null, null)
			{
				Events = { new EventMessage { Body = 1 } }
			}
		};
		static readonly CommitAttempt Attempt = new CommitAttempt
		{
			StreamId = streamId,
			CommitId = DuplicateCommitId,
			PreviousCommitSequence = Commits.Last().CommitSequence,
			StreamRevision = Commits.Last().StreamRevision + 1,
			Events = { new EventMessage() }
		};
		static Exception thrown;

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, MinRevision)).Returns(Commits);

		Because of = () => thrown = Catch.Exception(() =>
		{
			store.ReadFrom(streamId, MinRevision);
			store.Write(Attempt);
		});

		It should_throw_a_DuplicateCommitException = () =>
			thrown.ShouldBeOfType<DuplicateCommitException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_attempting_to_commit_the_same_commit_identifier_more_than_once : using_persistence
	{
		static readonly CommitAttempt Attempt = new CommitAttempt
		{
			StreamId = streamId,
			CommitId = Guid.NewGuid(), // will be duplicate
			StreamRevision = 1,
			Events = { new EventMessage() }
		};
		static Exception thrown;

		Establish context = () =>
		{
			store.Write(Attempt);
			Attempt.PreviousCommitSequence++;
			Attempt.StreamRevision++;
		};

		Because of = () =>
			thrown = Catch.Exception(() => store.Write(Attempt));

		It throw_a_DuplicateCommitException = () =>
			thrown.ShouldBeOfType<DuplicateCommitException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_an_attempt_with_a_sequence_less_than_the_most_recent_sequence_for_the_stream : using_persistence
	{
		const int StreamRevision = 42;
		const int MostRecentSequence = 42;
		static readonly Commit[] Commits = new[]
		{
			new Commit(streamId, StreamRevision, Guid.NewGuid(), MostRecentSequence, null, null, null), 
		};
		static readonly CommitAttempt Attempt = new CommitAttempt
		{
			StreamId = streamId,
			CommitId = Guid.NewGuid(),
			PreviousCommitSequence = MostRecentSequence - 1, // here's the problem
			StreamRevision = StreamRevision,
			Events = { new EventMessage() }
		};

		static Exception thrown;

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, StreamRevision)).Returns(Commits);

		Because of = () =>
		{
			store.ReadFrom(streamId, StreamRevision);
			thrown = Catch.Exception(() => store.Write(Attempt));
		};

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_an_attempt_with_a_revision_less_or_equal_to_than_the_most_recent_revision_read_for_the_stream : using_persistence
	{
		const int MostRecentStreamRevision = 2;
		const int CommitSequence = 1;
		static readonly Commit[] Commits = new[]
		{
			new Commit(streamId, MostRecentStreamRevision, Guid.NewGuid(), CommitSequence, null, null, null), 
		};
		static readonly CommitAttempt Attempt = new CommitAttempt
		{
			StreamId = streamId,
			CommitId = Guid.NewGuid(),
			PreviousCommitSequence = CommitSequence,
			StreamRevision = MostRecentStreamRevision,
			Events = { new EventMessage() }
		};

		static Exception thrown;

		Establish context = () =>
			persistence.Setup(x => x.GetUntil(streamId, MostRecentStreamRevision)).Returns(Commits);

		Because of = () =>
		{
			store.ReadUntil(streamId, MostRecentStreamRevision);
			thrown = Catch.Exception(() => store.Write(Attempt));
		};

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_an_attempt_with_sequence_and_revision_values_less_than_or_equal_to_the_most_recent_commit_for_the_stream : using_persistence
	{
		static readonly CommitAttempt Attempt = new CommitAttempt
		{
			StreamId = streamId,
			CommitId = Guid.NewGuid(),
			StreamRevision = 1,
			Events = { new EventMessage() }
		};
		static Exception thrown;

		Establish context = () =>
		{
			store.Write(Attempt);
			Attempt.CommitId = Guid.NewGuid(); // new attempt but with same sequence and revision values
		};

		Because of = () =>
			thrown = Catch.Exception(() => store.Write(Attempt));

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();
	}

	public abstract class using_persistence
	{
		protected static Guid streamId = Guid.NewGuid();
		protected static Mock<IPersistStreams> persistence;
		protected static Mock<IDispatchCommits> dispatcher;
		protected static OptimisticEventStore store;

		Establish context = () =>
		{
			persistence = new Mock<IPersistStreams>();
			dispatcher = new Mock<IDispatchCommits>();
			store = new OptimisticEventStore(persistence.Object, dispatcher.Object);
		};
		Cleanup everything = () =>
			streamId = Guid.NewGuid();
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169