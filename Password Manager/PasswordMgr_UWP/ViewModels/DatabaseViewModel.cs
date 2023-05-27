using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using PasswordMgr_UWP.Core.Models;
using PasswordMgr_UWP.Core.Extensions;
using PasswordMgr_UWP.Dialogs;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PasswordMgr_UWP.ViewModels
{
    public class DatabaseViewModel : ObservableObject
    {
        public DatabaseViewModel()
        {
            Databases = new ObservableCollection<EncryptedDatabase>();
            Databases.CollectionChanged += DatabaseCollectionChanged;
            NewDatabaseCommand = new AsyncRelayCommand(() => ShowDatabaseDialogAsync());
            NewPasswordCommand = new AsyncRelayCommand(() => ShowPasswordDialogAsync());
            DecryptDatabaseCommand = new AsyncRelayCommand(() => ShowMasterPasswordDialog());
            EncryptButtonCommand = new AsyncRelayCommand(() => EncryptDatabase());
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
            SaveCommand = new AsyncRelayCommand(async () => InfosChangedBarVisibility = await UpdateInformations());
            ExportDatabaseCommand = new AsyncRelayCommand(() => ExportDatabase((EncryptedDatabase)Selected));
            ExportAllDatabasesCommand = new AsyncRelayCommand(() => ExportAllDatabases());
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

            LoadData();
        }

        private void DatabaseCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(ListIsNotEmpty));

        #region Properties
        //Commands for UI Elements
        public AsyncRelayCommand NewDatabaseCommand { get; }
        public AsyncRelayCommand NewPasswordCommand { get; }
        public AsyncRelayCommand DecryptDatabaseCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand EncryptButtonCommand { get; }
        public AsyncRelayCommand ExportDatabaseCommand { get; }
        public AsyncRelayCommand ExportAllDatabasesCommand { get; }
        public RelayCommand<object> SetSelectedCommand { get; }
        public RelayCommand<string> CopyCommand { get; }

        /// <summary>
        /// An ObservableCollection that contains the password databases.
        /// </summary>
        public ObservableCollection<EncryptedDatabase> Databases { get; set; }
        public bool ListIsNotEmpty => Databases.Any();

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
                    Name = string.Empty;
                    Info = string.Empty;
                    PlaintextPassword = string.Empty;
                }

                OnPropertyChanged(nameof(EditChecked));
                OnPropertyChanged(nameof(SelectedIsDatabase));
                InfosChangedBarVisibility = false;
            }
        }
        private IPasswordInformation selected;
        public bool SelectedIsDatabase => Selected != null && Selected.PasswordType == PasswordType.Database;

        /// <summary>
        /// The name of the selected item.
        /// </summary>
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }
        private string name;

        /// <summary>
        /// Infos about the selected item.
        /// </summary>
        public string Info
        {
            get => info;
            set => SetProperty(ref info, value);
        }
        private string info;

        /// <summary>
        /// The PlaintextPassword of the selected item.
        /// </summary>
        public string PlaintextPassword
        {
            get => plaintextPassword;
            set => SetProperty(ref plaintextPassword, value);
        }
        private string plaintextPassword;

        /// <summary>
        /// Updates the informations about the selected item.
        /// </summary>
        /// <returns>"true" if informations are changed, "false" otherwise.</returns>
        public async Task<bool> UpdateInformations()
        {
            if ((Name, Info, PlaintextPassword) == (Selected.Name, Selected.Info, Selected.PlaintextPassword))
                return false;

            if (PlaintextPassword.Length < 8)
            {
                var dlg = new ContentDialog()
                {
                    Title = dlgResloader.GetString("passwordTooShort"),
                    Content = new TextBlock()
                    {
                        Text = dlgResloader.GetString("passwordTooShortContent"),
                        TextWrapping = TextWrapping.Wrap
                    },
                    PrimaryButtonText = "OK"
                };
                await dlg.ShowAsync();
                return false;
            }

            (Selected.Name, Selected.Info) = (Name, Info);

            if (Selected.PlaintextPassword != PlaintextPassword)
            {
                if (Selected.PasswordType == PasswordType.Password)
                {
                    var database = GetDatabaseFromPassword((EncryptedPassword)Selected);
                    var newPassword = await EncryptedPassword.CreateAsync(Selected.Name, Selected.Info, PlaintextPassword, database.PlaintextPassword);

                    (Selected.Password, Selected.IV, Selected.Salt) = (newPassword.Password, newPassword.IV, newPassword.Salt);

                    //Decrypt the new password.
                    await Selected.DecryptAsync(database.PlaintextPassword);
                    await UpdateBson(database);
                    return true;
                }
                else
                {
                    foreach (var password in ((EncryptedDatabase)Selected).Passwords)
                    {
                        var newPassword = await EncryptedPassword.CreateAsync(password.Name, password.Info, password.PlaintextPassword, PlaintextPassword);
                        password.Password = newPassword.Password;
                        password.IV = newPassword.IV;
                        password.Salt = newPassword.Salt;
                    }

                    var newDB = await EncryptedDatabase.CreateAsync(Name, PlaintextPassword);
                    Selected.Password = newDB.Password;
                    Selected.IV = newDB.IV;
                    Selected.Salt = newDB.Salt;
                    Selected.PlaintextPassword = PlaintextPassword;

                    await UpdateBson((EncryptedDatabase)Selected);
                }
            }

            if (Selected.PasswordType == PasswordType.Database)
            {
                await UpdateBson((EncryptedDatabase)Selected);
            }
            else
            {
                var database = GetDatabaseFromPassword((EncryptedPassword)Selected);
                await UpdateBson(database);
            }
            return true;
        }

        /// <summary>
        /// Show the InfosChangedInfoBar.
        /// </summary>
        public bool InfosChangedBarVisibility
        {
            get => infosChangedBarVisibility;
            set => SetProperty(ref infosChangedBarVisibility, value);
        }
        private bool infosChangedBarVisibility;

        public bool EditChecked => false;


        /// <summary>
        /// Checks whether the object has any value.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        public bool HasValue(object obj) => obj != null;
        #endregion


        /// <summary>
        /// Deserializes the EncryptedDatabases (.bson) and add them to the database collection.
        /// </summary>
        private void LoadData()
        {
            Databases.Clear();
            string bsonPath = installPath + @"\Databases";
            if (!Directory.Exists(bsonPath))
            {
                Directory.CreateDirectory(bsonPath);
                Debug.WriteLine(bsonPath + " created!");
                return;
            }

            //Check for legacy .json data
            if (Directory.GetFiles(bsonPath).Any(file => file.EndsWith(".json")))
            {
                string legacyPath = tempPath + @"\LegacyContent\";
                if (!Directory.Exists(legacyPath))
                    Directory.CreateDirectory(legacyPath);

                Parallel.ForEach(Directory.GetFiles(bsonPath).Where(f => f.EndsWith(".json")), file =>
                {
                    string filepath = Path.ChangeExtension(file, ".opv");
                    try
                    {
                        var reader = File.OpenText(file);
                        var writer = File.CreateText(filepath);

                        string bson = BsonSerialization.JsonToBson<EncryptedDatabase>(reader.ReadToEnd());
                        writer.Write(bson);

                        reader.Close();
                        writer.Close();
                        File.Move(file, legacyPath + $"{Path.GetRandomFileName()}.json");
                    }
                    catch (Exception e) { Debug.WriteLine(e.Message); }
                });
            }

            foreach (var file in Directory.GetFiles(bsonPath).Where(f => f.EndsWith(".opv")))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        Debug.WriteLine("Reading file at: " + file);
                        var encryptedDatabase = BsonSerialization.Deserialize<EncryptedDatabase>(sr.ReadToEnd());
                        if (encryptedDatabase == null) continue;
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
        /// Creates a new EncryptedDatabase and generates a .bson file.
        /// </summary>
        /// <param name="name">The name for the database.</param>
        /// <param name="password">The masterpassword for the database.</param>
        /// <param name="description">The description for the database.</param>
        public async Task AddNewDatabase(string name, string password, string description = "")
        {
            var encryptedDatabase = await EncryptedDatabase.CreateAsync(name, password, description);
            await GenerateBson(encryptedDatabase);
            await Databases.Last().DecryptAsync(password);
        }

        /// <summary>
        /// Adds an existing database and generates a .bson file for it.
        /// </summary>
        /// <param name="encryptedDatabase">The database to add.</param>
        public async Task AddExistingDatabase(EncryptedDatabase encryptedDatabase)
            => await GenerateBson(encryptedDatabase);

        /// <summary>
        /// Export a password database as an .opv file.
        /// </summary>
        /// <param name="encryptedDatabase">The database to export.</param>
        public async Task ExportDatabase(EncryptedDatabase encryptedDatabase)
        {
            if (encryptedDatabase == null)
                throw new ArgumentNullException(nameof(encryptedDatabase));

            if (!File.Exists(encryptedDatabase.JsonPath))
                throw new FileNotFoundException("The file has not been found.", encryptedDatabase.JsonPath);

            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add(resLoader.GetString("OpvName"), new List<string>() { ".opv" });
            savePicker.SuggestedFileName = encryptedDatabase.Name;
            var file = await savePicker.PickSaveFileAsync();
            if (file == null) return;

            CachedFileManager.DeferUpdates(file);
            using (var reader = File.OpenText(encryptedDatabase.JsonPath))
            {
                await FileIO.WriteTextAsync(file, await reader.ReadToEndAsync());
                reader.Close();
            }
            await CachedFileManager.CompleteUpdatesAsync(file);
        }

        /// <summary>
        /// Exports all password database.
        /// </summary>
        public async Task ExportAllDatabases()
        {
            var directoryInfo = Directory.CreateDirectory(tempPath + $@"\{Path.GetRandomFileName()}");
            string zipPath = directoryInfo.ToString() + @"\Databases.zip";
            ZipFile.CreateFromDirectory(dataPath, zipPath);

            var zipFile = await StorageFile.GetFileFromPathAsync(zipPath);
            var savePicker = new FileSavePicker();
            savePicker.SuggestedFileName = resLoader.GetString("databases");
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add(resLoader.GetString("zipArchive"), new List<string>() { ".zip" });
            var file = await savePicker.PickSaveFileAsync();
            if (file == null) return;

            CachedFileManager.DeferUpdates(file);
            await zipFile.MoveAndReplaceAsync(file);
            await CachedFileManager.CompleteUpdatesAsync(file);
        }

        /// <summary>
        /// Adds a password to the selected database and serializes the database file.
        /// </summary>
        /// <param name="platform">The platform for the account.</param>
        /// <param name="username">The username for the platform.</param>
        /// <param name="password">The password.</param>
        public async Task AddNewPassword(string platform, string username, string password)
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
            await UpdateBson((EncryptedDatabase)Selected);
        }

        /// <summary>
        /// Removes an encrypted database and deletes its .bson file.
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

            var database = GetDatabaseFromPassword(encryptedPassword);
            database.Passwords.Remove(encryptedPassword);
            await UpdateBson(database);
        }

        /// <summary>
        /// Searches the EncryptedDatabase with the same JsonPath as the EncryptedPassword.
        /// </summary>
        /// <returns>The EncryptedDatabase objet with the same JsonPath.</returns>
        private EncryptedDatabase GetDatabaseFromPassword(EncryptedPassword encryptedPassword)
        {
            if (encryptedPassword == null)
                throw new ArgumentNullException(nameof(encryptedPassword));

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
                    throw new ArgumentException("Database could not be found.", nameof(encryptedPassword));
            }
            return database;
        }

        /// <summary>
        /// Serializes an encrypted database and adds it to the Database collection.
        /// </summary>
        /// <param name="encryptedDatabase">The database to generate .bson for.</param>
        private async Task GenerateBson(EncryptedDatabase encryptedDatabase)
        {
            string newPath = installPath + $@"\Databases\{encryptedDatabase.Name.TrimToFilename()}.opv";

            // When a file with the same name already exists, create a new filename
            for (int i = 2; File.Exists(newPath); i++)
                newPath = installPath + $@"\Databases\{encryptedDatabase.Name.TrimToFilename()}_{i}.opv";

            //Serialize the database to a .bson file
            using (StreamWriter sw = File.CreateText(newPath))
                await sw.WriteAsync(BsonSerialization.Serialize(encryptedDatabase));

            encryptedDatabase.JsonPath = newPath;
            encryptedDatabase.UIPropertyChangedEventHandler += ModelPropertyChanged;
            Databases.Add(encryptedDatabase);
        }


        /// <summary>
        /// Updates the .bson file for an existing database.
        /// </summary>
        /// <param name="encryptedDatabase">The database to update.</param>
        private async Task UpdateBson(EncryptedDatabase encryptedDatabase)
        {
            if (!File.Exists(encryptedDatabase.JsonPath))
                await GenerateBson(encryptedDatabase);

            using (StreamWriter sw = new StreamWriter(encryptedDatabase.JsonPath))
                await sw.WriteAsync(BsonSerialization.Serialize(encryptedDatabase));
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
                    await ((EncryptedDatabase)Selected).Passwords.Last().DecryptAsync(Selected.PlaintextPassword);
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

        public async Task EncryptDatabase()
        {
            await ((EncryptedDatabase) selected).EncryptAsync();
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
                await ((EncryptedDatabase)Selected).DecryptAsync(dlg.Masterpassword);
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

        //Some private objects from this class...
        private static readonly string installPath = ApplicationData.Current.LocalFolder.Path;
        private static readonly string dataPath = installPath + @"\Databases";
        private static readonly string tempPath = ApplicationData.Current.TemporaryFolder.Path;
        private static readonly ResourceLoader resLoader = new ResourceLoader();
        private static readonly ResourceLoader dlgResloader = new ResourceLoader("DialogResources");

        protected virtual void ModelPropertyChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Selected));
            OnPropertyChanged(nameof(Databases));
            if (Selected == null)
                return;

            if (Selected.IsDecrypted)
            {
                Name = Selected.Name;
                Info = Selected.Info;
                PlaintextPassword = Selected.PlaintextPassword;
            }
            else
            {
                Name = string.Empty;
                Info = string.Empty;
                PlaintextPassword = string.Empty;
            }
        }
    }
}
