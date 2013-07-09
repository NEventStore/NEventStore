
#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.Tests
{
    using EventStore.Persistence.AcceptanceTests;
    using EventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using Xunit.Should;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Moq;

    public class when_building_a_stream : on_the_event_stream
	{
		const int MinRevision = 2;
		const int MaxRevision = 7;
		readonly int EachCommitHas = 2.Events();
		Commit[] Committed;

		protected override void Context()
		{
		    Committed = new[]
		    {
		        BuildCommitStub(2, 1, EachCommitHas), // 1-2
		        BuildCommitStub(4, 2, EachCommitHas), // 3-4
		        BuildCommitStub(6, 3, EachCommitHas), // 5-6
		        BuildCommitStub(8, 3, EachCommitHas) // 7-8
		    };

			Committed[0].Headers["Common"] = string.Empty;
			Committed[1].Headers["Common"] = string.Empty;
			Committed[2].Headers["Common"] = string.Empty;
			Committed[3].Headers["Common"] = string.Empty;
			Committed[0].Headers["Unique"] = string.Empty;

			Persistence.Setup(x => x.GetFrom(streamId, MinRevision, MaxRevision)).Returns(Committed);
		}

	    protected override void Because()
	    {
	        Stream = new OptimisticEventStream(streamId, Persistence.Object, MinRevision, MaxRevision);
	    }

        [Fact]
	    public void should_have_the_correct_stream_identifier()
	    {
	        Stream.StreamId.ShouldBe(streamId);
	    }

        [Fact]
        public void should_have_the_correct_head_stream_revision()
	    {
	        Stream.StreamRevision.ShouldBe(MaxRevision);
	    }

        [Fact]
        public void should_have_the_correct_head_commit_sequence()
	    {
	        Stream.CommitSequence.ShouldBe(Committed.Last().CommitSequence);
	    }

        [Fact]
        public void should_not_include_events_below_the_minimum_revision_indicated()
	    {
	        Stream.CommittedEvents.First().ShouldBe(Committed.First().Events.Last());
	    }

        [Fact]
        public void should_not_include_events_above_the_maximum_revision_indicated()
	    {
	        Stream.CommittedEvents.Last().ShouldBe(Committed.Last().Events.First());
	    }

        [Fact]
        public void should_have_all_of_the_committed_events_up_to_the_stream_revision_specified()
	    {
	        Stream.CommittedEvents.Count.ShouldBe(MaxRevision - MinRevision + 1);
	    }

        [Fact]
        public void should_contain_the_headers_from_the_underlying_commits()
	    {
	        Stream.CommittedHeaders.Count.ShouldBe(2);
	    }
	}
    
	public class when_the_head_event_revision_is_less_than_the_max_desired_revision : on_the_event_stream
	{
		readonly int EventsPerCommit = 2.Events();
		Commit[] Committed;

		protected override void Context()
		{
		    Committed = new[]
		    {
		        BuildCommitStub(2, 1, EventsPerCommit), // 1-2
		        BuildCommitStub(4, 2, EventsPerCommit), // 3-4
		        BuildCommitStub(6, 3, EventsPerCommit), // 5-6
		        BuildCommitStub(8, 3, EventsPerCommit) // 7-8
		    };

			Persistence.Setup(x => x.GetFrom(streamId, 0, int.MaxValue)).Returns(Committed);
        }

	    protected override void Because()
	    {
	        Stream = new OptimisticEventStream(streamId, Persistence.Object, 0, int.MaxValue);
	    }

        [Fact]
        public void should_set_the_stream_revision_to_the_revision_of_the_most_recent_event()
	    {
	        Stream.StreamRevision.ShouldBe(Committed.Last().StreamRevision);
	    }
	}
    
	public class when_adding_a_null_event_message : on_the_event_stream
	{
	    protected override void Because()
	    {
	        Stream.Add(null);
	    }

        [Fact]
        public void should_be_ignored()
	    {
	        Stream.UncommittedEvents.ShouldBeEmpty();
	    }
	}
	
	public class when_adding_an_unpopulated_event_message : on_the_event_stream
	{
	    protected override void Because()
	    {
	        Stream.Add(new EventMessage {Body = null});
	    }

        [Fact]
        public void should_be_ignored()
	    {
	        Stream.UncommittedEvents.ShouldBeEmpty();
	    }
	}
    
	public class when_adding_a_fully_populated_event_message : on_the_event_stream
	{
	    protected override void Because()
	    {
	        Stream.Add(new EventMessage {Body = "populated"});
	    }

        [Fact]
        public void should_add_the_event_to_the_set_of_uncommitted_events()
	    {
	        Stream.UncommittedEvents.Count.ShouldBe(1);
	    }
	}
    
	public class when_adding_multiple_populated_event_messages : on_the_event_stream
	{
		protected override void Because()
		{
			Stream.Add(new EventMessage { Body = "populated" });
			Stream.Add(new EventMessage { Body = "also populated" });
		}

        [Fact]
        public void should_add_all_of_the_events_provided_to_the_set_of_uncommitted_events()
		{
		    Stream.UncommittedEvents.Count.ShouldBe(2);
		}
	}
    
	public class when_adding_a_simple_object_as_an_event_message : on_the_event_stream
	{
		const string MyEvent = "some event data";

	    protected override void Because()
	    {
	        Stream.Add(new EventMessage {Body = MyEvent});
	    }

        [Fact]
        public void should_add_the_uncommited_event_to_the_set_of_uncommitted_events()
	    {
	        Stream.UncommittedEvents.Count.ShouldBe(1);
	    }

        [Fact]
        public void should_wrap_the_uncommited_event_in_an_EventMessage_object()
	    {
	        Stream.UncommittedEvents.First().Body.ShouldBe(MyEvent);
	    }
	}
    
	public class when_clearing_any_uncommitted_changes : on_the_event_stream
	{
	    protected override void Context()
	    {
	        Stream.Add(new EventMessage {Body = string.Empty});
	    }

	    protected override void Because()
	    {
	        Stream.ClearChanges();
	    }

        [Fact]
        public void should_clear_all_uncommitted_events()
	    {
	        Stream.UncommittedEvents.Count.ShouldBe(0);
	    }
	}
    
	public class when_committing_an_empty_changeset : on_the_event_stream
	{
	    protected override void Because()
	    {
	        Stream.CommitChanges(Guid.NewGuid());
	    }

        [Fact]
        public void should_not_call_the_underlying_infrastructure()
	    {
	        Persistence.Verify(x => x.Commit(It.IsAny<Commit>()), Times.Never());
	    }

        [Fact]
        public void should_not_increment_the_current_stream_revision()
	    {
	        Stream.StreamRevision.ShouldBe(0);
	    }

        [Fact]
        public void should_not_increment_the_current_commit_sequence()
	    {
	        Stream.CommitSequence.ShouldBe(0);
	    }
	}
    
	public class when_committing_any_uncommitted_changes : on_the_event_stream
	{
		readonly Guid commitId = Guid.NewGuid();
		readonly EventMessage uncommitted = new EventMessage { Body = string.Empty };
		readonly Dictionary<string, object> headers = new Dictionary<string, object> { { "key", "value" } };
		Commit constructed;

		protected override void Context()
		{
			Persistence.Setup(x => x.Commit(It.IsAny<Commit>())).Callback<Commit>(x => constructed = x);
			Stream.Add(uncommitted);
			foreach (var item in headers)
				Stream.UncommittedHeaders[item.Key] = item.Value;
		}

	    protected override void Because()
	    {
	        Stream.CommitChanges(commitId);
	    }

        [Fact]
        public void should_provide_a_commit_to_the_underlying_infrastructure()
	    {
	        Persistence.Verify(x => x.Commit(It.IsAny<Commit>()), Times.Once());
	    }

        [Fact]
        public void should_build_the_commit_with_the_correct_stream_identifier()
	    {
	        constructed.StreamId.ShouldBe(streamId);
	    }

        [Fact]
        public void should_build_the_commit_with_the_correct_stream_revision()
	    {
	        constructed.StreamRevision.ShouldBe(DefaultStreamRevision);
	    }

        [Fact]
        public void should_build_the_commit_with_the_correct_commit_identifier()
	    {
	        constructed.CommitId.ShouldBe(commitId);
	    }

        [Fact]
        public void should_build_the_commit_with_an_incremented_commit_sequence()
	    {
	        constructed.CommitSequence.ShouldBe(DefaultCommitSequence);
	    }

        [Fact]
        public void should_build_the_commit_with_the_correct_commit_stamp()
	    {
	        SystemTime.UtcNow.ShouldBe(constructed.CommitStamp);
	    }

        [Fact]
        public void should_build_the_commit_with_the_headers_provided()
	    {
	        constructed.Headers[headers.First().Key].ShouldBe(headers.First().Value);
	    }

        [Fact]
        public void should_build_the_commit_containing_all_uncommitted_events()
	    {
	        constructed.Events.Count.ShouldBe(headers.Count);
	    }

        [Fact]
        public void should_build_the_commit_using_the_event_messages_provided()
	    {
	        constructed.Events.First().ShouldBe(uncommitted);
	    }

        [Fact]
        public void should_contain_a_copy_of_the_headers_provided()
	    {
	        constructed.Headers.ShouldNotBeEmpty();
	    }

        [Fact]
        public void should_update_the_stream_revision()
	    {
	        Stream.StreamRevision.ShouldBe(constructed.StreamRevision);
	    }

        [Fact]
        public void should_update_the_commit_sequence()
	    {
	        Stream.CommitSequence.ShouldBe(constructed.CommitSequence);
	    }

        [Fact]
        public void should_add_the_uncommitted_events_the_committed_events()
	    {
	        Stream.CommittedEvents.Last().ShouldBe(uncommitted);
	    }

        [Fact]
        public void should_clear_the_uncommitted_events_on_the_stream()
	    {
	        Stream.UncommittedEvents.ShouldBeEmpty();
	    }

        [Fact]
        public void should_clear_the_uncommitted_headers_on_the_stream()
	    {
	        Stream.UncommittedHeaders.ShouldBeEmpty();
	    }

        [Fact]
        public void should_copy_the_uncommitted_headers_to_the_committed_stream_headers()
	    {
	        Stream.CommittedHeaders.Count.ShouldBe(headers.Count);
	    }
	}

	/// <summary>
	/// This behavior is primarily to support a NoSQL storage solution where CommitId is not being used as the "primary key"
	/// in a NoSQL environment, we'll most likely use StreamId + CommitSequence, which also enables optimistic concurrency.
	/// </summary>
	public class when_committing_with_an_identifier_that_was_previously_read : on_the_event_stream
	{
		Commit[] Committed;
	    Guid DupliateCommitId;
		Exception thrown;

		protected override void Context()
		{
            Committed = new[] { BuildCommitStub(1, 1, 1) };
            DupliateCommitId = Committed[0].CommitId;

			Persistence
				.Setup(x => x.GetFrom(streamId, 0, int.MaxValue))
				.Returns(Committed);

			Stream = new OptimisticEventStream(
				streamId, Persistence.Object, 0, int.MaxValue);
		}

	    protected override void Because()
	    {
	        thrown = Catch.Exception(() => Stream.CommitChanges(DupliateCommitId));
	    }

        [Fact]
        public void should_throw_a_DuplicateCommitException()
	    {
	        thrown.ShouldBeInstanceOf<DuplicateCommitException>();
	    }
	}
	
	public class when_committing_after_another_thread_or_process_has_moved_the_stream_head : on_the_event_stream
	{
		const int StreamRevision = 1;
		Commit[] Committed;
		readonly EventMessage uncommitted = new EventMessage { Body = string.Empty };
		Commit[] DiscoveredOnCommit;
		Commit constructed;
		Exception thrown;

		protected override void Context()
		{
            Committed = new[] { BuildCommitStub(1, 1, 1) };
            DiscoveredOnCommit = new[] { BuildCommitStub(3, 2, 2) };

			Persistence
				.Setup(x => x.Commit(It.IsAny<Commit>()))
				.Throws(new ConcurrencyException());
			Persistence
				.Setup(x => x.GetFrom(streamId, StreamRevision, int.MaxValue))
				.Returns(Committed);
			Persistence
				.Setup(x => x.GetFrom(streamId, StreamRevision + 1, int.MaxValue))
				.Returns(DiscoveredOnCommit);

			Stream = new OptimisticEventStream(streamId, Persistence.Object, StreamRevision, int.MaxValue);
			Stream.Add(uncommitted);
		}

	    protected override void Because()
	    {
	        thrown = Catch.Exception(() => Stream.CommitChanges(Guid.NewGuid()));
	    }

        [Fact]
        public void should_throw_a_ConcurrencyException()
	    {
	        thrown.ShouldBeInstanceOf<ConcurrencyException>();
	    }

        [Fact]
        public void should_query_the_underlying_storage_to_discover_the_new_commits()
	    {
	        Persistence.Verify(x => x.GetFrom(streamId, StreamRevision + 1, int.MaxValue), Times.Once());
	    }

        [Fact]
        public void should_update_the_stream_revision_accordingly()
	    {
	        Stream.StreamRevision.ShouldBe(DiscoveredOnCommit[0].StreamRevision);
	    }

        [Fact]
        public void should_update_the_commit_sequence_accordingly()
	    {
	        Stream.CommitSequence.ShouldBe(DiscoveredOnCommit[0].CommitSequence);
	    }

        [Fact]
        public void should_add_the_newly_discovered_committed_events_to_the_set_of_committed_events_accordingly()
	    {
	        Stream.CommittedEvents.Count.ShouldBe(DiscoveredOnCommit[0].Events.Count + 1);
	    }
	}
    
	public class when_attempting_to_invoke_behavior_on_a_disposed_stream : on_the_event_stream
	{
		Exception thrown;

	    protected override void Context()
	    {
	        Stream.Dispose();
	    }

	    protected override void Because()
	    {
	        thrown = Catch.Exception(() => Stream.CommitChanges(Guid.NewGuid()));
	    }

        [Fact]
        public void should_throw_a_ObjectDisposedException()
	    {
	        thrown.ShouldBeInstanceOf<ObjectDisposedException>();
	    }
	}
    
	public class when_attempting_to_modify_the_event_collections : on_the_event_stream
	{
        [Fact]
        public void should_throw_an_exception_when_adding_to_the_committed_collection()
	    {
	        Catch.Exception(() => Stream.CommittedEvents.Add(null)).ShouldBeInstanceOf<NotSupportedException>();
	    }

        [Fact]
        public void should_throw_an_exception_when_adding_to_the_uncommitted_collection()
	    {
	        Catch.Exception(() => Stream.UncommittedEvents.Add(null)).ShouldBeInstanceOf<NotSupportedException>();
	    }

        [Fact]
        public void should_throw_an_exception_when_clearing_the_committed_collection()
	    {
	        Catch.Exception(() => Stream.CommittedEvents.Clear()).ShouldBeInstanceOf<NotSupportedException>();
	    }

        [Fact]
        public void should_throw_an_exception_when_clearing_the_uncommitted_collection()
	    {
	        Catch.Exception(() => Stream.UncommittedEvents.Clear()).ShouldBeInstanceOf<NotSupportedException>();
	    }

        [Fact]
        public void should_throw_an_exception_when_removing_from_the_committed_collection()
	    {
	        Catch.Exception(() => Stream.CommittedEvents.Remove(null)).ShouldBeInstanceOf<NotSupportedException>();
	    }

        [Fact]
        public void should_throw_an_exception_when_removing_from_the_uncommitted_collection()
	    {
	        Catch.Exception(() => Stream.UncommittedEvents.Remove(null)).ShouldBeInstanceOf<NotSupportedException>();
	    }
	}

	public abstract class on_the_event_stream : SpecificationBase, IUseFixture<FakeTimeFixture>
	{
		protected const int DefaultStreamRevision = 1;
		protected const int DefaultCommitSequence = 1;
		protected Guid streamId = Guid.NewGuid();
	    OptimisticEventStream stream;
	    Mock<ICommitEvents> persistence;

	    public Mock<ICommitEvents> Persistence
	    {
            get { return persistence ?? (persistence = new Mock<ICommitEvents>()); }
	    }

	    public OptimisticEventStream Stream
	    {
            get { return stream ?? (stream = new OptimisticEventStream(streamId, Persistence.Object)); }
	        set { stream = value; }
	    }
        
		protected Commit BuildCommitStub(int revision, int sequence, int eventCount)
		{
			var events = new List<EventMessage>(eventCount);
			for (var i = 0; i < eventCount; i++)
				events.Add(new EventMessage());

			return new Commit(streamId, revision, Guid.NewGuid(), sequence, SystemTime.UtcNow, null, events);
		}

	    public void SetFixture(FakeTimeFixture data)
	    {
	        
	    }
	}

    public class FakeTimeFixture : IDisposable
    {
        public FakeTimeFixture()
        {
            SystemTime.Resolver = () =>  new DateTime(2012, 1, 1, 13, 0, 0);
        }

        public void Dispose()
        {
            SystemTime.Resolver = null;
        }
    }
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169