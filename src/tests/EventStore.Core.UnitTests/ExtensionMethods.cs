namespace EventStore.Core.UnitTests
{
	using System;
	using System.Linq;

	internal static class ExtensionMethods
	{
		public static Commit BuildCommit(this Guid streamId, object messageBody, object snapshot)
		{
			return new Commit(streamId, Guid.NewGuid(), 1, null, null, snapshot)
			{
				Events = { new EventMessage { StreamRevision = 1, Body = messageBody } }
			};
		}
		public static Commit Add(this Commit commit, object messageBody, object snapshot)
		{
			commit = new Commit(
				commit.StreamId, commit.CommitId, commit.CommitSequence, commit.Headers, commit.Events, snapshot);

			var lastEvent = commit.Events.LastOrDefault();
			var revision = (lastEvent == null ? 0 : lastEvent.StreamRevision) + 1;
			commit.Events.Add(new EventMessage { StreamRevision = revision, Body = messageBody });

			return commit;
		}
	}
}