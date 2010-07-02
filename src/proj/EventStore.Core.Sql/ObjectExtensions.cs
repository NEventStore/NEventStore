namespace EventStore.Core.Sql
{
	using System;

	public static class ObjectExtensions
	{
		public static object ToNull(this Guid value)
		{
			return Guid.Empty == value ? null : (object)value;
		}
	}
}