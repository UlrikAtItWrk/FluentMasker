using System;
using System.Buffers;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Specifies the character class to be masked.
    /// </summary>
    public enum CharClass
    {
        /// <summary>
        /// Numeric digits (0-9)
        /// </summary>
        Digit,

        /// <summary>
        /// Alphabetic letters (A-Z, a-z)
        /// </summary>
        Letter,

        /// <summary>
        /// Alphanumeric characters (letters or digits)
        /// </summary>
        LetterOrDigit,

        /// <summary>
        /// Whitespace characters (space, tab, newline, etc.)
        /// </summary>
        Whitespace,

        /// <summary>
        /// Punctuation characters (.,;:!? etc.)
        /// </summary>
        Punctuation,

        /// <summary>
        /// Uppercase letters (A-Z)
        /// </summary>
        Upper,

        /// <summary>
        /// Lowercase letters (a-z)
        /// </summary>
        Lower
    }

    /// <summary>
    /// Masks characters in a string based on their character class (digit, letter, whitespace, etc.).
    /// </summary>
    /// <remarks>
    /// <para>This rule allows selective masking of specific character types within a string.</para>
    /// <para>Only characters matching the specified character class are masked; other characters remain unchanged.</para>
    /// <para>Uses ArrayPool&lt;char&gt; internally for high performance and low memory allocation.</para>
    /// <para>Null and empty strings are returned unchanged.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Mask all digits
    /// var digitRule = new MaskCharClassRule(CharClass.Digit, "*");
    /// var result1 = digitRule.Apply("Test123");
    /// // result1 = "Test***"
    ///
    /// // Mask all letters
    /// var letterRule = new MaskCharClassRule(CharClass.Letter, "*");
    /// var result2 = letterRule.Apply("Test123");
    /// // result2 = "****123"
    ///
    /// // Mask whitespace
    /// var whitespaceRule = new MaskCharClassRule(CharClass.Whitespace, "_");
    /// var result3 = whitespaceRule.Apply("Hello World");
    /// // result3 = "Hello_World"
    ///
    /// // Mask uppercase letters
    /// var upperRule = new MaskCharClassRule(CharClass.Upper, "*");
    /// var result4 = upperRule.Apply("HelloWorld");
    /// // result4 = "*ello*orld"
    ///
    /// // Edge cases:
    /// digitRule.Apply("");        // Returns "" (empty string unchanged)
    /// digitRule.Apply(null);      // Returns null
    /// </code>
    /// </example>
    public class MaskCharClassRule : IMaskRule, IMaskRule<string, string>
    {
        private readonly CharClass _charClass;
        private readonly string _maskChar;
        private readonly Func<char, bool> _predicate;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskCharClassRule"/> class.
        /// </summary>
        /// <param name="charClass">The character class to mask</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <exception cref="ArgumentNullException">Thrown when maskChar is null</exception>
        /// <exception cref="ArgumentException">Thrown when maskChar is empty or when an unsupported character class is specified</exception>
        public MaskCharClassRule(CharClass charClass, string maskChar = "*")
        {
            if (maskChar == null)
                throw new ArgumentNullException(nameof(maskChar));
            if (string.IsNullOrEmpty(maskChar))
                throw new ArgumentException("Mask character cannot be empty", nameof(maskChar));

            _charClass = charClass;
            _maskChar = maskChar;

            // Set up predicate function based on character class
            _predicate = charClass switch
            {
                CharClass.Digit => char.IsDigit,
                CharClass.Letter => char.IsLetter,
                CharClass.LetterOrDigit => char.IsLetterOrDigit,
                CharClass.Whitespace => char.IsWhiteSpace,
                CharClass.Punctuation => char.IsPunctuation,
                CharClass.Upper => char.IsUpper,
                CharClass.Lower => char.IsLower,
                _ => throw new ArgumentException($"Unsupported character class: {charClass}", nameof(charClass))
            };
        }

        /// <summary>
        /// Applies the mask rule to the input string, masking characters of the specified character class.
        /// </summary>
        /// <param name="input">The string to mask. Can be null or empty.</param>
        /// <returns>
        /// The masked string with characters matching the specified class replaced by the mask character.
        /// Characters not matching the class are preserved unchanged.
        /// Returns the original input if it is null or empty.
        /// </returns>
        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Use ArrayPool for better performance
            var pool = ArrayPool<char>.Shared;
            char[] buffer = pool.Rent(input.Length);

            try
            {
                // Iterate through input and mask matching characters
                for (int i = 0; i < input.Length; i++)
                {
                    buffer[i] = _predicate(input[i]) ? _maskChar[0] : input[i];
                }

                return new string(buffer, 0, input.Length);
            }
            finally
            {
                pool.Return(buffer);
            }
        }

        // Explicit interface implementation to avoid ambiguity in method overload resolution
        string IMaskRule<string, string>.Apply(string input) => Apply(input);
    }
}
