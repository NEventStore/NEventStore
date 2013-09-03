namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    internal static class Guard
    {
        internal static void Not(bool condition, Func<Exception> createException)
        {
            if (!condition)
            {
                return;
            }
            throw createException();
        }

        internal static void NotNullOrWhiteSpace(Expression<Func<string>> reference, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(GetParameterName(reference));
            }
        }


        internal static void NotNull<T>(Expression<Func<T>> reference, T value)
        {
            if (ReferenceEquals(value, null))
            {
                throw new ArgumentNullException(GetParameterName(reference));
            }
        }

        internal static void NotLessThanOrEqualTo<T>(Expression<Func<T>> reference, T value, T compareTo)
            where T: IComparable
        {
            NotNull(reference, value);
            if (value.CompareTo(compareTo) <= 0)
            {
                throw new ArgumentOutOfRangeException("{0} has value {1} which is less than or equal to {2}".FormatWith(GetParameterName(reference), value, compareTo));
            }
        }

        internal static void NotLessThan<T>(Expression<Func<T>> reference, T value, T compareTo)
            where T : IComparable
        {
            NotNull(reference, value);
            if (value.CompareTo(compareTo) < 0)
            {
                throw new ArgumentOutOfRangeException("{0} has value {1} which is less than {2}".FormatWith(GetParameterName(reference), value, compareTo));
            }
        }

        internal static void NotDefault<T>(Expression<Func<T>> reference, T value)
            where T : IComparable
        {
            NotNull(reference, value);
            if (value.CompareTo(default(T)) == 0)
            {
                throw new ArgumentException("{0} has value {1} which cannot be equal to it's default value {2}".FormatWith(GetParameterName(reference), value, default(T)));
            }
        }

        internal static void NotEmpty<T>(Expression<Func<IEnumerable<T>>> reference, IEnumerable<T> value)
        {
            NotNull(reference, value);
            if (!value.Any())
            {
                throw new ArgumentException("{0} cannot be empty".FormatWith(GetParameterName(reference), value, default(T)));
            }
        }

        private static string GetParameterName(LambdaExpression reference)
        {
            return ((MemberExpression) reference.Body).Member.Name;
        }
    }
}