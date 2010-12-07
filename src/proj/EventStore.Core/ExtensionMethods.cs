namespace EventStore.Core
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Reflection;

	internal static class ExtensionMethods
	{
		public static string FormatWith(this string format, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, format, values);
		}
		public static bool HasAttribute<T>(this Type type) where T : Attribute
		{
			return type.GetCustomAttributes(typeof(T), false).Length > 0;
		}
		public static Assembly[] LoadAssemblies(this IEnumerable<string> searchPatterns)
		{
			return searchPatterns
				.Where(pattern => !string.IsNullOrEmpty(pattern))
				.SelectMany(GetAssemblyFiles)
				.Select(assemblyFileName => assemblyFileName.LoadAssembly())
				.Where(assembly => assembly != null)
				.ToArray();
		}
		private static IEnumerable<string> GetAssemblyFiles(string pattern)
		{
			var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
			return new DirectoryInfo(currentDirectory).GetFiles(pattern, SearchOption.AllDirectories)
				.Select(x => x.FullName);
		}
		private static Assembly LoadAssembly(this string assemblyFileName)
		{
			try
			{
				return Assembly.LoadFrom(assemblyFileName);
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}