namespace EventStore.Core.SqlStorage
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