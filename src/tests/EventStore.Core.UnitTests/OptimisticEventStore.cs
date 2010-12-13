#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests
{
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject("OptimisticEventStore")]
	public class when_reading_a_stream_until_a_maximum_revision
	{
		It should_query_the_configured_persistence_engine;
		It should_ignore_events_prior_to_the_most_recent_snapshot_retreived;
	}

	[Subject("OptimisticEventStore")]
	public class when_reading_a_stream_from_a_minimum_revision
	{
		It should_query_the_configured_persistence_engine;
		It should_complete_ignore_snapshots_during_stream_reconstruction;
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_an_invalid_commit_back_to_the_stream
	{
		It should_drop_the_commit_provided;
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_back_to_the_stream
	{
		It should_write_the_commit_to_the_configured_persistence_engine;
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_with_an_identifier_that_has_already_been_read
	{
		It should_throw_a_DuplicateCommitException;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169