using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PasswordMgr_UWP.Core.Models
{
    public static class GenerationTools
    {
        /// <summary>
        /// Generates a password.
        /// </summary>
        /// <param name="passwordOptions">Options for password generation.</param>
        /// <returns>A random generated password</returns>
        public static string GeneratePassword(OptionSet passwordOptions)
        {
            if (passwordOptions == null)
                throw new ArgumentNullException(nameof(passwordOptions));

            var letters = new List<string>();
            var random = new Random();
            var password = new StringBuilder();

            #region If-Statements for used letters
            if (passwordOptions.UseUpperLetters)
                letters.Add(upperCaseLetters);

            if (passwordOptions.UseLowerLetters)
                letters.Add(lowerCaseLetters);

            if (passwordOptions.UseNumbers)
                letters.Add(numbers);

            if (passwordOptions.UseSymbols)
                letters.Add(symbols);
            #endregion

            for (int i = 0; i < passwordOptions.Lenght; i++)
            {
                byte array = (byte)random.Next(0, letters.Count);
                byte letter = (byte)random.Next(0, letters[array].Length);
                password.Append(letters[array][letter]);
            }

            return password.ToString();
        }

        /// <summary>
        /// Generates the given number of passwords.
        /// </summary>
        /// <param name="amount">The amount of passwords to create</param>
        /// <param name="passwordOptions">Options for password generation</param>
        /// <returns>Random generated passwords in an IEnumerable</returns>
        public static async Task<IEnumerable<string>> GeneratePasswordsAsync(int amount, OptionSet passwordOptions)
        {
            var passwords = new HashSet<string>();

            while (passwords.Count < amount)
                passwords.Add(GeneratePassword(passwordOptions));

            await Task.Yield();
            return passwords;
        }

        //Sets of characters to use in generation process.
        private static readonly string upperCaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly string lowerCaseLetters = upperCaseLetters.ToLower();
        private static readonly string numbers = "0123456789";
        private static readonly string symbols = @"€@%$&§?!<>=+-""*\/()[]{}#_.,:;";
    }
}
