namespace EventStore.Persistence.AcceptanceTests
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
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

			try
			{
				var factory = (IPersistenceFactory)Activator.CreateInstance(type);
				var key = factory.GetType().Name
					.Replace("AcceptanceTest", string.Empty)
					.Replace("Factory", string.Empty);

				Factories[key] = factory;
			}
			catch
			{
				return; // no-op (added to suppress a warning)
			}
		}

		public virtual IPersistenceFactory GetFactory()
		{
			var persistenceEngine = "persistence".GetSetting() ?? "MsSqlPersistence";

			try
			{
				return Factories[persistenceEngine];
			}
			catch (KeyNotFoundException)
			{
				var message = string.Format(
					CultureInfo.InvariantCulture,
					"The key '{0}' was not a configured persistence engine.",
					persistenceEngine);

				throw new StorageException(message);
			}
		}

		public virtual int PageSize
		{
			get { return int.Parse("pageSize".GetSetting() ?? "0"); }
		}
	}
}