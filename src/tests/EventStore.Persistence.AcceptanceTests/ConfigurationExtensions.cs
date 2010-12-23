namespace EventStore.Persistence.AcceptanceTests
{
	using System;
	using System.Configuration;
	using System.Linq;

	public static class ConfigurationExtensions
	{
		public static string GetSetting(this string settingName)
		{
			return GetEnvironmentVariable("/" + settingName + ":")
				?? ConfigurationManager.AppSettings[settingName];
		}
		private static string GetEnvironmentVariable(string name)
		{
			return Environment.GetCommandLineArgs()
				.Where(arg => arg.StartsWith(name))
				.Select(arg => arg.Replace(name, string.Empty))
				.FirstOrDefault();
		}
	}
}