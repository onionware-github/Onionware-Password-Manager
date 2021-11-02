using System.Linq;

namespace PasswordMgr_UWP.Core.Extensions
{
    public static class StringExtensions
    {
        //Entfernt verbotene Zeichen, damit der String als Dateiname verwendet werden kann
        public static string TrimToFilename(this string input)
        {
            char[] forbiddenChars = @"<>:/\|?""*".ToCharArray();
            return string.Concat(input.Trim().Where(c => !char.IsWhiteSpace(c)).Where(c => !forbiddenChars.Contains(c)));
        }

        public static bool IsNullOrEmpty(this string input)
                => string.IsNullOrEmpty(input);
    }
}