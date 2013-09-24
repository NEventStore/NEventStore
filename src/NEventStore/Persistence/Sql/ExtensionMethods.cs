namespace NEventStore.Persistence.Sql
{
    using System;
    using System.Data;
    using NEventStore.Logging;

    internal static class ExtensionMethods
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (ExtensionMethods));

        public static Guid ToGuid(this object value)
        {
            if (value is Guid)
            {
                return (Guid) value;
            }

            var bytes = value as byte[];
            return bytes != null ? new Guid(bytes) : Guid.Empty;
        }

        public static int ToInt(this object value)
        {
            return value is int
                ? (int) value
                : value is long
                    ? (int) (long) value
                    : value is decimal ? (int) (decimal) value : Convert.ToInt32(value);
        }

        public static long ToLong(this object value)
        {
            return value is long
                ? (long) value
                : value is int
                    ? (int) value
                    : value is decimal ? (long) (decimal) value : Convert.ToInt32(value);
        }

        public static DateTime ToDateTime(this object value)
        {
            value = value is decimal ? (long) (decimal) value : value;
            return value is long ? new DateTime((long) value) : ((DateTime) value).ToUniversalTime();
        }

        public static IDbCommand SetParameter(this IDbCommand command, string name, object value)
        {
            Logger.Verbose("Rebinding parameter '{0}' with value: {1}", name, value);
            var parameter = (IDataParameter) command.Parameters[name];
            parameter.Value = value;
            return command;
        }
    }
}