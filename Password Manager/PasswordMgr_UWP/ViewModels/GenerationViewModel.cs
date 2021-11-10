using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using PasswordMgr_UWP.Core.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;

namespace PasswordMgr_UWP.ViewModels
{
    public class GenerationViewModel : ObservableObject
    {
        public GenerationViewModel()
        {
            Output = new ObservableCollection<string>();
            Output.CollectionChanged += (o, e) => OnPropertyChanged(nameof(OutputHasElements));

            PasswordOptions = new OptionSet(16);
            Lenght = PasswordOptions.Lenght;

            GenerateCommand = new AsyncRelayCommand<int>(amount => Generate(amount));
            CopyCommand = new RelayCommand<string>(content =>
            {
                var data = new DataPackage();
                data.SetText(content);
                Clipboard.SetContent(data);
            }, content => !string.IsNullOrEmpty(content));
            ClearCommand = new RelayCommand(() => Output.Clear());
        }

        public AsyncRelayCommand<int> GenerateCommand { get; set; }
        public RelayCommand<string> CopyCommand { get; set; }
        public RelayCommand ClearCommand { get; set; }

        private OptionSet passwordOptions;
        public OptionSet PasswordOptions
        {
            get => passwordOptions;
            set => SetProperty(ref passwordOptions, value);
        }

        public ObservableCollection<string> Output { get; set; }
        public bool OutputHasElements => Output.Any();

        public async Task Generate(int amount)
        {
            var passwords = await GenerationTools.GeneratePasswordsAsync(amount, PasswordOptions);
            Output = new ObservableCollection<string>(passwords);
            OnPropertyChanged(nameof(Output));
            OnPropertyChanged(nameof(OutputHasElements));
        }

        private int lenght;
        public int Lenght
        {
            get => lenght;
            set
            {
                if (lenght != value)
                {
                    lenght = value;
                    PasswordOptions.Lenght = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PasswordStrength));
                }
            }
        }

        private static ResourceLoader strengthResources = ResourceLoader.GetForCurrentView("EnumResources");
        public string PasswordStrength => strengthResources.GetString(PasswordOptions.PasswordStrength.ToString());

        private string result;
        public string Result
        {
            get => result;
            set => SetProperty(ref result, value);
        }
    }
}
