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
        public DataTemplate DatabaseTemplate { get; set; }
        public DataTemplate PasswordTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
            => ((IPasswordInformation)item).PasswordType == PasswordType.Database ? DatabaseTemplate : PasswordTemplate;
    }
}
