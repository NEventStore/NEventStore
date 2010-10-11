namespace EventStore.Core.SqlStorage
{
	using System.Data;

	public class CommandBuilder
	{
		private readonly IDbConnection connection;
		private readonly IDbTransaction transaction;

		public CommandBuilder(IDbConnection connection)
			: this(connection, null)
		{
		}
		public CommandBuilder(IDbConnection connection, IDbTransaction transaction)
		{
			this.connection = connection;
			this.transaction = transaction;
		}
		public virtual IDbCommand Build(string commandText)
		{
			var command = this.connection.CreateCommand();
			command.Transaction = this.transaction;
			command.CommandText = commandText;
			return command;
		}
	}
}