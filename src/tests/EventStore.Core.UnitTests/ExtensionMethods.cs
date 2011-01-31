namespace EventStore.Core.UnitTests
{
	using System.Collections;
	using System.Collections.Generic;

	internal static class ExtensionMethods
	{
		public static int Events(this int events)
		{
			return events;
		}
	}

	public class EnumerableCounter : IEnumerable<Commit>
	{
		private readonly IEnumerable<Commit> enumerable;
		public int GetEnumeratorCallCount { get; private set; }

		public EnumerableCounter(IEnumerable<Commit> enumerable)
		{
			this.enumerable = enumerable;
			this.GetEnumeratorCallCount = 0;
		}
		public IEnumerator<Commit> GetEnumerator()
		{
			this.GetEnumeratorCallCount++;
			return this.enumerable.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}
}