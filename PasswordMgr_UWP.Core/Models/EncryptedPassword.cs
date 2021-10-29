using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using PasswordMgr_UWP.Core.Extensions;

namespace PasswordMgr_UWP.Core.Models
{
    public class EncryptedPassword : ObservableBase, IPasswordInformation
    {
        /// <summary>
        /// Initializes a new EncryptedPassword object.
        /// </summary>
        /// <param name="platform">The name of the platform.</param>
        /// <param name="username">The username/e-mail for the platform.</param>
        /// <param name="password">The encrypted password.</param>
        /// <param name="salt">The salt for the password.</param>
        /// <param name="iv">The InitializationVector used for encryption.</param>
        public EncryptedPassword(string platform, string username, byte[] password, string salt, byte[] iv)
        {
            Name = platform;
            Info = username;
            Password = password;
            Salt = salt;
            IV = iv;
        }

        /// <summary>
        /// Returns a new EncryptedPassword object.
        /// </summary>
        /// <param name="platform">The name of the platform.</param>
        /// <param name="username">The username/e-mail for the platform.</param>
        /// <param name="password">The password to encrypt.</param>
        /// <param name="key">The Masterpassword to encrypt the password.</param>
        /// <returns></returns>
        public static async Task<EncryptedPassword> CreateAsync(string platform, string username, string password, string key)
        {
            using (Aes aes = Aes.Create())
            {
                //A salt will be generated and added to the password.
                string salt = GenerationTools.GeneratePassword(new OptionSet(8));
                string saltedPassword = password + salt;

                byte[] encryptedPassword = await AESEncryptions.EncryptStringAsync(saltedPassword, key, aes.IV);
                return new EncryptedPassword(platform, username, encryptedPassword, salt, aes.IV);
            }
        }

        public void Encrypt()
        {
            IsDecrypted = false;
            PlaintextPassword = null;
        }

        public async Task Decrypt(string masterpassword)
        {
            string plaintextPassword = await AESEncryptions.DecryptStringAsync(Password, masterpassword, IV);
            if (!Salt.IsNullOrEmpty())
                PlaintextPassword = plaintextPassword.Replace(Salt, "");
            else
                PlaintextPassword = plaintextPassword;
            IsDecrypted = true;
        }

        public PasswordType PasswordType => PasswordType.Password;

        /// <summary>
        /// The name of the platform.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The username/e-mail for the platform.
        /// </summary>
        public string Info { get; set; }

        /// <summary>
        /// The encrypted password.
        /// </summary>
        public byte[] Password { get; set; }

        /// <summary>
        /// The InitializationVector used for encryption.
        /// </summary>
        public byte[] IV { get; set; }

        /// <summary>
        /// The salt for the password.
        /// </summary>
        public string Salt { get; set; }

        /// <summary>
        /// The Json path to the database from the password
        /// </summary>
        [JsonIgnore]
        public string JsonPath { get; set; }

        /// <summary>
        /// The real password. It is not saved in the .json and the value is assigned during decryption.
        /// </summary>
        [JsonIgnore]
        public string PlaintextPassword { get; set; }

        /// <summary>
        /// Defines the decryption status of the password
        /// </summary>
        [JsonIgnore]
        public bool IsDecrypted
        {
            get => isDecrypted;
            set
            {
                if (isDecrypted != value)
                {
                    if (PlaintextPassword.IsNullOrEmpty())
                        isDecrypted = false;
                    else
                        isDecrypted = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool isDecrypted;
    }
}
