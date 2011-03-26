namespace EventStore.Example
{
	public class PreventUnauthorizedReadHook : IReadHook
	{
		public Commit Select(Commit committed)
		{
			// Authorization or other logging may be done here.
			return committed;
		}
	}
}