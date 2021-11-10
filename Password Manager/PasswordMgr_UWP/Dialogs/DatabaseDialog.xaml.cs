using PasswordMgr_UWP.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace PasswordMgr_UWP.Dialogs
{
    public sealed partial class DatabaseDialog : ContentDialog
    {
        public DatabaseDialog()
        {
            Data = new DataDialogViewModel();
            PrivateData = new DataDialogPrivateViewModel(Data);
            this.InitializeComponent();
        }

        public DataDialogViewModel Data { get; set; }
        private DataDialogPrivateViewModel PrivateData { get; set; }
    }
}
