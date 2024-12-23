using System.Globalization;

namespace NEventStore
{
    /// <summary>
    ///     A set of common methods used through the NEventStore.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        ///     Formats the string provided using the values specified.
        /// </summary>
        /// <param name="format">The string to be formatted.</param>
        /// <param name="values">The values to be embedded into the string.</param>
        /// <returns>The formatted string.</returns>
        public static string FormatWith(this string format, params object[] values)
        {
            return string.Format(CultureInfo.InvariantCulture, format ?? string.Empty, values);
        }
    }
}