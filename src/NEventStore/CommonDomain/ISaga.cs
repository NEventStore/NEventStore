namespace CommonDomain
{
	using System;
	using System.Collections;

	public interface ISaga
	{
		Guid Id { get; }

		int Version { get; }

		void Transition(object message);

		ICollection GetUncommittedEvents();

		void ClearUncommittedEvents();

		ICollection GetUndispatchedMessages();

		void ClearUndispatchedMessages();
	}
}