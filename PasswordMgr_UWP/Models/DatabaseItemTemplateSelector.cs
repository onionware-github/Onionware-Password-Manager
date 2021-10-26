using PasswordMgr_UWP.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PasswordMgr_UWP.Models
{
    public class DatabaseItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FolderTemplate { get; set; }
        public DataTemplate FileTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            var passwordItem = (IPasswordInformation)item;
            return passwordItem.PasswordType == PasswordType.Database ? FolderTemplate : FileTemplate;
        }
    }
}
