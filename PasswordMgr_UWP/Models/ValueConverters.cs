using PasswordMgr_UWP.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace PasswordMgr_UWP.Models
{
    public class TreeViewItemInvokedEventArgsToInvokedItemConverter : IValueConverter
    {
        public static object Convert(TreeViewItemInvokedEventArgs args)
            => args.InvokedItem;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TreeViewItemInvokedEventArgs args)
                return Convert(args);

            throw new ArgumentException("Input is not an object of the type TreeViewItemInvokedEventArgs.", nameof(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public class BoolToPasswordRevealModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolean)
            {
                if (boolean)
                    return PasswordRevealMode.Visible;

                return PasswordRevealMode.Hidden;
            }

            throw new ArgumentException("Input is not a bool.", nameof(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is PasswordRevealMode mode)
                return mode == PasswordRevealMode.Visible;

            throw new ArgumentException("Input is not an object of the type PasswordRevealMode.", nameof(value));
        }
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolean)
                return !boolean;

            throw new ArgumentException("Input is not a bool.", nameof(value));
        }


        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public class CheckForValueConverter : IValueConverter
    {
        /// <summary>
        /// Checks whether an object has a value.
        /// </summary>
        /// <param name="value">The object to check for a value.</param>
        /// <returns>If the object has a value, return true. Otherwise, return false.</returns>
        public object Convert(object value, Type targetType, object parameter, string language) => value != null;

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public class CheckForDecryptedDatabaseConverter : IValueConverter
    {
        /// <summary>
        /// Checks whether the object is a decrypted password database.
        /// </summary>
        /// <param name="value">The IPasswordInformation to check.</param>
        /// <returns>Returns "true" if the given object is a decrypted password database, false otherwise.</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is IPasswordInformation item)
                return item.IsDecrypted && item.PasswordType == PasswordType.Database;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public class ConversionGroup : List<IValueConverter>, IValueConverter
    {
        /// <summary>
        /// Cycles through all included converters and returns the final result.
        /// </summary>
        /// <returns>The result after all conversions.</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
            => this.Aggregate(value, (current, converter) => converter.Convert(current, targetType, parameter, language));

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
