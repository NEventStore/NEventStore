#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Persistence.AcceptanceTests
{
	using Machine.Specifications;

	[Subject("OptimisticEventStore")]
	public class when_a_commit_attempt_is_persisted
	{
		It should_make_the_commit_available_to_be_read_from_the_stream;
		It should_add_the_commit_to_the_set_of_undispatched_commits;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169