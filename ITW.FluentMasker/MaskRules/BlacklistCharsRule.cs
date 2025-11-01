using System;
using System.Buffers;
using System.Collections.Generic;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Masks characters that are in a blacklist (inverse of whitelist), replacing them with a specified string.
    /// </summary>
    /// <remarks>
    /// <para>Only characters present in the blacklist will be masked in the output.</para>
    /// <para>All other characters remain unchanged.</para>
    /// <para>Uses ArrayPool&lt;char&gt; internally for high performance and low memory allocation.</para>
    /// <para>Null and empty strings are returned unchanged.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Mask @ and . in email
    /// var rule = new BlacklistCharsRule("@.", "*");
    /// var result = rule.Apply("test@example.com");
    /// // result = "test*example*com"
    ///
    /// // Mask special characters
    /// var rule2 = new BlacklistCharsRule("!@#$%^&amp;*()", "");
    /// var result2 = rule2.Apply("Hello@World!");
    /// // result2 = "HelloWorld"
    ///
    /// // Mask digits
    /// var rule3 = new BlacklistCharsRule("0123456789", "X");
    /// var result3 = rule3.Apply("Card 1234-5678");
    /// // result3 = "Card XXXX-XXXX"
    ///
    /// // Edge cases:
    /// rule.Apply("");        // Returns "" (empty string unchanged)
    /// rule.Apply(null);      // Returns null
    /// </code>
    /// </example>
    public class BlacklistCharsRule : IMaskRule, IMaskRule<string, string>
    {
        private readonly HashSet<char> _blacklistedChars;
        private readonly string _replaceWith;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlacklistCharsRule"/> class with a string of blacklisted characters.
        /// </summary>
        /// <param name="blacklistedChars">String containing all characters that should be masked</param>
        /// <param name="replaceWith">String to replace blacklisted characters with (default: "*")</param>
        /// <exception cref="ArgumentException">Thrown when blacklistedChars is null or empty</exception>
        public BlacklistCharsRule(string blacklistedChars, string replaceWith = "*")
        {
            if (string.IsNullOrEmpty(blacklistedChars))
                throw new ArgumentException("Blacklisted chars cannot be null or empty", nameof(blacklistedChars));

            _blacklistedChars = new HashSet<char>(blacklistedChars);
            _replaceWith = replaceWith ?? string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlacklistCharsRule"/> class with a collection of blacklisted characters.
        /// </summary>
        /// <param name="blacklistedChars">Collection of characters that should be masked</param>
        /// <param name="replaceWith">String to replace blacklisted characters with (default: "*")</param>
        /// <exception cref="ArgumentNullException">Thrown when blacklistedChars is null</exception>
        public BlacklistCharsRule(IEnumerable<char> blacklistedChars, string replaceWith = "*")
        {
            _blacklistedChars = new HashSet<char>(blacklistedChars ?? throw new ArgumentNullException(nameof(blacklistedChars)));
            _replaceWith = replaceWith ?? string.Empty;
        }

        /// <summary>
        /// Applies the blacklist rule to the input string, masking blacklisted characters.
        /// </summary>
        /// <param name="input">The string to mask. Can be null or empty.</param>
        /// <returns>
        /// A string where blacklisted characters are replaced with the specified replacement string.
        /// Non-blacklisted characters remain unchanged.
        /// Returns the original input if it is null or empty.
        /// </returns>
        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var pool = ArrayPool<char>.Shared;
            // Worst case: every character is blacklisted and replaced with replaceWith string
            char[] buffer = pool.Rent(input.Length * Math.Max(1, _replaceWith.Length));
            int writeIndex = 0;

            try
            {
                foreach (char c in input)
                {
                    if (_blacklistedChars.Contains(c))
                    {
                        // Character is blacklisted, replace with replaceWith string
                        if (_replaceWith.Length > 0)
                        {
                            foreach (char r in _replaceWith)
                                buffer[writeIndex++] = r;
                        }
                        // else: replaceWith is empty, so we skip this character (remove it)
                    }
                    else
                    {
                        // Character not blacklisted, keep it
                        buffer[writeIndex++] = c;
                    }
                }

                return new string(buffer, 0, writeIndex);
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
