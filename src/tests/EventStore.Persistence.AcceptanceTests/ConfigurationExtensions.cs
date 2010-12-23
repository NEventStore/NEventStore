namespace EventStore.Persistence.AcceptanceTests
{
	using System;
	using System.Configuration;
	using System.Linq;

	public static class ConfigurationExtensions
	{
		public static string GetSetting(this string settingName)
		{
			return GetCommandLineArgument("/" + settingName + ":")
				?? Environment.GetEnvironmentVariable(settingName)
				?? ConfigurationManager.AppSettings[settingName];
		}
		private static string GetCommandLineArgument(string settingName)
		{
			return Environment.GetCommandLineArgs()
				.Where(arg => arg.StartsWith(settingName))
				.Select(arg => arg.Replace(settingName, string.Empty))
				.FirstOrDefault();
		}
	}
}