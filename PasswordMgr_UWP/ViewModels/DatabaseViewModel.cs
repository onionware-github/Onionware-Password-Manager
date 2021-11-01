using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using PasswordMgr_UWP.Core.Models;
using PasswordMgr_UWP.Core.Extensions;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;
using PasswordMgr_UWP.Dialogs;
using System.Security.Cryptography;
using Windows.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;

namespace PasswordMgr_UWP.ViewModels
{
    public class DatabaseViewModel : ObservableObject
    {
        public DatabaseViewModel()
        {
            Databases = new ObservableCollection<EncryptedDatabase>();
            NewDatabaseCommand = new AsyncRelayCommand(() => ShowDatabaseDialogAsync());
            NewPasswordCommand = new AsyncRelayCommand(() => ShowPasswordDialogAsync());
            DecryptDatabaseCommand = new AsyncRelayCommand(() => ShowMasterPasswordDialog());
            DeleteCommand = new AsyncRelayCommand(async () =>
            {
                //Shows a ContentDialog to prevent an unintended delete
                if (Selected == null || !await ShowDeleteDialogAsync(Selected.Name, Selected.PasswordType))
                    return;

                if (Selected.PasswordType == PasswordType.Database)
                    DeleteDatabase((EncryptedDatabase)Selected);

                else
                    await DeletePassword((EncryptedPassword)Selected);

                Selected = null;
            });
            SetSelectedCommand = new RelayCommand<object>(item =>
            {
                switch (((IPasswordInformation)item).PasswordType)
                {
                    case PasswordType.Database:
                        Selected = (EncryptedDatabase)item;
                        Debug.WriteLine("Selected is " + Selected.PasswordType);
                        break;

                    case PasswordType.Password:
                        Selected = (EncryptedPassword)item;
                        Debug.WriteLine("Selected is " + Selected.PasswordType);
                        break;

                    default:
                        Selected = null;
                        Debug.WriteLine("Selected is null");
                        break;
                }
            });
            CopyCommand = new RelayCommand<string>(content =>
            {
                var data = new DataPackage();
                data.SetText(content);
                Clipboard.SetContent(data);
            }, content => !string.IsNullOrEmpty(content));

            LoadJsonDataAsync();
        }

        #region Properties
        //Commands for UI Elements
        public AsyncRelayCommand NewDatabaseCommand { get; }
        public AsyncRelayCommand NewPasswordCommand { get; }
        public AsyncRelayCommand DecryptDatabaseCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public RelayCommand<object> SetSelectedCommand { get; }
        public RelayCommand<string> CopyCommand { get; }
        public RelayCommand SaveCommand { get; }

        /// <summary>
        /// An ObservableCollection that contains the password databases.
        /// </summary>
        public ObservableCollection<EncryptedDatabase> Databases { get; set; }

        /// <summary>
        /// The selected item from the TreeView.
        /// </summary>
        public IPasswordInformation Selected
        {
            get => selected;
            set
            {
                if (!SetProperty(ref selected, value))
                    return;

                if (value != null)
                {
                    Name = value.Name;
                    Info = value.Info;
                    PlaintextPassword = value.PlaintextPassword;
                }
                else
                {
                    Name = null;
                    Info = null;
                    PlaintextPassword = null;
                }
                OnPropertyChanged(nameof(EditChecked));
            }
        }
        private IPasswordInformation selected;

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }
        private string name;

        public string Info
        {
            get => info;
            set => SetProperty(ref info, value);
        }
        private string info;

        public string PlaintextPassword
        {
            get => plaintextPassword;
            set => SetProperty(ref plaintextPassword, value);
        }
        private string plaintextPassword;

        public bool EditChecked => false;

        /// <summary>
        /// Checks whether the object has any value.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        public bool HasValue(object obj) => obj != null;
        #endregion

