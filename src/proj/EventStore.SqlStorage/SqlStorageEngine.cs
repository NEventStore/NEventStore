namespace EventStore.SqlStorage
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;

	public class SqlStorageEngine : IStorageEngine
	{
		private const int SerializedDataColumnIndex = 0;
		private const int VersionColumnIndex = 1;
		private readonly IBuildStatements builder;
		private readonly ISerializeObjects serializer;

		public SqlStorageEngine(IBuildStatements builder, ISerializeObjects serializer)
		{
			this.builder = builder;
			this.serializer = serializer;
		}

		public CommittedEventStream LoadById(Guid id, long maxStartingVersion)
		{
			return this.Load(id, this.builder.BuildLoadByIdQuery(id, maxStartingVersion));
		}
		public ICollection LoadStartingAfter(Guid id, long version)
		{
			if (id == Guid.Empty)
				return new object[0];

			return this.Load(id, this.builder.BuildLoadStartingAfterQuery(id, version)).Events;
		}
		public ICollection LoadByCommandId(Guid commandId)
		{
			if (commandId == Guid.Empty)
				return new object[] { };

			return this.Load(Guid.Empty, this.builder.BuildLoadByCommandIdQuery(commandId)).Events;
		}
		private CommittedEventStream Load(Guid id, IDbCommand query)
		{
			using (query)
			using (var reader = this.WrapOnFailure(query.ExecuteReader))
				return this.BuildStream(id, reader);
		}
		private CommittedEventStream BuildStream(Guid id, IDataReader reader)
		{
			ICollection<object> events = new LinkedList<object>();
			object snapshot = null;

			while (reader.Read())
				events.Add(this.serializer.Deserialize<object>(reader[SerializedDataColumnIndex] as byte[]));

			long version = 0;
			if (reader.NextResult() && reader.Read())
			{
				snapshot = this.serializer.Deserialize<object>(reader[SerializedDataColumnIndex] as byte[]);
				version = (long)reader[VersionColumnIndex];
			}

			return new CommittedEventStream(id, version + events.Count, (ICollection)events, snapshot);
		}

		public void Save(UncommittedEventStream stream)
		{
			using (var query = this.builder.BuildSaveCommand(stream, this.serializer))
				this.WrapOnFailure(query.ExecuteNonQuery);
		}
		private TResult WrapOnFailure<TResult>(Func<TResult> func)
		{
			try
			{
				return func();
			}
			catch (DbException exception)
			{
				if (this.builder.IsDuplicateKey(exception))
					throw new DuplicateKeyException(exception.Message, exception);

				var message = this.builder.IsConstraintViolation(exception)
				              	? SqlMessages.ConstraintViolation
				              	: exception.Message;

				throw new StorageEngineException(message, exception);
			}
		}
	}
}