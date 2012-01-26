namespace EventStore.Example
{
	using System;
	using System.Transactions;
	using Dispatcher;

	internal static class MainProgram
	{
		private static readonly Guid StreamId = Guid.NewGuid(); // aggregate identifier
		private static readonly byte[] EncryptionKey = new byte[]
		{
			0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf
		};
		private static IStoreEvents store;

		private static void Main()
		{
			using (var scope = new TransactionScope())
			using (store = WireupEventStore())
			{
				OpenOrCreateStream();
				AppendToStream();
				TakeSnapshot();
				LoadFromSnapshotForwardAndAppend();
				scope.Complete();
			}

			Console.WriteLine(Resources.PressAnyKey);
			Console.ReadKey();
		}

		private static IStoreEvents WireupEventStore()
		{
			 return Wireup.Init()
				.LogToOutputWindow()
				.UsingSqlPersistence("EventStore") // Connection string is in app.config
					.EnlistInAmbientTransaction() // two-phase commit
					.InitializeStorageEngine()
					.UsingJsonSerialization()
						.Compress()
						.EncryptWith(EncryptionKey)
				.HookIntoPipelineUsing(new[] { new AuthorizationPipelineHook() })
				.UsingAsynchronousDispatchScheduler()
					.DispatchTo(new DelegateMessageDispatcher(DispatchCommit))
				.Build();
		}
		private static void DispatchCommit(Commit commit)
		{
			// This is where we'd hook into our messaging infrastructure, such as NServiceBus,
			// MassTransit, WCF, or some other communications infrastructure.
			// This can be a class as well--just implement IDispatchCommits.
			try
			{
				foreach (var @event in commit.Events)
					Console.WriteLine(Resources.MessagesDispatched + ((SomeDomainEvent)@event.Body).Value);
			}
			catch (Exception)
			{
				Console.WriteLine(Resources.UnableToDispatch);
			}
		}

		private static void OpenOrCreateStream()
		{
			// we can call CreateStream(StreamId) if we know there isn't going to be any data.
			// or we can call OpenStream(StreamId, 0, int.MaxValue) to read all commits,
			// if no commits exist then it creates a new stream for us.
			using (var stream = store.OpenStream(StreamId, 0, int.MaxValue))
			{
				var @event = new SomeDomainEvent { Value = "Initial event." };

				stream.Add(new EventMessage { Body = @event });
				stream.CommitChanges(Guid.NewGuid());
			}
		}
		private static void AppendToStream()
		{
			using (var stream = store.OpenStream(StreamId, int.MinValue, int.MaxValue))
			{
				var @event = new SomeDomainEvent { Value = "Second event." };

				stream.Add(new EventMessage { Body = @event });
				stream.CommitChanges(Guid.NewGuid());
			}
		}
		private static void TakeSnapshot()
		{
			var memento = new AggregateMemento { Value = "snapshot" };
			store.Advanced.AddSnapshot(new Snapshot(StreamId, 2, memento));
		}
		private static void LoadFromSnapshotForwardAndAppend()
		{
			var latestSnapshot = store.Advanced.GetSnapshot(StreamId, int.MaxValue);

			using (var stream = store.OpenStream(latestSnapshot, int.MaxValue))
			{
				var @event = new SomeDomainEvent { Value = "Third event (first one after a snapshot)." };

				stream.Add(new EventMessage { Body = @event });
				stream.CommitChanges(Guid.NewGuid());
			}
		}
	}
}