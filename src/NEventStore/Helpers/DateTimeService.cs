namespace NEventStore.Helpers
{
    /// <summary>
    /// Provides a way to get the current date and time.
    /// Useful for testing.
    /// </summary>
    public static class DateTimeService
    {
        private static Func<DateTime> _nowFunc = () => DateTime.Now;
        private static Func<DateTime> _utcNowFunc = () => DateTime.UtcNow;

        /// <summary>
        /// Gets the current date and time.
        /// </summary>
        public static DateTime Now
        {
            get
            {
                return _nowFunc();
            }
        }

        /// <summary>
        /// Gets the current date and time in UTC.
        /// </summary>
        public static DateTime UtcNow
        {
            get
            {
                return _utcNowFunc();
            }
        }

        #region "test function"

        internal static DisposableAction Override(Func<DateTime> functor)
        {
            _nowFunc = functor;
            _utcNowFunc = () => functor().ToUniversalTime();
            return new DisposableAction(() =>
            {
                _nowFunc = () => DateTime.Now;
                _utcNowFunc = () => DateTime.UtcNow;
            });
        }

        internal static DisposableAction Override(DateTime overrideNowDate)
        {
            return Override(() => overrideNowDate);
        }

        #endregion
    }
}
