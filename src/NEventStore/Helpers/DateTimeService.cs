using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NEventStore.Helpers
{
    public static class DateTimeService
    {
        private static Func<DateTime> _nowFunc = () => DateTime.Now;
        private static Func<DateTime> _utcNowFunc = () => DateTime.UtcNow;

        public static DateTime Now => _nowFunc();

        public static DateTime UtcNow => _utcNowFunc();

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