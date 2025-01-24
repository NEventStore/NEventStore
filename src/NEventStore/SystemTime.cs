namespace NEventStore
{
    /// <summary>
    ///     Provides the ability to override the current moment in time to facilitate testing.
    ///     Original idea by Ayende Rahien:
    ///     http://ayende.com/Blog/archive/2008/07/07/Dealing-with-time-in-tests.aspx
    /// </summary>
    public static class SystemTime
    {
        /// <summary>
        ///     The callback to be used to resolve the current moment in time.
        /// </summary>
        public static Func<DateTime>? Resolver { get; set; }

        /// <summary>
        ///     Gets the current moment in time.
        /// </summary>
        public static DateTime UtcNow
        {
            get { return Resolver == null ? DateTime.UtcNow : Resolver(); }
        }
    }
}