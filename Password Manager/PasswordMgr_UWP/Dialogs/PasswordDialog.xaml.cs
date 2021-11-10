using PasswordMgr_UWP.ViewModels;
using Windows.UI.Xaml.Controls;

namespace PasswordMgr_UWP.Dialogs
{
    public sealed partial class PasswordDialog : ContentDialog
    {
        public PasswordDialog()
        {
            Data = new DataDialogViewModel();
            PrivateData = new DataDialogPrivateViewModel(Data);
            this.InitializeComponent();
        }

        public DataDialogViewModel Data { get; set; }
        private DataDialogPrivateViewModel PrivateData { get; set; }
    }
}
