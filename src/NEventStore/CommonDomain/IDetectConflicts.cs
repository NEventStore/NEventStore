namespace CommonDomain
{
	using System;
	using System.Collections.Generic;

	public interface IDetectConflicts
	{
		void Register<TUncommitted, TCommitted>(ConflictDelegate<TUncommitted, TCommitted> handler)
			where TUncommitted : class
			where TCommitted : class;

		bool ConflictsWith(IEnumerable<object> uncommittedEvents, IEnumerable<object> committedEvents);
	}

	[Obsolete("Use ConflictDelegate<TUncommitted, TCommitted> delegate instead")]
	public delegate bool ConflictDelegate(object uncommitted, object committed);

	public delegate bool ConflictDelegate<in TUncommitted, in TCommitted>(TUncommitted uncommitted, TCommitted committed)
		where TUncommitted : class
		where TCommitted : class;

	public static class DetectConflictsExtensions
	{
		/// <summary>
		///   Provides backward compatibility for untyped ConflictDelegate users
		/// </summary>
		// ReSharper disable once CSharpWarnings::CS0612
		[Obsolete("Use Register<TUncommitted, TCommitted>(ConflictDelegate<TUncommitted, TCommitted>) overload instead")]
		public static void Register<TUncommitted, TCommitted>(this IDetectConflicts conflictDetector, ConflictDelegate handler)
			where TUncommitted : class
			where TCommitted : class
		{
			conflictDetector.Register<TUncommitted, TCommitted>((uncommitted, committed) => handler(uncommitted, committed));
		}
	}
}