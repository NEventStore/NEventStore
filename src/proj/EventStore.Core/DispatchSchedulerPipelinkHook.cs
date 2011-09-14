namespace EventStore
{
	using System;
	using Dispatcher;

	public class DispatchSchedulerPipelinkHook : IPipelineHook
	{
		private readonly IScheduleDispatches scheduler;

		public DispatchSchedulerPipelinkHook()
			: this(null)
		{
		}
		public DispatchSchedulerPipelinkHook(IScheduleDispatches scheduler)
		{
			this.scheduler = scheduler ?? new NullDispatcher(); // serves as a scheduler also
		}
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			this.scheduler.Dispose();
		}

		public Commit Select(Commit committed)
		{
			return committed;
		}
		public virtual bool PreCommit(Commit attempt)
		{
			return true;
		}
		public void PostCommit(Commit committed)
		{
			if (committed != null)
				this.scheduler.ScheduleDispatch(committed);
		}
	}
}