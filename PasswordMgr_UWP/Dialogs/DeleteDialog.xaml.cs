using PasswordMgr_UWP.Core.Models;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace PasswordMgr_UWP.Dialogs
{
    public sealed partial class DeleteDialog : ContentDialog
    {
        public DeleteDialog(string _name, PasswordType _passwordType)
        {
            name = _name;
            passwordType = _passwordType;
            this.InitializeComponent();
        }

        private string name;
        private PasswordType passwordType;
        private ResourceLoader resLoader = new ResourceLoader("DialogResources");

        #region UI Resources
        private string DialogTitle => name + resLoader.GetString("deleteEnd");
        private string Text
        {
            get
            {
                string start = resLoader.GetString($"delete{passwordType}Start");
                string end = resLoader.GetString("deleteEnd");
                return start + name + end;
            }
        }
        #endregion
    }
}
