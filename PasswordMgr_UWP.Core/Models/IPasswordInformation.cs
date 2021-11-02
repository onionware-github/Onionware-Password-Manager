using System.Threading.Tasks;

namespace PasswordMgr_UWP.Core.Models
{
    public interface IPasswordInformation
    {
        string Name { get; set; }
        string Info { get; set; }
        string PlaintextPassword { get; set; }
        string JsonPath { get; set; }
        PasswordType PasswordType { get; }

        byte[] Password { get; set; }
        byte[] IV { get; set; }
        string Salt { get; set; }

        bool IsDecrypted { get; set; }
        bool IsDecryptButtonEnabled { get; }
        void Encrypt();
        Task Decrypt(string password);
    }

    public enum PasswordType
    {
        Password,
        Database
    }
}
