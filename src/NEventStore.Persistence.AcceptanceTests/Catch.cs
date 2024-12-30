namespace NEventStore.Persistence.AcceptanceTests
{
    using System;

    public static class Catch
    {
        public static Exception? Exception(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }
    }
}