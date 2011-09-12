namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Logging;
	using Persistence;

	public class OptimisticEventStore : IStoreEvents, ICommitEvents
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(OptimisticEventStore));
		private readonly IPersistStreams persistence;
		private readonly IEnumerable<IPipelineHook> pipelineHooks;

		public OptimisticEventStore(IPersistStreams persistence, IEnumerable<IPipelineHook> pipelineHooks)
		{
			if (persistence == null)
				throw new ArgumentNullException("persistence");

			this.persistence = persistence;
			this.pipelineHooks = pipelineHooks ?? new IPipelineHook[0];
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				this.persistence.Dispose();
		}

		public virtual IEventStream CreateStream(Guid streamId)
		{
			Logger.Info(Resources.CreatingStream, streamId);
			return new OptimisticEventStream(streamId, this);
		}
		public virtual IEventStream OpenStream(Guid streamId, int minRevision, int maxRevision)
		{
			maxRevision = maxRevision <= 0 ? int.MaxValue : maxRevision;

			Logger.Debug(Resources.OpeningStreamAtRevision, streamId, minRevision, maxRevision);
			return new OptimisticEventStream(streamId, this, minRevision, maxRevision);
		}
		public virtual IEventStream OpenStream(Snapshot snapshot, int maxRevision)
		{
			Logger.Debug(Resources.OpeningStreamWithSnapshot, snapshot.StreamId, snapshot.StreamRevision, maxRevision);
			maxRevision = maxRevision <= 0 ? int.MaxValue : maxRevision;
			return new OptimisticEventStream(snapshot, this, maxRevision);
		}

		IEnumerable<Commit> ICommitEvents.GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			foreach (var commit in this.persistence.GetFrom(streamId, minRevision, maxRevision))
			{
				var filtered = commit;
				foreach (var hook in this.pipelineHooks.Where(x => (filtered = x.Select(filtered)) == null))
				{
					Logger.Info(Resources.PipelineHookSkippedCommit, hook.GetType(), commit.CommitId);
					break;
				}

				if (filtered == null)
					Logger.Info(Resources.PipelineHookFilteredCommit);
				else
					yield return filtered;
			}
		}
		void ICommitEvents.Commit(Commit attempt)
		{
			if (!attempt.IsValid() || attempt.IsEmpty())
			{
				Logger.Debug(Resources.CommitAttemptFailedIntegrityChecks);
				return;
			}

			foreach (var hook in this.pipelineHooks)
			{
				Logger.Debug(Resources.InvokingPreCommitHooks, attempt.CommitId, hook.GetType());
				if (hook.PreCommit(attempt))
					continue;

				Logger.Info(Resources.CommitRejectedByPipelineHook, hook.GetType(), attempt.CommitId);
				return;
			}

			Logger.Info(Resources.CommittingAttempt, attempt.CommitId);
			this.persistence.Commit(attempt);

			foreach (var hook in this.pipelineHooks)
			{
				Logger.Debug(Resources.InvokingPostCommitPipelineHooks, attempt.CommitId, hook.GetType());
				hook.PostCommit(attempt);
			}
		}

		public virtual IPersistStreams Advanced
		{
			get { return this.persistence; }
		}
	}
}