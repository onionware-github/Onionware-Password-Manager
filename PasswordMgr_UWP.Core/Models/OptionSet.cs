namespace PasswordMgr_UWP.Core.Models
{
    public class OptionSet
    {
        /// <summary>
        /// Initializes a new object of OptionSet with the given properties
        /// </summary>
        /// <param name="lenght">The lenght of the password</param>
        /// <param name="useUpper">Use uppercase letters in the password</param>
        /// <param name="useLower">Use lowercase letters in the password</param>
        /// <param name="useNumbers">Use numbers in the password</param>
        /// <param name="useSymbols">Use symbols in the password</param>
        public OptionSet(int lenght, bool useUpper = true, bool useLower = true, bool useNumbers = true, bool useSymbols = true)
        {
            UseUpperLetters = useUpper;
            UseLowerLetters = useLower;
            UseNumbers = useNumbers;
            UseSymbols = useSymbols;
            Lenght = lenght;
        }

        /// <summary>
        /// Use uppercase letters in the password
        /// </summary>
        public bool UseUpperLetters { get; set; }

        /// <summary>
        /// Use lowercase letters in the password
        /// </summary>
        public bool UseLowerLetters { get; set; }

        /// <summary>
        /// Use numbers in the password
        /// </summary>
        public bool UseNumbers { get; set; }

        /// <summary>
        /// Use symbols in the password
        /// </summary>
        public bool UseSymbols { get; set; }

        /// <summary>
        /// The lenght of the password
        /// </summary>
        public int Lenght { get; set; }

        /// <summary>
        /// The strenght of the password
        /// </summary>
        public PasswordStrength PasswordStrength
        {
            get
            {
                switch (Lenght)
                {
                    case int n when n <= 4:
                        return PasswordStrength.VeryWeak;

                    case int n when n <= 9:
                        return PasswordStrength.Weak;

                    case int n when n <= 16:
                        return PasswordStrength.Average;

                    case int n when n <= 24:
                        return PasswordStrength.Strong;

                    case int n when n <= 40:
                        return PasswordStrength.VeryStrong;

                    default:
                        return PasswordStrength.Awesome;
                }
            }
        }
    }
}
