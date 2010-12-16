namespace EventStore.RavenPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Reflection;
	using Persistence;
	using Raven.Client.Document;

	public class RavenPersistence : IPersistStreams
	{
		private readonly DocumentStore store;

		public RavenPersistence(DocumentStore store)
		{
			this.store = store;
			this.store.Initialize();
			this.store.Conventions.FindIdentityProperty = FindIdentityProperty;
			this.store.Conventions.IdentityPartsSeparator = ".";
		}
		private static bool FindIdentityProperty(PropertyInfo property)
		{
			if (property.DeclaringType != typeof(Commit))
				return false;

			// TODO: understand how RavenDB handles composite keys, if at all.
			return property.Name == "StreamId" || property.Name == "CommitSequence";
		}

		public virtual IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision)
		{
			return null;
		}
		public virtual IEnumerable<Commit> GetFrom(Guid streamId, long minRevision)
		{
			return null;
		}
		public virtual void Persist(CommitAttempt uncommitted)
		{
			using (var session = this.store.OpenSession())
			{
				session.Store(uncommitted);
				session.SaveChanges();
			}
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			return null;
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
		}

		public virtual IEnumerable<Guid> GetStreamsToSnapshot(int maxThreshold)
		{
			return null;
		}
		public virtual void AddSnapshot(Guid streamId, long commitSequence, object snapshot)
		{
		}
	}
}