using System.Text;

namespace EventStore
{
	using System.Globalization;

	/// <summary>
	/// A set of common methods used through the EventStore.
	/// </summary>
	public static class ExtensionMethods
	{
		/// <summary>
		/// Formats the string provided using the values specified.
		/// </summary>
		/// <param name="format">The string to be formated.</param>
		/// <param name="values">The values to be embedded into the string.</param>
		/// <returns>The formatted string.</returns>
		public static string FormatWith(this string format, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, format ?? string.Empty, values);
		}

        /// <summary>
        /// Returns a string representation of the byte array
        /// </summary>
        /// <param name="ba">This byte array</param>
        /// <returns>The string value</returns>
        public static string ByteArrayToString(this byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (var b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
	}
}