namespace EventStore
{
	using Persistence;

	public class OptimisticCommitHook : IHookCommitSelects, IHookCommitAttempts
	{
		private readonly CommitTracker tracker = new CommitTracker();

		public virtual Commit Select(Commit persisted)
		{
			this.tracker.Track(persisted);
			return persisted;
		}
		public virtual bool PreCommit(Commit attempt)
		{
			if (this.tracker.Contains(attempt))
				throw new DuplicateCommitException();

			var head = this.tracker.GetStreamHead(attempt.StreamId);
			if (head == null)
				return true;

			if (head.CommitSequence >= attempt.CommitSequence)
				throw new ConcurrencyException();

			if (head.StreamRevision >= attempt.StreamRevision)
				throw new ConcurrencyException();

			if (head.CommitSequence < attempt.CommitSequence - 1)
				throw new StorageException(); // beyond the end of the stream

			if (head.StreamRevision < attempt.StreamRevision - attempt.Events.Count)
				throw new StorageException(); // beyond the end of the stream

			return true;
		}
		public virtual void PostCommit(Commit persisted)
		{
			this.tracker.Track(persisted);
		}
	}
}