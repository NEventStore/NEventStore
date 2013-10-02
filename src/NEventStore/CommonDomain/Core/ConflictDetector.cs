namespace CommonDomain.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	///   The conflict detector is used to determine if the events to be committed represent
	///   a true business conflict as compared to events that have already been committed, thus
	///   allowing reconciliation of optimistic concurrency problems.
	/// </summary>
	/// <remarks>
	///   The implementation contains some internal lambda "magic" which allows casting between
	///   TCommitted, TUncommitted, and System.Object and in a completely type-safe way.
	/// </remarks>
	public class ConflictDetector : IDetectConflicts
	{
		private readonly IDictionary<Type, IDictionary<Type, ConflictDelegate>> actions =
			new Dictionary<Type, IDictionary<Type, ConflictDelegate>>();

		public void Register<TUncommitted, TCommitted>(ConflictDelegate handler) where TUncommitted : class
			where TCommitted : class
		{
			IDictionary<Type, ConflictDelegate> inner;
			if (!this.actions.TryGetValue(typeof(TUncommitted), out inner))
			{
				this.actions[typeof(TUncommitted)] = inner = new Dictionary<Type, ConflictDelegate>();
			}

			inner[typeof(TCommitted)] = (uncommitted, committed) => handler(uncommitted as TUncommitted, committed as TCommitted);
		}

		public bool ConflictsWith(IEnumerable<object> uncommittedEvents, IEnumerable<object> committedEvents)
		{
			return (from object uncommitted in uncommittedEvents
			        from object committed in committedEvents
			        where this.Conflicts(uncommitted, committed)
			        select uncommittedEvents).Any();
		}

		private bool Conflicts(object uncommitted, object committed)
		{
			IDictionary<Type, ConflictDelegate> registration;
			if (!this.actions.TryGetValue(uncommitted.GetType(), out registration))
			{
				return uncommitted.GetType() == committed.GetType(); // no reg, only conflict if the events are the same time
			}

			ConflictDelegate callback;
			if (!registration.TryGetValue(committed.GetType(), out callback))
			{
				return true;
			}

			return callback(uncommitted, committed);
		}
	}
}