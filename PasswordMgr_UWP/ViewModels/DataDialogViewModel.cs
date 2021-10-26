using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace PasswordMgr_UWP.ViewModels
{
    public class DataDialogViewModel : ObservableRecipient
    {
        /// <summary>
        /// The name for the database or platform.
        /// </summary>
        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertiesChange();
                }
            }
        }
        private string name = string.Empty;
        
        /// <summary>
        /// The password for the database or platform.
        /// </summary>
        public string Password
        {
            get => password;
            set
            {
                if (password != value)
                {
                    password = value;
                    OnPropertiesChange();
                }
            }
        }
        private string password = string.Empty;

        /// <summary>
        /// The accountname or the description for the object to create.
        /// </summary>
        public string Info { get; set; }


        /// <summary>
        /// Occurs, when properties are changed.
        /// </summary>
        public event EventHandler OnPropertiesChanged;

        /// <summary>
        /// Raises the OnPropertiesChanged event.
        /// </summary>
        private void OnPropertiesChange()
            => OnPropertiesChanged?.Invoke(this, new EventArgs());
    }

    public class DataDialogPrivateViewModel : ObservableObject
    {
        public DataDialogPrivateViewModel(DataDialogViewModel _data)
        {
            if (_data == null)
                throw new ArgumentNullException(nameof(_data));

            data = _data;
            data.OnPropertiesChanged += OnPropertiesChanged;
        }
        private DataDialogViewModel data;


        public string PasswordRepeat
        {
            get => passwordRepeat;
            set
            {
                if (passwordRepeat != value)
                {
                    passwordRepeat = value;
                    OnPropertiesChanged(this, null);
                }
            }
        }
        private string passwordRepeat;

        public bool PasswordTooShort => data.Password.Length < 8;
        public bool NotTheSamePasswords => data.Password != PasswordRepeat;
        public bool ValidData => !PasswordTooShort && !NotTheSamePasswords && !string.IsNullOrEmpty(data.Name);

        private void OnPropertiesChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(PasswordTooShort));
            OnPropertyChanged(nameof(NotTheSamePasswords));
            OnPropertyChanged(nameof(ValidData));
        }
    }
}
