using System;
using PasswordMgr_UWP.Core.Models;
using PasswordMgr_UWP.ViewModels;

using Windows.UI.Xaml.Controls;

namespace PasswordMgr_UWP.Views
{
    public sealed partial class DatabasePage : Page
    {
        public DatabaseViewModel ViewModel { get; } = new DatabaseViewModel();

        public DatabasePage()
        {
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;
            InitializeComponent();
        }
    }
}
