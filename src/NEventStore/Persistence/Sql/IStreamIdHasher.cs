namespace NEventStore.Persistence.Sql
{
    /// <summary>
    /// Defines a method to generate a hash of a stream ID.
    /// </summary>
    public interface IStreamIdHasher
    {
        /// <summary>
        /// Gets a hash of the stream ID. Hash length must be less than or equal to 40 characters.
        /// </summary>
        /// <param name="streamId">The stream ID to be hashed.</param>
        /// <returns>A hash of the stream Id.</returns>
        string GetHash(string streamId);
    }
}