using System;

namespace EventStore
{
    /// <summary>
    /// Provides the ability to convert events from a specific commit
    /// </summary>
    public interface IConvertCommits : IDisposable
    {
        /// <summary>
        /// Converts all events in the commit, if converters are found
        /// </summary>
        /// <param name="commit">The commit containing the events to convert</param>
        /// <returns>The same commit containing the events which might have been converted</returns>
        Commit Convert(Commit commit);
    }
}