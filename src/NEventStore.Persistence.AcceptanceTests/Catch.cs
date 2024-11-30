using System;

namespace NEventStore.Persistence.AcceptanceTests;

public static class Catch
{
    public static Exception Exception(Action action)
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