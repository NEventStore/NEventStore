namespace CommonDomain.Persistence
{
    using System;

    public interface IConstructSagas
    {
        ISaga Build(Type type);
    }
}