namespace EventStore
{
	using System;

	/// <summary>
	/// Provides the ability to override the current moment in time to facilitate testing.
	/// Original idea by Ayende Rahien:
	/// http://ayende.com/Blog/archive/2008/07/07/Dealing-with-time-in-tests.aspx
	/// </summary>
	public static class SystemTime
	{
		/// <summary>
		/// Returns the current moment in time via <see cref="DateTime" />.
		/// </summary>
		public static Func<DateTime> UtcNow = () => DateTime.UtcNow;
	}
}