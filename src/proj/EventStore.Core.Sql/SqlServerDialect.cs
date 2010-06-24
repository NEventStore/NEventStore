namespace EventStore.Core.Sql
{
	public sealed class SqlServerDialect : SqlDialect
	{
		private const string SelectEvents =
			@"SELECT Payload
			    FROM [dbo].[Events]
			   WHERE [AggregateId] = @id
			     AND [version] >= @version;";

		private const string InsertEvents =
			@"INSERT
                INTO [dbo].[Aggregates]
			  SELECT @id, 0, @type, @created
			   WHERE NOT EXISTS
                   ( SELECT *
				       FROM [dbo].[Aggregates]
			          WHERE [AggregateId] = @id );

			  SELECT @version = [Version] FROM [dbo].[Aggregates] WHERE [AggregateId] = @id;

			  {0}

			  SELECT @version = @version - 1;
			  UPDATE [dbo].[Aggregates] SET [Version] = @version;";

		private const string InsertEvent = 
			@"INSERT INTO [dbo].[Events] SELECT @id, @version + {0}, @type{0}, @created, @payload{0};
			  SELECT @version = @version + 1;";

		private const string SelectSnapshot =
			@"SELECT TOP 1 Payload
			    FROM [dbo].[Events]
			   WHERE [AggregateId] = @id
			   ORDER BY [version];";

		private const string InsertSnapshot =
			@"INSERT INTO [dbo].[Events] SELECT @id, @version, @type, @created, @payload;";

		public override string IdParameter
		{
			get { return "@id"; }
		}
		public override string VersionParameter
		{
			get { return "@version"; }
		}
		public override string RuntimeTypeParameter
		{
			get { return "@type"; }
		}
		public override string CreatedParameter
		{
			get { return "@created"; }
		}
		public override string PayloadParameter
		{
			get { return "@payload"; }
		}

		public override string LoadEvents
		{
			get { return SelectEvents; }
		}
		public override string StoreEvents
		{
			get { return InsertEvents; }
		}
		public override string StoreEvent
		{
			get { return InsertEvent; }
		}
		public override string LoadSnapshot
		{
			get { return SelectSnapshot; }
		}
		public override string StoreSnapshot
		{
			get { return InsertSnapshot; }
		}
	}
}