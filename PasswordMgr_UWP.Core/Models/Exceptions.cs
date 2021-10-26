using System;
using System.Collections.Generic;
using System.Text;

namespace PasswordMgr_UWP.Core.Models
{
    public class DatabaseEncryptedException : Exception
    {
        public DatabaseEncryptedException() : base() { }
        public DatabaseEncryptedException(string message) : base(message) { }
        public DatabaseEncryptedException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class NullValueException : Exception
    {
        public NullValueException() : base() { }
        public NullValueException(string message) : base(message) { }
        public NullValueException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class PasswordTypeException : Exception
    {
        public PasswordTypeException() : base() { }
        public PasswordTypeException(string message) : base(message) { }
        public PasswordTypeException(string message, Exception innerException) : base(message, innerException) { }
    }
}
