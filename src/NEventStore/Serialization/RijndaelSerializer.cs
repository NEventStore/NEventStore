using System.Collections;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using NEventStore.Logging;

namespace NEventStore.Serialization
{
    /// <summary>
    ///    Represents a serializer that encrypts the serialized data using the Rijndael algorithm.
    /// </summary>
    public class RijndaelSerializer : ISerialize
    {
        private const int KeyLength = 16; // bytes
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(RijndaelSerializer));
        private readonly byte[] _encryptionKey;
        private readonly ISerialize _inner;

        /// <summary>
        /// Initializes a new instance of the RijndaelSerializer class.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public RijndaelSerializer(ISerialize inner, byte[] encryptionKey)
        {
            if (!KeyIsValid(encryptionKey, KeyLength))
            {
                throw new ArgumentException(Messages.InvalidKeyLength, "encryptionKey");
            }

            _encryptionKey = encryptionKey;
            _inner = inner;
        }

        /// <inheritdoc/>
        public virtual void Serialize<T>(Stream output, T graph)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.SerializingGraph, typeof(T));
            }

            using (var rijndael = new RijndaelManaged())
            {
                rijndael.Key = _encryptionKey;
                rijndael.Mode = CipherMode.CBC;
                rijndael.GenerateIV();

                using (ICryptoTransform encryptor = rijndael.CreateEncryptor())
                using (var wrappedOutput = new IndisposableStream(output))
                using (var encryptionStream = new CryptoStream(wrappedOutput, encryptor, CryptoStreamMode.Write))
                {
                    wrappedOutput.Write(rijndael.IV, 0, rijndael.IV.Length);
                    _inner.Serialize(encryptionStream, graph);
                    encryptionStream.Flush();
                    encryptionStream.FlushFinalBlock();
                }
            }
        }

        /// <inheritdoc/>
        public virtual T Deserialize<T>(Stream input)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.DeserializingStream, typeof(T));
            }

            using (var rijndael = new RijndaelManaged())
            {
                rijndael.Key = _encryptionKey;
                rijndael.IV = GetInitVectorFromStream(input, rijndael.IV.Length);
                rijndael.Mode = CipherMode.CBC;

                using (ICryptoTransform decryptor = rijndael.CreateDecryptor())
                using (var decryptedStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read))
                    return _inner.Deserialize<T>(decryptedStream);
            }
        }

        private static bool KeyIsValid(ICollection key, int length)
        {
            return key != null && key.Count == length;
        }

        private static byte[] GetInitVectorFromStream(Stream encrypted, int initVectorSizeInBytes)
        {
            var buffer = new byte[initVectorSizeInBytes];
            encrypted.Read(buffer, 0, buffer.Length);
            return buffer;
        }
    }
}