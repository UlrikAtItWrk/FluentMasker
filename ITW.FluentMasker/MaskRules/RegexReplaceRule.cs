using System;
using System.Text.RegularExpressions;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Performs regex-based find and replace operations with ReDoS (Regular Expression Denial of Service) protection.
    /// </summary>
    /// <remarks>
    /// <para>This rule uses compiled regex patterns for optimal performance.</para>
    /// <para>A default 100ms timeout prevents catastrophic backtracking (ReDoS attacks).</para>
    /// <para>If the regex pattern times out, an InvalidOperationException is thrown.</para>
    /// <para>Null and empty strings are returned unchanged.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Replace all digits with 'X'
    /// var rule = new RegexReplaceRule(@"\d", "X");
    /// var result = rule.Apply("Order123");
    /// // result = "OrderXXX"
    ///
    /// // Replace email domain
    /// var rule2 = new RegexReplaceRule(@"@[\w.-]+", "@example.com");
    /// var result2 = rule2.Apply("user@gmail.com");
    /// // result2 = "user@example.com"
    ///
    /// // Custom timeout for complex patterns
    /// var rule3 = new RegexReplaceRule(@"complex.*pattern", "REDACTED", TimeSpan.FromMilliseconds(200));
    ///
    /// // With regex options
    /// var rule4 = new RegexReplaceRule(@"hello", "HELLO", RegexOptions.IgnoreCase);
    /// var result4 = rule4.Apply("Hello World");
    /// // result4 = "HELLO World"
    /// </code>
    /// </example>
    public class RegexReplaceRule : IStringMaskRule
    {
        private readonly Regex _pattern;
        private readonly string _replacement;
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexReplaceRule"/> class with default timeout.
        /// </summary>
        /// <param name="pattern">The regular expression pattern to search for</param>
        /// <param name="replacement">The replacement string</param>
        /// <param name="options">Optional regex options (default: None)</param>
        /// <exception cref="ArgumentException">Thrown when pattern is null/empty or invalid</exception>
        /// <exception cref="ArgumentNullException">Thrown when replacement is null</exception>
        public RegexReplaceRule(string pattern, string replacement, RegexOptions options = RegexOptions.None)
            : this(pattern, replacement, DefaultTimeout, options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexReplaceRule"/> class with custom timeout.
        /// </summary>
        /// <param name="pattern">The regular expression pattern to search for</param>
        /// <param name="replacement">The replacement string</param>
        /// <param name="timeout">Maximum time allowed for regex matching to prevent ReDoS attacks</param>
        /// <param name="options">Optional regex options (default: None)</param>
        /// <exception cref="ArgumentException">Thrown when pattern is null/empty or invalid</exception>
        /// <exception cref="ArgumentNullException">Thrown when replacement is null</exception>
        public RegexReplaceRule(string pattern, string replacement, TimeSpan timeout, RegexOptions options = RegexOptions.None)
        {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

            _replacement = replacement ?? throw new ArgumentNullException(nameof(replacement));

            try
            {
                // Compile for better performance
                _pattern = new Regex(pattern, options | RegexOptions.Compiled, timeout);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Invalid regex pattern: {pattern}", nameof(pattern), ex);
            }
        }

        /// <summary>
        /// Applies the regex replacement to the input string.
        /// </summary>
        /// <param name="input">The string to process. Can be null or empty.</param>
        /// <returns>
        /// The string with all pattern matches replaced by the replacement string.
        /// Returns the original input if it is null or empty.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the regex matching exceeds the timeout threshold, indicating a potential ReDoS attack.
        /// </exception>
        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            try
            {
                return _pattern.Replace(input, _replacement);
            }
            catch (RegexMatchTimeoutException ex)
            {
                throw new InvalidOperationException(
                    $"Regex timeout exceeded ({_pattern.MatchTimeout.TotalMilliseconds}ms). " +
                    $"Pattern may be too complex or input too long. Consider simplifying the pattern or increasing the timeout.", ex);
            }
        }

        // Explicit interface implementation to avoid ambiguity in method overload resolution
        string IMaskRule<string, string>.Apply(string input) => Apply(input);
    }
}
