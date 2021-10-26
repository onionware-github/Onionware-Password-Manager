using System;

using PasswordMgr_UWP.ViewModels;

using Windows.UI.Xaml.Controls;

namespace PasswordMgr_UWP.Views
{
    public sealed partial class GenerationPage : Page
    {
        public GenerationViewModel ViewModel { get; } = new GenerationViewModel();

        public GenerationPage()
        {
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            InitializeComponent();
        }
    }
}
