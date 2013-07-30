namespace NEventStore.Serialization
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Security.Cryptography;
    using Logging;

    public class RijndaelSerializer : ISerialize
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(RijndaelSerializer));
		private const int KeyLength = 16; // bytes
		private readonly ISerialize inner;
		private readonly byte[] encryptionKey;

		public RijndaelSerializer(ISerialize inner, byte[] encryptionKey)
		{
			if (!KeyIsValid(encryptionKey, KeyLength))
				throw new ArgumentException(Messages.InvalidKeyLength, "encryptionKey");

			this.encryptionKey = encryptionKey;
			this.inner = inner;
		}
		private static bool KeyIsValid(ICollection key, int length)
		{
			return key != null && key.Count == length;
		}

		public virtual void Serialize<T>(Stream output, T graph)
		{
			Logger.Verbose(Messages.SerializingGraph, typeof(T));

			using (var rijndael = new RijndaelManaged())
			{
				rijndael.Key = this.encryptionKey;
				rijndael.Mode = CipherMode.CBC;
				rijndael.GenerateIV();

				using (var encryptor = rijndael.CreateEncryptor())
				using (var wrappedOutput = new IndisposableStream(output))
				using (var encryptionStream = new CryptoStream(wrappedOutput, encryptor, CryptoStreamMode.Write))
				{
					wrappedOutput.Write(rijndael.IV, 0, rijndael.IV.Length);
					this.inner.Serialize(encryptionStream, graph);
					encryptionStream.Flush();
					encryptionStream.FlushFinalBlock();
				}
			}
		}

		public virtual T Deserialize<T>(Stream input)
		{
			Logger.Verbose(Messages.DeserializingStream, typeof(T));

			using (var rijndael = new RijndaelManaged())
			{
				rijndael.Key = this.encryptionKey;
				rijndael.IV = GetInitVectorFromStream(input, rijndael.IV.Length);
				rijndael.Mode = CipherMode.CBC;

				using (var decryptor = rijndael.CreateDecryptor())
				using (var decryptedStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read))
					return this.inner.Deserialize<T>(decryptedStream);
			}
		}
		private static byte[] GetInitVectorFromStream(Stream encrypted, int initVectorSizeInBytes)
		{
			var buffer = new byte[initVectorSizeInBytes];
			encrypted.Read(buffer, 0, buffer.Length);
			return buffer;
		}
	}
}