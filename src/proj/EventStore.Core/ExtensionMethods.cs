namespace EventStore
{
	using System;

	internal static class ExtensionMethods
	{
		public static bool IsValid(this Commit attempt)
		{
			if (attempt == null)
				throw new ArgumentNullException("attempt");

			if (!attempt.HasIdentifier())
				throw new ArgumentException("The commit must be uniquely identified.", "attempt");

			if (attempt.CommitSequence <= 0)
				throw new ArgumentException("The commit sequence must be a positive number.", "attempt");

			if (attempt.StreamRevision <= 0)
				throw new ArgumentException("The stream revision must be a positive number.", "attempt");

			if (attempt.StreamRevision < attempt.CommitSequence)
				throw new ArgumentException("The stream revision must always be greater than or equal to the commit sequence.", "attempt");

			return true;
		}

		public static bool HasIdentifier(this Commit attempt)
		{
			return attempt.StreamId != Guid.Empty && attempt.CommitId != Guid.Empty;
		}

		public static bool IsEmpty(this Commit attempt)
		{
			return attempt == null || attempt.Events.Count == 0;
		}
	}
}