namespace EventStore.Persistence.AcceptanceTests
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;

	public class PersistenceFactoryScanner
	{
		private static readonly IDictionary<string, IPersistenceFactory> Factories =
			new Dictionary<string, IPersistenceFactory>();

		public PersistenceFactoryScanner()
		{
			if (Factories.Count > 0)
				return;

			foreach (var type in GetAssemblyFiles().SelectMany(GetTypes))
				AddFactory(type);
		}

		private static IEnumerable<string> GetAssemblyFiles()
		{
            return Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
		}

		private static IEnumerable<Type> GetTypes(string filename)
		{
			try
			{
				return Assembly.LoadFrom(filename).GetTypes();
			}
			catch (FileLoadException)
			{
				return new Type[] { };
			}
			catch (Exception)
			{
				return new Type[] { };
			}
		}
		private static void AddFactory(Type type)
		{
			if (!typeof(IPersistenceFactory).IsAssignableFrom(type))
				return;

			if (typeof(IPersistenceFactory) == type || type.IsAbstract)
				return;

			var factory = (IPersistenceFactory)Activator.CreateInstance(type);
			Factories[factory.GetType().Name] = factory;
		}

		public virtual IPersistenceFactory GetFactory()
		{
			var persistenceEngine = "persistence".GetSetting() ?? "MsSqlPersistence";
			return Factories[persistenceEngine + "Factory"];
		}
	}
}