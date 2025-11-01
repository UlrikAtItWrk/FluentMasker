using System;
using System.Buffers;
using System.Collections.Generic;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Filters a string to only include characters from a whitelist, optionally replacing non-whitelisted characters.
    /// </summary>
    /// <remarks>
    /// <para>Only characters present in the allowed character set will remain in the output.</para>
    /// <para>Non-whitelisted characters can be either removed (when replaceWith="") or replaced with a specified string.</para>
    /// <para>Uses ArrayPool&lt;char&gt; internally for high performance and low memory allocation.</para>
    /// <para>Null and empty strings are returned unchanged.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Remove non-alphanumeric characters
    /// var rule = new WhitelistCharsRule("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
    /// var result = rule.Apply("Hello@World123!");
    /// // result = "HelloWorld123"
    ///
    /// // Replace non-digits with asterisks
    /// var rule2 = new WhitelistCharsRule("0123456789", "*");
    /// var result2 = rule2.Apply("Card: 1234-5678");
    /// // result2 = "******1234*5678"
    ///
    /// // Edge cases:
    /// rule.Apply("");        // Returns "" (empty string unchanged)
    /// rule.Apply(null);      // Returns null
    /// </code>
    /// </example>
    public class WhitelistCharsRule : IMaskRule, IMaskRule<string, string>
    {
        private readonly HashSet<char> _allowedChars;
        private readonly string _replaceWith;

        /// <summary>
        /// Initializes a new instance of the <see cref="WhitelistCharsRule"/> class with a string of allowed characters.
        /// </summary>
        /// <param name="allowedChars">String containing all characters that should be whitelisted</param>
        /// <param name="replaceWith">String to replace non-whitelisted characters with. Empty string removes them. (default: "")</param>
        /// <exception cref="ArgumentException">Thrown when allowedChars is null or empty</exception>
        public WhitelistCharsRule(string allowedChars, string replaceWith = "")
        {
            if (string.IsNullOrEmpty(allowedChars))
                throw new ArgumentException("Allowed chars cannot be null or empty", nameof(allowedChars));

            _allowedChars = new HashSet<char>(allowedChars);
            _replaceWith = replaceWith ?? string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WhitelistCharsRule"/> class with a collection of allowed characters.
        /// </summary>
        /// <param name="allowedChars">Collection of characters that should be whitelisted</param>
        /// <param name="replaceWith">String to replace non-whitelisted characters with. Empty string removes them. (default: "")</param>
        /// <exception cref="ArgumentNullException">Thrown when allowedChars is null</exception>
        public WhitelistCharsRule(IEnumerable<char> allowedChars, string replaceWith = "")
        {
            _allowedChars = new HashSet<char>(allowedChars ?? throw new ArgumentNullException(nameof(allowedChars)));
            _replaceWith = replaceWith ?? string.Empty;
        }

        /// <summary>
        /// Applies the whitelist rule to the input string, keeping only allowed characters.
        /// </summary>
        /// <param name="input">The string to filter. Can be null or empty.</param>
        /// <returns>
        /// A string containing only whitelisted characters. Non-whitelisted characters are either removed or replaced based on replaceWith parameter.
        /// Returns the original input if it is null or empty.
        /// </returns>
        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var pool = ArrayPool<char>.Shared;
            // Worst case: every character is replaced with replaceWith string
            char[] buffer = pool.Rent(input.Length * Math.Max(1, _replaceWith.Length));
            int writeIndex = 0;

            try
            {
                foreach (char c in input)
                {
                    if (_allowedChars.Contains(c))
                    {
                        // Character is whitelisted, keep it
                        buffer[writeIndex++] = c;
                    }
                    else if (_replaceWith.Length > 0)
                    {
                        // Character not whitelisted, replace with replaceWith string
                        foreach (char r in _replaceWith)
                            buffer[writeIndex++] = r;
                    }
                    // else: replaceWith is empty, so we skip this character (remove it)
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
