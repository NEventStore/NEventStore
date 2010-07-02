namespace EventStore.Core.SqlStorage
{
	using System;

	public static class ObjectExtensions
	{
		public static byte[] ToNull(this Guid value)
		{
			return value == Guid.Empty ? null : value.ToByteArray();
		}
	}
}