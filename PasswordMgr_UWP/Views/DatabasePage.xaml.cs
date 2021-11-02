using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PasswordMgr_UWP.Core.Models;
using PasswordMgr_UWP.ViewModels;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace PasswordMgr_UWP.Views
{
    public sealed partial class DatabasePage : Page
    {
        public DatabaseViewModel ViewModel { get; } = new DatabaseViewModel();

        public DatabasePage()
        {
            NavigationCacheMode = NavigationCacheMode.Required;
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e.Parameter as IActivatedEventArgs;
            if (args == null || args.Kind != ActivationKind.File) return;

            var fileArgs = args as FileActivatedEventArgs;
            var databases = new List<EncryptedDatabase>();
            foreach (var item in fileArgs.Files.Where(i => i != null && Path.GetExtension(i.Path) == ".opv"))
            {
                try
                {
                    string bson = await FileIO.ReadTextAsync((IStorageFile)item);
                    if (!string.IsNullOrEmpty(bson))
                        databases.Add(BsonSerialization.Deserialize<EncryptedDatabase>(bson));
                }
                catch (Exception ex) { Debug.WriteLine(ex.Message); }
            }
            if (!databases.Any()) return;

            ContentDialog dlg = databases.Count > 1
                ? new ContentDialog()
                {
                    Title = resLoader.GetString("importDatabases"),
                    Content = new TextBlock()
                    {
                        Text = resLoader.GetString("importDatabasesStart") + databases.Count + resLoader.GetString("importDatabasesEnd"),
                        TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap
                    },
                    PrimaryButtonText = resLoader.GetString("yes"),
                    SecondaryButtonText = resLoader.GetString("no")
                }
                : new ContentDialog()
                {
                    Title = resLoader.GetString("importDatabase"),
                    Content = new TextBlock()
                    {
                        Text = resLoader.GetString("importSingleDatabaseStart") + databases[0].Name + resLoader.GetString("importSingleDatabaseEnd"),
                        TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap
                    },
                    PrimaryButtonText = resLoader.GetString("yes"),
                    SecondaryButtonText = resLoader.GetString("no")
                };

            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            foreach (var database in databases)
                await ViewModel.AddExistingDatabase(database);
        }
        private static readonly ResourceLoader resLoader = new ResourceLoader("DialogResources");
    }
}
