namespace EventStore.Example
{
	using System;
	using Dispatcher;
	using Persistence;
	using Persistence.SqlPersistence;
	using Serialization;

	internal static class MainProgram
	{
		private static readonly byte[] EncryptionKey = new byte[]
		{
			0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf
		};

		private static void Main()
		{
			using (var eventStore = BuildEventStore())
			{
				ExampleUsage.Show(eventStore); // look in this class for how to use the EventStore.

				Console.WriteLine(Resources.PressAnyKey);
				Console.ReadLine();
			}
		}

		private static IStoreEvents BuildEventStore()
		{
			var persistence = BuildPersistenceEngine();
			persistence.Initialize();

			var dispatcher = BuildDispatcher(persistence);
			return new OptimisticEventStore(persistence, dispatcher);
		}
		private static IPersistStreams BuildPersistenceEngine()
		{
			return new SqlPersistenceFactory(
				"EventStore",
				BuildSerializer()).Build();
		}
		private static ISerialize BuildSerializer()
		{
			var serializer = new JsonSerializer() as ISerialize;
			serializer = new GzipSerializer(serializer);
			return new RijndaelSerializer(serializer, EncryptionKey);
		}
		private static IDispatchCommits BuildDispatcher(IPersistStreams persistence)
		{
			return new AsynchronousDispatcher(
				new DelegateMessagePublisher(DispatchCommit),
				persistence,
				OnDispatchError);
		}
		private static void DispatchCommit(Commit commit)
		{
			// this is where we'd hook into our messaging infrastructure, e.g. NServiceBus.
			Console.WriteLine(Resources.MessagesPublished);
		}
		private static void OnDispatchError(Commit commit, Exception exception)
		{
			// if for some reason our messaging infrastructure couldn't dispatch the messages we've committed
			// we would be alerted here.
			Console.WriteLine(Resources.ErrorWhilePublishing);
		}
	}
}