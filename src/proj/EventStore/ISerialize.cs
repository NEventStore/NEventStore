namespace EventStore
{
	/// <summary>
	/// 
	/// </summary>
	public interface ISerialize
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		byte[] Serialize(object graph);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="input"></param>
		/// <returns></returns>
		T Deserialize<T>(byte[] input);
	}
}