        /// <summary>
        /// Deserializes the EncryptedDatabases and add them to the database collection.
        /// </summary>
        private void LoadJsonDataAsync()
        {
            string jsonPath = InstallPath + @"\Databases";
            if (!Directory.Exists(jsonPath))
            {
                Directory.CreateDirectory(jsonPath);
                Debug.WriteLine(jsonPath + " created!");
                return;
            }

            foreach (var file in Directory.GetFiles(jsonPath))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        Debug.WriteLine("Reading file at: " + file);
                        var encryptedDatabase = (EncryptedDatabase)jsonser.Deserialize(sr, typeof(EncryptedDatabase));
                        encryptedDatabase.JsonPath = file;
                        foreach (var password in encryptedDatabase.Passwords)
                            password.JsonPath = file;

                        encryptedDatabase.UIPropertyChangedEventHandler += ModelPropertyChanged;
                        Databases.Add(encryptedDatabase);
                    }
                }
                catch (Exception e) { Debug.WriteLine(e.Message); }
            }
        }

        /// <summary>
        /// Creates a new EncryptedDatabase and generates a .json file.
        /// </summary>
        /// <param name="name">The name for the database.</param>
        /// <param name="password">The masterpassword for the database.</param>
        /// <param name="description">The description for the database.</param>
        private async Task AddNewDatabase(string name, string password, string description = "")
        {
            var encryptedDatabase = await EncryptedDatabase.CreateAsync(name, password, description);
            await GenerateJson(encryptedDatabase);
        }

        /// <summary>
        /// Adds a password to the selected database and serializes the database file.
        /// </summary>
        /// <param name="platform">The platform for the account.</param>
        /// <param name="username">The username for the platform.</param>
        /// <param name="password">The password.</param>
        private async Task AddNewPassword(string platform, string username, string password)
        {
            if (Selected == null)
                throw new NullValueException("The selected item is null.");

            if (Selected.PlaintextPassword.IsNullOrEmpty())
                throw new DatabaseEncryptedException("The PlaintextPassword from the selected database is invalid.");

            if (Selected.PasswordType == PasswordType.Password)
                throw new PasswordTypeException("The selected IPasswordInformation is an EncryptedPassword. It has to be a Database.");

            var encryptedPassword = await EncryptedPassword.CreateAsync(platform, username, password, Selected.PlaintextPassword);

            ((EncryptedDatabase)Selected).Passwords.Add(encryptedPassword);
            OnPropertyChanged(nameof(Selected));
            await UpdateJson((EncryptedDatabase)Selected);
        }

        /// <summary>
        /// Removes an encrypted database and deletes its .json file.
        /// </summary>
        /// <param name="encryptedDatabase">The database to delete.</param>
        public void DeleteDatabase(EncryptedDatabase encryptedDatabase)
        {
            if (encryptedDatabase == null)
                return;

            File.Delete(encryptedDatabase.JsonPath);
            Databases.Remove(encryptedDatabase);
        }

        /// <summary>
        /// Removes an encrypted password from the database.
        /// </summary>
        /// <param name="encryptedPassword">The EncryptedPassword to remove.</param>
        public async Task DeletePassword(EncryptedPassword encryptedPassword)
        {
            if (encryptedPassword == null)
                return;

            EncryptedDatabase database = null;
            try
            {
                //Deletes the password from the database with the same JsonPath.
                database = Databases.First(item => item.JsonPath == encryptedPassword.JsonPath);
            }
            catch (InvalidOperationException)
            {
                foreach (var db in Databases)
                {
                    if (db.Passwords.Contains(encryptedPassword))
                        database = db;
                }
            }
            finally
            {
                if (database == null)
                    throw new ArgumentException("Password could not be removed.", nameof(encryptedPassword));

                database.Passwords.Remove(encryptedPassword);
                await UpdateJson(database);
            }
        }

        /// <summary>
        /// Serializes an encrypted database and adds it to the Database collection.
        /// </summary>
        /// <param name="encryptedDatabase">The database to generate .json for.</param>
        private async Task GenerateJson(EncryptedDatabase encryptedDatabase)
        {
            string newPath = InstallPath + $@"\Databases\{encryptedDatabase.Name.TrimToFilename()}.json";

            // When a file with the same name already exists, create a new filename
            for (int i = 2; File.Exists(newPath); i++)
                newPath = InstallPath + $@"\Databases\{encryptedDatabase.Name.TrimToFilename()}_{i}.json";

            //Serialize the database to a .json file
            using (FileStream fs = File.Create(newPath))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                    jsonser.Serialize(sw, encryptedDatabase);
            }

            encryptedDatabase.JsonPath = newPath;
            encryptedDatabase.UIPropertyChangedEventHandler += ModelPropertyChanged;
            Databases.Add(encryptedDatabase);
            await Task.Yield();
        }

        private void ModelPropertyChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Selected));
            if (Selected == null)
                return;

            Name = Selected.Name;
            Info = Selected.Info;
            PlaintextPassword = Selected.PlaintextPassword;
        }

        /// <summary>
        /// Updates the .json file for an existing database.
        /// </summary>
        /// <param name="encryptedDatabase">The database to update.</param>
        private async Task UpdateJson(EncryptedDatabase encryptedDatabase)
        {
            if (!File.Exists(encryptedDatabase.JsonPath))
                await GenerateJson(encryptedDatabase);

            using (StreamWriter sw = new StreamWriter(encryptedDatabase.JsonPath))
                jsonser.Serialize(sw, encryptedDatabase);
        }

        /// <summary>
        /// Shows a DatabaseDialog and creates a new EncryptedDatabase with the given informations.
        /// </summary>
        public async Task ShowDatabaseDialogAsync()
        {
            var dlg = new DatabaseDialog();

            if (await dlg.ShowAsync() != ContentDialogResult.Primary)
                return;

            await AddNewDatabase(dlg.Data.Name, dlg.Data.Password, dlg.Data.Info);
        }

        /// <summary>
        /// Shows a PasswordDialog and creates a new EncryptedDatabase with the given informations.
        /// </summary>
        public async Task ShowPasswordDialogAsync()
        {
            var dlg = new PasswordDialog();

            if (await dlg.ShowAsync() != ContentDialogResult.Primary)
                return;

            try
            {
                await AddNewPassword(dlg.Data.Name, dlg.Data.Info, dlg.Data.Password);

                //If the database is decrypted in the moment, also decrypt the new password
                if (Selected.IsDecrypted || !Selected.PlaintextPassword.IsNullOrEmpty())
                {
                    await ((EncryptedDatabase)Selected).Passwords.Last().Decrypt(Selected.PlaintextPassword);
                }
            }
            catch (NullValueException e) { Debug.WriteLine(e.Message); }
            catch (DatabaseEncryptedException)
            {
                Debug.WriteLine("The database is encrypted.");
                var errorDlg = new ContentDialog
                {
                    Title = dlgResloader.GetString("encryptedDatabaseDlgTitle"),
                    PrimaryButtonText = "OK",
                    Content = new TextBlock() { Text = dlgResloader.GetString("encryptedDatabaseDlgText"), TextWrapping = TextWrapping.Wrap }
                };
                await errorDlg.ShowAsync();
            }
        }

        /// <summary>
        /// Shows a MasterPasswordDialog and tries to decrypt with the given informations.
        /// </summary>
        public async Task ShowMasterPasswordDialog()
        {
            if (Selected == null || Selected.PasswordType != PasswordType.Database || Selected.IsDecrypted)
                return;

            var dlg = new MasterPasswordDialog();

            if (await dlg.ShowAsync() != ContentDialogResult.Primary)
                return;

            try
            {
                await ((EncryptedDatabase)Selected).Decrypt(dlg.Masterpassword);
            }
            catch (CryptographicException)
            {
                Debug.WriteLine("Failed to decrypt the database.");
                var errorDlg = new ContentDialog
                {
                    Title = dlgResloader.GetString("wrongPasswordDlgTitle"),
                    PrimaryButtonText = "OK",
                    Content = new TextBlock() { Text = dlgResloader.GetString("wrongPasswordDlgText"), TextWrapping = TextWrapping.Wrap }
                };
                await errorDlg.ShowAsync();
            }
        }

        /// <summary>
        /// Shows a DeleteDialog and returns its result.
        /// </summary>
        /// <param name="name">The name of the object to delete.</param>
        /// <param name="passwordType">The PasswordType of the object to delete.</param>
        /// <returns>true when clicked "Delete", otherwise false.</returns>
        public async Task<bool> ShowDeleteDialogAsync(string name, PasswordType passwordType)
            => await new DeleteDialog(name, passwordType).ShowAsync() == ContentDialogResult.Primary;

        //Some private objects in this class...
        private static readonly string InstallPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        private static readonly JsonSerializer jsonser = new JsonSerializer();
        private static readonly ResourceLoader dlgResloader = new ResourceLoader("DialogResources");
    }
}
