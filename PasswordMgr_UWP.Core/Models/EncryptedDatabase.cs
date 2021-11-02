using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Threading.Tasks;
using PasswordMgr_UWP.Core.Extensions;

namespace PasswordMgr_UWP.Core.Models
{
    /// <summary>
    /// A list of encrypted passwords.
    /// </summary>
    public class EncryptedDatabase : ObservableBase, IPasswordInformation
    {
        /// <summary>
        /// Initializes a new EncryptedDatabase object.
        /// </summary>
        /// <param name="name">The name of the database.</param>
        /// <param name="password">The masterpassword for the database.</param>
        /// <param name="iv">The InitializationVector for the database.</param>
        /// <param name="salt">The salt for the password of the database.</param>
        /// <param name="description">The description for the database (optional).</param>
        public EncryptedDatabase(string name, byte[] password, byte[] iv, string salt, string description)
        {
            Name = name;
            Password = password;
            IV = iv;
            Info = description;
            Created = DateTime.Now;
            Salt = salt;
            Passwords = new ObservableCollection<EncryptedPassword>();
        }

        /// <summary>
        /// Returns a new object of EncryptedDatabase.
        /// </summary>
        /// <param name="name">The name of the database.</param>
        /// <param name="password">The plaintext password for the database.</param>
        /// <param name="description">The description for the database (optional).</param>
        /// <returns>The encrypted database.</returns>
        public static async Task<EncryptedDatabase> CreateAsync(string name, string password, string description = "")
        {
            using (Aes aes = Aes.Create())
            {
                //A salt will be generated and added to the password.
                string salt = GenerationTools.GeneratePassword(new OptionSet(8));
                string saltedPassword = password + salt;

                var encryptedPassword = await EncryptedPassword.CreateAsync(name, name, saltedPassword, saltedPassword);
                return new EncryptedDatabase(name, encryptedPassword.Password, encryptedPassword.IV, salt, description);
            }
        }

        /// <summary>
        /// Removes the Plaintextpasswords and sets IsDecrypted to false.
        /// </summary>
        public void Encrypt()
        {
            foreach (var password in Passwords)
                password.Encrypt();

            IsDecrypted = false;
            PlaintextPassword = null;
        }

        /// <summary>
        /// Decrypts the database.
        /// </summary>
        /// <param name="password">The masterpassword for the database.</param>
        public async Task Decrypt(string password)
        {
            //Return if the database is already decrypted
            if (IsDecrypted)
                return;

            //Throws an exception when the masterpassword is false.
            await AESEncryptions.DecryptStringAsync(Password, password + Salt, IV);

            //Decrypt all contained items with the given masterpassword
            foreach (var p in Passwords)
                await p.Decrypt(password);

            PlaintextPassword = password;
            IsDecrypted = true;
#if DEBUG
            int wait = 30000;
#else
            int wait = 300000;
#endif
            await Task.Delay(wait);
            Encrypt();
        }

        public PasswordType PasswordType => PasswordType.Database;

        /// <summary>
        /// Name of the database.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Masterpassword for the database.
        /// </summary>
        public byte[] Password { get; set; }

        /// <summary>
        /// InitializationVector for the database.
        /// </summary>
        public byte[] IV { get; set; }

        /// <summary>
        /// The salt for the database.
        /// </summary>
        public string Salt { get; set; }

        /// <summary>
        /// Description for the database.
        /// </summary>
        public string Info { get; set; }

        /// <summary>
        /// The DateTime of creation from the database.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// The encrypted passwords.
        /// </summary>
        public ObservableCollection<EncryptedPassword> Passwords { get; set; }

        /// <summary>
        /// The path to the saved .json file.
        /// </summary>
        [JsonIgnore]
        public string JsonPath { get; set; }

        /// <summary>
        /// The Masterpassword for the database. It is not saved in the .json and the value is assigned during decryption.
        /// </summary>
        [JsonIgnore]
        public string PlaintextPassword { get; set; }

        /// <summary>
        /// Defines the decryption status of the database
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
                    {
                        if (isDecrypted)
                        {
                            isDecrypted = false;
                            UIPropertyChanged();
                        }
                    }
                    else
                    {
                        isDecrypted = value;
                        UIPropertyChanged();
                    }
                    Debug.WriteLine("IsDecrypted property set to " + isDecrypted);
                }
            }
        }
        private bool isDecrypted;

        [JsonIgnore]
        public bool IsDecryptButtonEnabled => !IsDecrypted;

        public event EventHandler UIPropertyChangedEventHandler;
        protected void UIPropertyChanged([CallerMemberName] string propertyName = "")
        {
            OnPropertyChanged(propertyName);
            UIPropertyChangedEventHandler?.Invoke(this, new EventArgs());
        }
    }
}
