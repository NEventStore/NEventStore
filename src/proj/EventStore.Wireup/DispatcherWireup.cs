namespace EventStore
{
	using Dispatcher;

	public class DispatcherWireup : Wireup
	{
		private readonly Wireup inner;

		public DispatcherWireup(Wireup inner)
		{
			this.inner = inner;
		}

		public override Wireup WithDispatcher(IDispatchCommits instance)
		{
			return this.inner.WithDispatcher(instance);
		}

		public override IStoreEvents Build()
		{
			return this.inner.Build();
		}
	}
}