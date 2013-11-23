namespace NEventStore.Persistence.MongoDB
{
	using System;
	using global::MongoDB.Driver;
	using NEventStore.Serialization;

	/// <summary>
	/// Options for the MongoPersistence engine.
	/// http://docs.mongodb.org/manual/core/write-concern/#write-concern
	/// </summary>
	public  class MongoPersistenceOptions
	{
		/// <summary>
		/// Get the  <see href="http://docs.mongodb.org/manual/core/write-concern/#write-concern">WriteConcern</see> for the commit insert operation.
		/// Concurrency / duplicate commit detection require a safe mode so level should be at least Acknowledged
		/// </summary>
		/// <returns>the write concern for the commit insert operation</returns>
		public virtual WriteConcern GetInsertCommitWriteConcern()
		{
			// for concurrency / duplicate commit detection safe mode is required
			// minimum level is Acknowledged
			return WriteConcern.Acknowledged;
		}

		public virtual MongoCollectionSettings GetCommitSettings()
		{
			return new MongoCollectionSettings
			{
				AssignIdOnInsert = false,
				WriteConcern = WriteConcern.Acknowledged
			};
		}

		public virtual MongoCollectionSettings GetSnapshotSettings()
		{
			return new MongoCollectionSettings
			{
				AssignIdOnInsert = false,
				WriteConcern = WriteConcern.Unacknowledged
			};
		}

		public virtual MongoCollectionSettings GetStreamSettings()
		{
			return new MongoCollectionSettings
			{
				AssignIdOnInsert = false,
				WriteConcern = WriteConcern.Unacknowledged
			};
		}


		/// <summary>
		/// Connects to NEvenstore Mongo database
		/// </summary>
		/// <param name="connectionString">Connection string</param>
		/// <returns>nevenstore mongodatabase store</returns>
		public virtual MongoDatabase ConnectToDatabase(string connectionString)
		{
			var builder = new MongoUrlBuilder(connectionString);
			MongoDatabase database = (new MongoClient(connectionString)).GetServer().GetDatabase(builder.DatabaseName);
			return database;
		}
	}
}
