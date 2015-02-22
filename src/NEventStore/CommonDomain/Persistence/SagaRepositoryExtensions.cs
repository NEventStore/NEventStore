namespace CommonDomain.Persistence
{
    using System;
    using System.Collections.Generic;
    using NEventStore;

    public static class SagaRepositoryExtensions
    {
        public static TSaga GetById<TSaga>(this ISagaRepository sagaRepository, Guid sagaId)
            where TSaga : class, ISaga
        {
            return sagaRepository.GetById<TSaga>(Bucket.Default, sagaId.ToString());
        }

        public static void Save(
            this ISagaRepository sagaRepository,
            ISaga saga,
            Guid commitId,
            Action<IDictionary<string, object>> updateHeaders)
        {
            sagaRepository.Save(Bucket.Default, saga, commitId, updateHeaders);
        }

        public static TSaga GetById<TSaga>(this ISagaRepository sagaRepository, string sagaId)
            where TSaga : class, ISaga
        {
            return sagaRepository.GetById<TSaga>(Bucket.Default, sagaId);
        }
    }
}