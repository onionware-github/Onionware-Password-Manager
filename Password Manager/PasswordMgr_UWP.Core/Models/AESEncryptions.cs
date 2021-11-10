using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PasswordMgr_UWP.Core.Models
{
    /// <summary>
    /// Provides methods to encrypt/decrypt strings
    /// </summary>
    public static class AESEncryptions
    {
        /// <summary>
        /// Encrypts a string.
        /// </summary>
        /// <param name="text">The string to encrypt.</param>
        /// <param name="password">The password to encrypt the string.</param>
        /// <param name="iv">The InitializationVector to use for AES.</param>
        /// <returns>A byte array that contains the encrypted string</returns>
        public static async Task<byte[]> EncryptStringAsync(string text, string password, byte[] iv)
        {
            byte[] key = hasher.ComputeHash(Encoding.UTF8.GetBytes(password));

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                var encryptor = aes.CreateEncryptor();
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var sw = new StreamWriter(cs))
                            await sw.WriteAsync(text);

                        return ms.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Encrypts a string.
        /// </summary>
        /// <param name="aes">The Aes object to get the key and IV from.</param>
        /// <param name="text">The string to encrypt.</param>
        /// <returns>A byte array that contains the encrypted string.</returns>
        public static async Task<byte[]> EncryptStringAsync(this Aes aes, string text)
        {
            using (aes)
            {
                var encryptor = aes.CreateEncryptor();
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var sw = new StreamWriter(cs))
                            await sw.WriteAsync(text);

                        return ms.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Decrypts a string.
        /// </summary>
        /// <param name="encrypted">The encrypted string as a byte[]</param>
        /// <param name="password">The password to decrypt the string</param>
        /// <param name="iv">The InitializationVector to use for AES Decryption of the string</param>
        /// <returns>The decrypted string.</returns>
        public static async Task<string> DecryptStringAsync(byte[] encryptedString, string password, byte[] iv)
        {
            byte[] key = hasher.ComputeHash(Encoding.UTF8.GetBytes(password));

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                var decryptor = aes.CreateDecryptor();
                using (var ms = new MemoryStream(encryptedString))
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (var sr = new StreamReader(cs))
                            return await sr.ReadToEndAsync();
                    }
                }
            }
        }

        public static async Task<string> DecryptStringAsync(this Aes aes, byte[] encrypted)
        {
            using (aes)
            {
                var decryptor = aes.CreateDecryptor();
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (var sr = new StreamReader(cs))
                            return await sr.ReadToEndAsync();
                    }
                }
            }
        }

        private static readonly HashAlgorithm hasher = SHA256.Create();
    }
}
