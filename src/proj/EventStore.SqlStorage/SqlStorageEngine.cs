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
			using (var query = this.builder.BuildLoadByIdQuery(id, maxStartingVersion))
			using (var reader = this.WrapOnFailure(query.ExecuteReader, 0))
				return this.BuildStream(reader, id);
		}
		public ICollection LoadStartingAfter(Guid id, long version)
		{
			if (id == Guid.Empty)
				return new object[0];

			using (var query = this.builder.BuildLoadStartingAfterQuery(id, version))
			using (var reader = this.WrapOnFailure(query.ExecuteReader, 0))
				return this.BuildStream(reader, Guid.Empty).Events;
		}
		public ICollection LoadByCommandId(Guid commandId)
		{
			if (commandId == Guid.Empty)
				return new object[] { };

			using (var query = this.builder.BuildLoadByCommandIdQuery(commandId))
			using (var reader = this.WrapOnFailure(query.ExecuteReader, 0))
				return this.BuildStream(reader, Guid.Empty).Events;
		}
		private CommittedEventStream BuildStream(IDataReader reader, Guid id)
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
			using (var command = this.builder.BuildSaveCommand(stream, this.serializer))
				this.WrapOnFailure(command.ExecuteNonQuery, stream.ExpectedVersion);
		}
		private TResult WrapOnFailure<TResult>(Func<TResult> func, long version)
		{
			try
			{
				return func();
			}
			catch (DbException exception)
			{
				if (this.builder.Dialect.IsDuplicateKey(exception))
					throw new DuplicateKeyException(exception.Message, exception);

				var constraintViolation = this.builder.Dialect.IsConstraintViolation(exception);
				if (constraintViolation && version > 0)
					throw new CrossTenantAccessException();

				var message = constraintViolation ? SqlMessages.ConstraintViolation : exception.Message;
				throw new StorageEngineException(message, exception);
			}
		}
	}
}