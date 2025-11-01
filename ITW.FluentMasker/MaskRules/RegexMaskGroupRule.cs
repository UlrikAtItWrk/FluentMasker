using System;
using System.Buffers;
using System.Text.RegularExpressions;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Masks only specific capture groups within regex pattern matches, leaving the rest of the string unchanged.
    /// </summary>
    /// <remarks>
    /// <para>This rule uses compiled regex patterns for optimal performance.</para>
    /// <para>A default 100ms timeout prevents catastrophic backtracking (ReDoS attacks).</para>
    /// <para>If the specified group doesn't exist in a match, that match is left unchanged.</para>
    /// <para>Null and empty strings are returned unchanged.</para>
    /// <para>Group 0 refers to the entire match. Named groups can be used via overload methods.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Mask area code in phone number (group 1)
    /// var rule = new RegexMaskGroupRule(@"(\d{3})-(\d{3})-(\d{4})", 1, "*");
    /// var result = rule.Apply("555-123-4567");
    /// // result = "***-123-4567"
    ///
    /// // Mask middle section of SSN (group 2)
    /// var rule2 = new RegexMaskGroupRule(@"(\d{3})-(\d{2})-(\d{4})", 2, "X");
    /// var result2 = rule2.Apply("123-45-6789");
    /// // result2 = "123-XX-6789"
    ///
    /// // Mask entire match (group 0)
    /// var rule3 = new RegexMaskGroupRule(@"\d+", 0, "#");
    /// var result3 = rule3.Apply("Order 12345 ready");
    /// // result3 = "Order ##### ready"
    ///
    /// // With regex options
    /// var rule4 = new RegexMaskGroupRule(@"(user)\d+", 1, "*", RegexOptions.IgnoreCase);
    /// var result4 = rule4.Apply("USER123");
    /// // result4 = "****123"
    /// </code>
    /// </example>
    public class RegexMaskGroupRule : IStringMaskRule
    {
        private readonly Regex _pattern;
        private readonly int _groupIndex;
        private readonly string _maskChar;
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexMaskGroupRule"/> class with default timeout.
        /// </summary>
        /// <param name="pattern">The regular expression pattern with capture groups</param>
        /// <param name="groupIndex">The index of the capture group to mask (0 = entire match, 1+ = capture groups)</param>
        /// <param name="maskChar">The character to use for masking (default: "*")</param>
        /// <param name="options">Optional regex options (default: None)</param>
        /// <exception cref="ArgumentException">Thrown when pattern is null/empty or groupIndex is negative</exception>
        /// <exception cref="ArgumentNullException">Thrown when maskChar is null</exception>
        public RegexMaskGroupRule(string pattern, int groupIndex, string maskChar = "*", RegexOptions options = RegexOptions.None)
            : this(pattern, groupIndex, maskChar, DefaultTimeout, options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexMaskGroupRule"/> class with custom timeout.
        /// </summary>
        /// <param name="pattern">The regular expression pattern with capture groups</param>
        /// <param name="groupIndex">The index of the capture group to mask (0 = entire match, 1+ = capture groups)</param>
        /// <param name="maskChar">The character to use for masking (default: "*")</param>
        /// <param name="timeout">Maximum time allowed for regex matching to prevent ReDoS attacks</param>
        /// <param name="options">Optional regex options (default: None)</param>
        /// <exception cref="ArgumentException">Thrown when pattern is null/empty or groupIndex is negative</exception>
        /// <exception cref="ArgumentNullException">Thrown when maskChar is null</exception>
        public RegexMaskGroupRule(string pattern, int groupIndex, string maskChar, TimeSpan timeout, RegexOptions options = RegexOptions.None)
        {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));
            if (groupIndex < 0)
                throw new ArgumentException("Group index must be non-negative", nameof(groupIndex));

            _maskChar = maskChar ?? throw new ArgumentNullException(nameof(maskChar));

            if (_maskChar.Length == 0)
                throw new ArgumentException("Mask character cannot be empty", nameof(maskChar));

            try
            {
                // Compile for better performance
                _pattern = new Regex(pattern, options | RegexOptions.Compiled, timeout);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Invalid regex pattern: {pattern}", nameof(pattern), ex);
            }

            _groupIndex = groupIndex;
        }

        /// <summary>
        /// Applies the regex group masking to the input string.
        /// </summary>
        /// <param name="input">The string to process. Can be null or empty.</param>
        /// <returns>
        /// The string with the specified capture group(s) masked in all pattern matches.
        /// Returns the original input if it is null or empty.
        /// If the specified group doesn't exist in a match, that match is left unchanged.
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
                return _pattern.Replace(input, match =>
                {
                    // Check if the group exists in this match
                    if (_groupIndex >= match.Groups.Count)
                        return match.Value; // Group doesn't exist, return original

                    var group = match.Groups[_groupIndex];

                    // If group didn't match (can happen with optional groups), return original
                    if (!group.Success)
                        return match.Value;

                    // Use ArrayPool for performance
                    var resultLength = match.Value.Length;
                    char[] buffer = ArrayPool<char>.Shared.Rent(resultLength);

                    try
                    {
                        // Copy the entire match to buffer
                        match.Value.CopyTo(0, buffer, 0, resultLength);

                        // Calculate the position of the group within the match
                        int groupStartInMatch = group.Index - match.Index;
                        int groupLength = group.Length;

                        // Mask the specific group
                        char maskCharacter = _maskChar[0];
                        for (int i = 0; i < groupLength; i++)
                        {
                            buffer[groupStartInMatch + i] = maskCharacter;
                        }

                        return new string(buffer, 0, resultLength);
                    }
                    finally
                    {
                        ArrayPool<char>.Shared.Return(buffer);
                    }
                });
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
