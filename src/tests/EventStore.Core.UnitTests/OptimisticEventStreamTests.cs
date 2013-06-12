#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject("OptimisticEventStream")]
	public class when_building_a_stream : on_the_event_stream
	{
		const int MinRevision = 2;
		const int MaxRevision = 7;
		static readonly int EachCommitHas = 2.Events();
		static readonly Commit[] Committed = new[]
		{
			BuildCommitStub(2, 1, EachCommitHas), // 1-2
			BuildCommitStub(4, 2, EachCommitHas), // 3-4
			BuildCommitStub(6, 3, EachCommitHas), // 5-6
			BuildCommitStub(8, 3, EachCommitHas) // 7-8
		};

		Establish context = () =>
		{
			Committed[0].Headers["Common"] = string.Empty;
			Committed[1].Headers["Common"] = string.Empty;
			Committed[2].Headers["Common"] = string.Empty;
			Committed[3].Headers["Common"] = string.Empty;
			Committed[0].Headers["Unique"] = string.Empty;

			persistence.Setup(x => x.GetFrom(streamId, MinRevision, MaxRevision)).Returns(Committed);
		};

		Because of = () =>
			stream = new OptimisticEventStream(streamId, persistence.Object, MinRevision, MaxRevision);

		It should_have_the_correct_stream_identifier = () =>
			stream.StreamId.ShouldEqual(streamId);

		It should_have_the_correct_head_stream_revision = () =>
			stream.StreamRevision.ShouldEqual(MaxRevision);

		It should_have_the_correct_head_commit_sequence = () =>
			stream.CommitSequence.ShouldEqual(Committed.Last().CommitSequence);

		It should_not_include_events_below_the_minimum_revision_indicated = () =>
			stream.CommittedEvents.First().ShouldEqual(Committed.First().Events.Last());

		It should_not_include_events_above_the_maximum_revision_indicated = () =>
			stream.CommittedEvents.Last().ShouldEqual(Committed.Last().Events.First());

		It should_have_all_of_the_committed_events_up_to_the_stream_revision_specified = () =>
			stream.CommittedEvents.Count.ShouldEqual(MaxRevision - MinRevision + 1);

		It should_contain_the_headers_from_the_underlying_commits = () =>
			stream.CommittedHeaders.Count.ShouldEqual(2);
	}

	[Subject("OptimisticEventStream")]
	public class when_the_head_event_revision_is_less_than_the_max_desired_revision : on_the_event_stream
	{
		static readonly int EventsPerCommit = 2.Events();
		static readonly Commit[] Committed = new[]
		{
			BuildCommitStub(2, 1, EventsPerCommit), // 1-2
			BuildCommitStub(4, 2, EventsPerCommit), // 3-4
			BuildCommitStub(6, 3, EventsPerCommit), // 5-6
			BuildCommitStub(8, 3, EventsPerCommit) // 7-8
		};

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, 0, int.MaxValue)).Returns(Committed);

		Because of = () =>
			stream = new OptimisticEventStream(streamId, persistence.Object, 0, int.MaxValue);

		It should_set_the_stream_revision_to_the_revision_of_the_most_recent_event = () =>
			stream.StreamRevision.ShouldEqual(Committed.Last().StreamRevision);
	}

	[Subject("OptimisticEventStream")]
	public class when_adding_a_null_event_message : on_the_event_stream
	{
		Because of = () =>
			stream.Add(null);

		It should_be_ignored = () =>
			stream.UncommittedEvents.ShouldBeEmpty();
	}

	[Subject("OptimisticEventStream")]
	public class when_adding_an_unpopulated_event_message : on_the_event_stream
	{
		Because of = () =>
			stream.Add(new EventMessage { Body = null });

		It should_be_ignored = () =>
			stream.UncommittedEvents.ShouldBeEmpty();
	}

	[Subject("OptimisticEventStream")]
	public class when_adding_a_fully_populated_event_message : on_the_event_stream
	{
		Because of = () =>
			stream.Add(new EventMessage { Body = "populated" });

		It should_add_the_event_to_the_set_of_uncommitted_events = () =>
			stream.UncommittedEvents.Count.ShouldEqual(1);
	}

	[Subject("OptimisticEventStream")]
	public class when_adding_multiple_populated_event_messages : on_the_event_stream
	{
		Because of = () =>
		{
			stream.Add(new EventMessage { Body = "populated" });
			stream.Add(new EventMessage { Body = "also populated" });
		};

		It should_add_all_of_the_events_provided_to_the_set_of_uncommitted_events = () =>
			stream.UncommittedEvents.Count.ShouldEqual(2);
	}

	[Subject("OptimisticEventStream")]
	public class when_adding_a_simple_object_as_an_event_message : on_the_event_stream
	{
		const string MyEvent = "some event data";

		Because of = () =>
			stream.Add(new EventMessage { Body = MyEvent });

		It should_add_the_uncommited_event_to_the_set_of_uncommitted_events = () =>
			stream.UncommittedEvents.Count.ShouldEqual(1);

		It should_wrap_the_uncommited_event_in_an_EventMessage_object = () =>
			stream.UncommittedEvents.First().Body.ShouldEqual(MyEvent);
	}

	[Subject("OptimisticEventStream")]
	public class when_clearing_any_uncommitted_changes : on_the_event_stream
	{
		Establish context = () =>
			stream.Add(new EventMessage { Body = string.Empty });

		Because of = () =>
			stream.ClearChanges();

		It should_clear_all_uncommitted_events = () =>
			stream.UncommittedEvents.Count.ShouldEqual(0);
	}

	[Subject("OptimisticEventStream")]
	public class when_committing_an_empty_changeset : on_the_event_stream
	{
		Because of = () =>
			stream.CommitChanges(Guid.NewGuid());

		It should_not_call_the_underlying_infrastructure = () =>
			persistence.Verify(x => x.Commit(Moq.It.IsAny<Commit>()), Times.Never());

		It should_not_increment_the_current_stream_revision = () =>
			stream.StreamRevision.ShouldEqual(0);

		It should_not_increment_the_current_commit_sequence = () =>
			stream.CommitSequence.ShouldEqual(0);
	}

	[Subject("OptimisticEventStream")]
	public class when_committing_any_uncommitted_changes : on_the_event_stream
	{
		static readonly Guid commitId = Guid.NewGuid();
		static readonly EventMessage uncommitted = new EventMessage { Body = string.Empty };
		static readonly Dictionary<string, object> headers = new Dictionary<string, object> { { "key", "value" } };
		static Commit constructed;

		Establish context = () =>
		{
			persistence.Setup(x => x.Commit(Moq.It.IsAny<Commit>())).Callback<Commit>(x => constructed = x);
			stream.Add(uncommitted);
			foreach (var item in headers)
				stream.UncommittedHeaders[item.Key] = item.Value;
		};

		Because of = () =>
			stream.CommitChanges(commitId);

		It should_provide_a_commit_to_the_underlying_infrastructure = () =>
			persistence.Verify(x => x.Commit(Moq.It.IsAny<Commit>()), Times.Once());

		It should_build_the_commit_with_the_correct_stream_identifier = () =>
			constructed.StreamId.ShouldEqual(streamId);

		It should_build_the_commit_with_the_correct_stream_revision = () =>
			constructed.StreamRevision.ShouldEqual(DefaultStreamRevision);

		It should_build_the_commit_with_the_correct_commit_identifier = () =>
			constructed.CommitId.ShouldEqual(commitId);

		It should_build_the_commit_with_an_incremented_commit_sequence = () =>
			constructed.CommitSequence.ShouldEqual(DefaultCommitSequence);

		It should_build_the_commit_with_the_correct_commit_stamp = () =>
			SystemTime.UtcNow.ShouldEqual(constructed.CommitStamp);

		It should_build_the_commit_with_the_headers_provided = () =>
			constructed.Headers[headers.First().Key].ShouldEqual(headers.First().Value);

		It should_build_the_commit_containing_all_uncommitted_events = () =>
			constructed.Events.Count.ShouldEqual(headers.Count);

		It should_build_the_commit_using_the_event_messages_provided = () =>
			constructed.Events.First().ShouldEqual(uncommitted);

		It should_contain_a_copy_of_the_headers_provided = () =>
			constructed.Headers.ShouldNotBeEmpty();

		It should_update_the_stream_revision = () =>
			stream.StreamRevision.ShouldEqual(constructed.StreamRevision);

		It should_update_the_commit_sequence = () =>
			stream.CommitSequence.ShouldEqual(constructed.CommitSequence);

		It should_add_the_uncommitted_events_the_committed_events = () =>
			stream.CommittedEvents.Last().ShouldEqual(uncommitted);

		It should_clear_the_uncommitted_events_on_the_stream = () =>
			stream.UncommittedEvents.ShouldBeEmpty();

		It should_clear_the_uncommitted_headers_on_the_stream = () =>
			stream.UncommittedHeaders.ShouldBeEmpty();

		It should_copy_the_uncommitted_headers_to_the_committed_stream_headers = () =>
			stream.CommittedHeaders.Count.ShouldEqual(headers.Count);
	}

	/// <summary>
	/// This behavior is primarily to support a NoSQL storage solution where CommitId is not being used as the "primary key"
	/// in a NoSQL environment, we'll most likely use StreamId + CommitSequence, which also enables optimistic concurrency.
	/// </summary>
	[Subject("OptimisticEventStream")]
	public class when_committing_with_an_identifier_that_was_previously_read : on_the_event_stream
	{
		static readonly Commit[] Committed = new[] { BuildCommitStub(1, 1, 1) };
		static readonly Guid DupliateCommitId = Committed[0].CommitId;
		static Exception thrown;

		Establish context = () =>
		{
			persistence
				.Setup(x => x.GetFrom(streamId, 0, int.MaxValue))
				.Returns(Committed);

			stream = new OptimisticEventStream(
				streamId, persistence.Object, 0, int.MaxValue);
		};

		Because of = () =>
			thrown = Catch.Exception(() => stream.CommitChanges(DupliateCommitId));

		It should_throw_a_DuplicateCommitException = () =>
			thrown.ShouldBeOfType<DuplicateCommitException>();
	}

	[Subject("OptimisticEventStream")]
	public class when_committing_after_another_thread_or_process_has_moved_the_stream_head : on_the_event_stream
	{
		const int StreamRevision = 1;
		private static readonly Commit[] Committed = new[] { BuildCommitStub(1, 1, 1) };
		static readonly EventMessage uncommitted = new EventMessage { Body = string.Empty };
		static readonly Commit[] DiscoveredOnCommit = new[] { BuildCommitStub(3, 2, 2) };
		static Commit constructed;
		static Exception thrown;

		Establish context = () =>
		{
			persistence
				.Setup(x => x.Commit(Moq.It.IsAny<Commit>()))
				.Throws(new ConcurrencyException());
			persistence
				.Setup(x => x.GetFrom(streamId, StreamRevision, int.MaxValue))
				.Returns(Committed);
			persistence
				.Setup(x => x.GetFrom(streamId, StreamRevision + 1, int.MaxValue))
				.Returns(DiscoveredOnCommit);

			stream = new OptimisticEventStream(streamId, persistence.Object, StreamRevision, int.MaxValue);
			stream.Add(uncommitted);
		};

		Because of = () =>
			thrown = Catch.Exception(() => stream.CommitChanges(Guid.NewGuid()));

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();

		It should_query_the_underlying_storage_to_discover_the_new_commits = () =>
			persistence.Verify(x => x.GetFrom(streamId, StreamRevision + 1, int.MaxValue), Times.Once());

		It should_update_the_stream_revision_accordingly = () =>
			stream.StreamRevision.ShouldEqual(DiscoveredOnCommit[0].StreamRevision);

		It should_update_the_commit_sequence_accordingly = () =>
			stream.CommitSequence.ShouldEqual(DiscoveredOnCommit[0].CommitSequence);

		It should_add_the_newly_discovered_committed_events_to_the_set_of_committed_events_accordingly = () =>
			stream.CommittedEvents.Count.ShouldEqual(DiscoveredOnCommit[0].Events.Count + 1);
	}

	[Subject("OptimisticEventStream")]
	public class when_attempting_to_invoke_behavior_on_a_disposed_stream : on_the_event_stream
	{
		static Exception thrown;

		Establish context = () =>
			stream.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => stream.CommitChanges(Guid.NewGuid()));

		It should_throw_a_ObjectDisposedException = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject("OptimisticEventStream")]
	public class when_attempting_to_modify_the_event_collections : on_the_event_stream
	{
		It should_throw_an_exception_when_adding_to_the_committed_collection = () =>
			Catch.Exception(() => stream.CommittedEvents.Add(null)).ShouldBeOfType<NotSupportedException>();
		It should_throw_an_exception_when_adding_to_the_uncommitted_collection = () =>
			Catch.Exception(() => stream.UncommittedEvents.Add(null)).ShouldBeOfType<NotSupportedException>();

		It should_throw_an_exception_when_clearing_the_committed_collection = () =>
			Catch.Exception(() => stream.CommittedEvents.Clear()).ShouldBeOfType<NotSupportedException>();
		It should_throw_an_exception_when_clearing_the_uncommitted_collection = () =>
			Catch.Exception(() => stream.UncommittedEvents.Clear()).ShouldBeOfType<NotSupportedException>();

		It should_throw_an_exception_when_removing_from_the_committed_collection = () =>
			Catch.Exception(() => stream.CommittedEvents.Remove(null)).ShouldBeOfType<NotSupportedException>();
		It should_throw_an_exception_when_removing_from_the_uncommitted_collection = () =>
			Catch.Exception(() => stream.UncommittedEvents.Remove(null)).ShouldBeOfType<NotSupportedException>();
	}

	public abstract class on_the_event_stream
	{
		protected const int DefaultStreamRevision = 1;
		protected const int DefaultCommitSequence = 1;
		protected static Guid streamId = Guid.NewGuid();
		protected static OptimisticEventStream stream;
		protected static Mock<ICommitEvents> persistence;

		Establish context = () =>
		{
			persistence = new Mock<ICommitEvents>();
			stream = new OptimisticEventStream(streamId, persistence.Object);
			SystemTime.Resolver = () => new DateTime(2012, 1, 1, 13, 0, 0);
		};

		Cleanup cleanup = () =>
		{
			streamId = Guid.NewGuid();
			SystemTime.Resolver = null;
		};

		protected static Commit BuildCommitStub(int revision, int sequence, int eventCount)
		{
			var events = new List<EventMessage>(eventCount);
			for (var i = 0; i < eventCount; i++)
				events.Add(new EventMessage());

			return new Commit(streamId, revision, Guid.NewGuid(), sequence, SystemTime.UtcNow, null, events);
		}
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169