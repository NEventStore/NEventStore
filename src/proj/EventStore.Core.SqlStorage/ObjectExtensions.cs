namespace EventStore.Core.SqlStorage
{
	using System;

	public static class ObjectExtensions
	{
		public static byte[] ToNull(this Guid value)
		{
			return value == Guid.Empty ? null : value.ToByteArray();
		}
		public static object ToNull(this long value)
		{
			return value == 0 ? null : (object)value;
		}
	}
}