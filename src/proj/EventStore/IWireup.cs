namespace EventStore
{
	/// <summary>
	/// Indicates the ability to perform component initialization and wireup.
	/// </summary>
	public interface IWireup
	{
		/// <summary>
		/// Registers the object instance provided.
		/// </summary>
		/// <typeparam name="T">The type of object to be registered.</typeparam>
		/// <param name="instance">The object instance to be resolved during construction.</param>
		/// <returns>An instance of the <see cref="IWireup"/> interface.</returns>
		IWireup With<T>(T instance) where T : class;

		/// <summary>
		/// Builds an object instance of the <see cref="IStoreEvents"/> interface according to the wireup configuration provided.
		/// </summary>
		/// <returns>An object instance of the <see cref="IStoreEvents"/> interface.</returns>
		IStoreEvents Build();
	}
}