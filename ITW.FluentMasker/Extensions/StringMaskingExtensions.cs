using System;
using ITW.FluentMasker.Builders;
using ITW.FluentMasker.MaskRules;

namespace ITW.FluentMasker.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="StringMaskingBuilder"/> providing a fluent API for string masking operations.
    /// These methods enable chainable masking rules that are applied in sequence.
    /// </summary>
    public static class StringMaskingExtensions
    {
        /// <summary>
        /// Sets a seed provider for deterministic masking on the next seeded string rule.
        /// The seed provider enables consistent masking where the same input always produces the same masked output.
        /// </summary>
        /// <param name="builder">The string masking builder instance</param>
        /// <param name="seedProvider">Function that generates a seed value from the input string</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <remarks>
        /// <para>
        /// The seed provider is applied to the next mask rule that implements <see cref="ISeededMaskRule{T}"/>.
        /// After being applied, the seed provider is cleared, so it only affects one rule.
        /// </para>
        /// <para>
        /// This is primarily used for future seeded string rules. Currently, most seeded rules are numeric.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var builder = new StringMaskingBuilder()
        ///     .WithRandomSeed(str => str.GetHashCode())
        ///     .AddRule(someSeededStringRule);
        /// </code>
        /// </example>
        public static StringMaskingBuilder WithRandomSeed(
            this StringMaskingBuilder builder,
            SeedProvider<string> seedProvider)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (seedProvider == null)
                throw new ArgumentNullException(nameof(seedProvider));

            builder.PendingSeedProvider = seedProvider;
            return builder;
        }

        /// <summary>
        /// Sets a constant seed value for deterministic masking on the next seeded string rule.
        /// Convenience overload for simple deterministic masking scenarios.
        /// </summary>
        /// <param name="builder">The string masking builder instance</param>
        /// <param name="seed">Constant seed value to use for all operations</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var builder = new StringMaskingBuilder()
        ///     .WithRandomSeed(12345)
        ///     .AddRule(someSeededStringRule);
        /// </code>
        /// </example>
        public static StringMaskingBuilder WithRandomSeed(
            this StringMaskingBuilder builder,
            int seed)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            // Create a constant seed provider
            SeedProvider<string> seedProvider = _ => seed;
            builder.PendingSeedProvider = seedProvider;
            return builder;
        }


        /// <summary>
        /// Masks the first N characters of the string.
        /// Example: "Hello" with count=2 becomes "**llo"
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="count">Number of characters to mask from the start</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.Name, m => m.MaskStart(2));
        /// // "John" becomes "**hn"
        /// </code>
        /// </example>
        public static StringMaskingBuilder MaskStart(this StringMaskingBuilder builder, int count, string maskChar = "*")
        {
            return builder.AddRule(new MaskStartRule(count, maskChar));
        }

        /// <summary>
        /// Masks the last N characters of the string.
        /// Example: "Hello" with count=2 becomes "Hel**"
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="count">Number of characters to mask from the end</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.Name, m => m.MaskEnd(2));
        /// // "John" becomes "Jo**"
        /// </code>
        /// </example>
        public static StringMaskingBuilder MaskEnd(this StringMaskingBuilder builder, int count, string maskChar = "*")
        {
            return builder.AddRule(new MaskEndRule(count, maskChar));
        }

        /// <summary>
        /// Keeps the first and last N characters visible and masks everything in between.
        /// Example: "HelloWorld" with keepFirst=2, keepLast=2 becomes "He******ld"
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="keepFirst">Number of characters to keep visible at the start</param>
        /// <param name="keepLast">Number of characters to keep visible at the end</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.Email, m => m.MaskMiddle(2, 4));
        /// // "john@example.com" becomes "jo*******e.com"
        /// </code>
        /// </example>
        public static StringMaskingBuilder MaskMiddle(this StringMaskingBuilder builder, int keepFirst, int keepLast, string maskChar = "*")
        {
            return builder.AddRule(new MaskMiddleRule(keepFirst, keepLast, maskChar));
        }

        /// <summary>
        /// Keeps the first N characters visible and masks the rest.
        /// Example: "Hello" with count=2 becomes "He***"
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="count">Number of characters to keep visible from the start</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.CreditCard, m => m.KeepFirst(4));
        /// // "1234567890123456" becomes "1234************"
        /// </code>
        /// </example>
        public static StringMaskingBuilder KeepFirst(this StringMaskingBuilder builder, int count, string maskChar = "*")
        {
            return builder.AddRule(new KeepFirstRule(count, maskChar));
        }

        /// <summary>
        /// Keeps the last N characters visible and masks the rest.
        /// Example: "Hello" with count=2 becomes "***lo"
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="count">Number of characters to keep visible from the end</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.CreditCard, m => m.KeepLast(4));
        /// // "1234567890123456" becomes "************3456"
        /// </code>
        /// </example>
        public static StringMaskingBuilder KeepLast(this StringMaskingBuilder builder, int count, string maskChar = "*")
        {
            return builder.AddRule(new KeepLastRule(count, maskChar));
        }

        /// <summary>
        /// Masks a specific range of characters starting at a position with a given length.
        /// Example: "HelloWorld" with start=2, length=5 becomes "He*****rld"
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="start">Starting position (0-based index)</param>
        /// <param name="length">Number of characters to mask</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.PhoneNumber, m => m.MaskRange(3, 4));
        /// // "555-1234" becomes "555-****"
        /// </code>
        /// </example>
        public static StringMaskingBuilder MaskRange(this StringMaskingBuilder builder, int start, int length, string maskChar = "*")
        {
            return builder.AddRule(new MaskRangeRule(start, length, maskChar));
        }

        /// <summary>
        /// Replaces the entire value with null, effectively removing it.
        /// This is useful for complete redaction of sensitive fields.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.SensitiveData, m => m.NullOut());
        /// // Any value becomes null
        /// </code>
        /// </example>
        public static StringMaskingBuilder NullOut(this StringMaskingBuilder builder)
        {
            return builder.AddRule(new NullOutRule());
        }

        /// <summary>
        /// Replaces the entire value with a redaction placeholder text.
        /// This is useful for indicating that a field has been redacted with a visible marker.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="text">The redaction text to use (default: "[REDACTED]")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.SensitiveData, m => m.Redact());
        /// // "secret123" becomes "[REDACTED]"
        ///
        /// masker.MaskFor(x => x.Password, m => m.Redact("[HIDDEN]"));
        /// // "password" becomes "[HIDDEN]"
        /// </code>
        /// </example>
        public static StringMaskingBuilder Redact(this StringMaskingBuilder builder, string text = "[REDACTED]")
        {
            return builder.AddRule(new RedactRule(text));
        }

        /// <summary>
        /// Truncates the string to a maximum length with an optional suffix.
        /// If the string exceeds maxLength, it is truncated and the suffix is appended.
        /// The suffix length is considered to ensure total length does not exceed maxLength.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="maxLength">Maximum allowed length (including suffix)</param>
        /// <param name="suffix">Suffix to append when truncating (default: "…")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.Description, m => m.Truncate(10));
        /// // "A very long string" becomes "A very lo…"
        ///
        /// masker.MaskFor(x => x.Comment, m => m.Truncate(15, "..."));
        /// // "This is a very long comment" becomes "This is a ve..."
        /// </code>
        /// </example>
        public static StringMaskingBuilder Truncate(this StringMaskingBuilder builder, int maxLength, string suffix = "…")
        {
            return builder.AddRule(new TruncateRule(maxLength, suffix));
        }

        /// <summary>
        /// Applies a template-based mask using declarative syntax.
        /// Templates use {{token}} placeholders for dynamic masking patterns.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="template">The template string with {{token}} placeholders</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <remarks>
        /// Supported tokens:
        /// - {{F}} or {{F|n}}: First n characters (default 1)
        /// - {{L}} or {{L|n}}: Last n characters (default 1)
        /// - {{*xN}}: N masked characters (e.g., {{*x5}} = "*****")
        /// - {{digits}} or {{digits|start-end}}: Extract digits with optional range
        /// - {{letters}} or {{letters|start-end}}: Extract letters with optional range
        /// </remarks>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        ///
        /// // Show first and last character with masking in between
        /// masker.MaskFor(x => x.Name, m => m.TemplateMask("{{F}}{{*x6}}{{L}}"));
        /// // "JohnDoe" becomes "J******e"
        ///
        /// // Show first 2 characters with masking
        /// masker.MaskFor(x => x.Name, m => m.TemplateMask("{{F|2}}{{*x10}}"));
        /// // "SarahJohnson" becomes "Sa**********"
        ///
        /// // Phone number masking with digit extraction
        /// masker.MaskFor(x => x.Phone, m => m.TemplateMask("+{{digits|0-2}} ** ** {{digits|-2}}"));
        /// // "+45 12 34 56 78" becomes "+45 ** ** 78"
        /// </code>
        /// </example>
        public static StringMaskingBuilder TemplateMask(this StringMaskingBuilder builder, string template)
        {
            return builder.AddRule(new TemplateMaskRule(template));
        }

        /// <summary>
        /// Applies a regular expression pattern replacement with ReDoS protection.
        /// Finds all matches of the pattern and replaces them with the replacement string.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="pattern">The regular expression pattern to search for</param>
        /// <param name="replacement">The replacement string (can use $1, $2 for capture groups)</param>
        /// <param name="options">Optional regex options (default: None)</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <remarks>
        /// This method has a default 100ms timeout to prevent ReDoS (Regular Expression Denial of Service) attacks.
        /// If the regex matching exceeds this timeout, an InvalidOperationException will be thrown.
        /// </remarks>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        ///
        /// // Replace all digits with 'X'
        /// masker.MaskFor(x => x.OrderNumber, m => m.RegexReplace(@"\d", "X"));
        /// // "Order123" becomes "OrderXXX"
        ///
        /// // Replace email domain
        /// masker.MaskFor(x => x.Email, m => m.RegexReplace(@"@[\w.-]+", "@example.com"));
        /// // "user@gmail.com" becomes "user@example.com"
        ///
        /// // Case-insensitive replacement
        /// masker.MaskFor(x => x.Text, m => m.RegexReplace(@"hello", "hi", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        /// // "Hello World" becomes "hi World"
        ///
        /// // Use capture groups for partial masking
        /// masker.MaskFor(x => x.SSN, m => m.RegexReplace(@"(\d{3})-(\d{2})-(\d{4})", "$1-XX-$3"));
        /// // "123-45-6789" becomes "123-XX-6789"
        /// </code>
        /// </example>
        public static StringMaskingBuilder RegexReplace(this StringMaskingBuilder builder, string pattern, string replacement, System.Text.RegularExpressions.RegexOptions options = System.Text.RegularExpressions.RegexOptions.None)
        {
            return builder.AddRule(new RegexReplaceRule(pattern, replacement, options));
        }

        /// <summary>
        /// Masks only a specific capture group within regex pattern matches, leaving the rest unchanged.
        /// Uses a numeric group index (0 = entire match, 1+ = capture groups).
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="pattern">The regular expression pattern with capture groups</param>
        /// <param name="groupIndex">The index of the capture group to mask (0 = entire match, 1+ = capture groups)</param>
        /// <param name="maskChar">The character to use for masking (default: "*")</param>
        /// <param name="options">Optional regex options (default: None)</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <remarks>
        /// This method has a default 100ms timeout to prevent ReDoS (Regular Expression Denial of Service) attacks.
        /// If the specified group doesn't exist in a match, that match is left unchanged.
        /// </remarks>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        ///
        /// // Mask area code in phone number (group 1)
        /// masker.MaskFor(x => x.Phone, m => m.RegexMaskGroup(@"(\d{3})-(\d{3})-(\d{4})", 1));
        /// // "555-123-4567" becomes "***-123-4567"
        ///
        /// // Mask middle section of SSN (group 2)
        /// masker.MaskFor(x => x.SSN, m => m.RegexMaskGroup(@"(\d{3})-(\d{2})-(\d{4})", 2, "X"));
        /// // "123-45-6789" becomes "123-XX-6789"
        ///
        /// // Mask entire match (group 0)
        /// masker.MaskFor(x => x.OrderNumber, m => m.RegexMaskGroup(@"\d+", 0, "#"));
        /// // "Order 12345 ready" becomes "Order ##### ready"
        /// </code>
        /// </example>
        public static StringMaskingBuilder RegexMaskGroup(this StringMaskingBuilder builder, string pattern, int groupIndex, string maskChar = "*", System.Text.RegularExpressions.RegexOptions options = System.Text.RegularExpressions.RegexOptions.None)
        {
            return builder.AddRule(new RegexMaskGroupRule(pattern, groupIndex, maskChar, options));
        }

        /// <summary>
        /// Masks only a specific named capture group within regex pattern matches, leaving the rest unchanged.
        /// Uses a named group identifier from the regex pattern.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="pattern">The regular expression pattern with named capture groups</param>
        /// <param name="groupName">The name of the capture group to mask</param>
        /// <param name="maskChar">The character to use for masking (default: "*")</param>
        /// <param name="options">Optional regex options (default: None)</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <remarks>
        /// This method has a default 100ms timeout to prevent ReDoS (Regular Expression Denial of Service) attacks.
        /// If the specified group doesn't exist in a match, that match is left unchanged.
        /// </remarks>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        ///
        /// // Mask named group 'area' in phone number
        /// masker.MaskFor(x => x.Phone, m => m.RegexMaskGroup(@"(?&lt;area&gt;\d{3})-(?&lt;prefix&gt;\d{3})-(?&lt;line&gt;\d{4})", "area"));
        /// // "555-123-4567" becomes "***-123-4567"
        ///
        /// // Mask 'middle' section of SSN
        /// masker.MaskFor(x => x.SSN, m => m.RegexMaskGroup(@"(?&lt;first&gt;\d{3})-(?&lt;middle&gt;\d{2})-(?&lt;last&gt;\d{4})", "middle", "X"));
        /// // "123-45-6789" becomes "123-XX-6789"
        /// </code>
        /// </example>
        public static StringMaskingBuilder RegexMaskGroup(this StringMaskingBuilder builder, string pattern, string groupName, string maskChar = "*", System.Text.RegularExpressions.RegexOptions options = System.Text.RegularExpressions.RegexOptions.None)
        {
            if (string.IsNullOrEmpty(groupName))
                throw new System.ArgumentException("Group name cannot be null or empty", nameof(groupName));

            // Convert named group to index using a temporary regex
            var tempRegex = new System.Text.RegularExpressions.Regex(pattern, options);
            int groupIndex = tempRegex.GroupNumberFromName(groupName);

            if (groupIndex == -1)
                throw new System.ArgumentException($"Named group '{groupName}' not found in pattern", nameof(groupName));

            return builder.AddRule(new RegexMaskGroupRule(pattern, groupIndex, maskChar, options));
        }

        /// <summary>
        /// Filters the string to only include characters from a whitelist, optionally replacing non-whitelisted characters.
        /// Characters not in the allowed set can be removed (when replaceWith="") or replaced with a specified string.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="allowedChars">String containing all characters that should be whitelisted</param>
        /// <param name="replaceWith">String to replace non-whitelisted characters with. Empty string removes them. (default: "")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        ///
        /// // Remove special characters (keep only alphanumeric)
        /// masker.MaskFor(x => x.Username, m => m.WhitelistChars("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"));
        /// // "Hello@World123!" becomes "HelloWorld123"
        ///
        /// // Replace non-digits with asterisks
        /// masker.MaskFor(x => x.PhoneDisplay, m => m.WhitelistChars("0123456789", "*"));
        /// // "555-1234" becomes "***5551234"
        /// </code>
        /// </example>
        public static StringMaskingBuilder WhitelistChars(this StringMaskingBuilder builder, string allowedChars, string replaceWith = "")
        {
            return builder.AddRule(new WhitelistCharsRule(allowedChars, replaceWith));
        }

        /// <summary>
        /// Convenience method to filter the string to only include alphanumeric characters (a-z, A-Z, 0-9).
        /// All other characters are removed.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.Username, m => m.WhitelistAlphanumeric());
        /// // "Hello@World123!" becomes "HelloWorld123"
        /// // "User_Name-2024" becomes "UserName2024"
        /// </code>
        /// </example>
        public static StringMaskingBuilder WhitelistAlphanumeric(this StringMaskingBuilder builder)
        {
            const string alphanumeric = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return builder.WhitelistChars(alphanumeric);
        }

        /// <summary>
        /// Convenience method to filter the string to only include digits (0-9).
        /// All non-digit characters are removed.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.PhoneNumber, m => m.WhitelistDigits());
        /// // "555-1234" becomes "5551234"
        /// // "(555) 123-4567" becomes "5551234567"
        /// </code>
        /// </example>
        public static StringMaskingBuilder WhitelistDigits(this StringMaskingBuilder builder)
        {
            return builder.WhitelistChars("0123456789");
        }

        /// <summary>
        /// Masks all digit characters (0-9) in the string with the specified mask character.
        /// All other characters remain unchanged.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.OrderNumber, m => m.MaskDigits());
        /// // "Order123" becomes "Order***"
        /// // "Code-42-ABC" becomes "Code-**-ABC"
        /// </code>
        /// </example>
        public static StringMaskingBuilder MaskDigits(this StringMaskingBuilder builder, string maskChar = "*")
        {
            return builder.AddRule(new MaskCharClassRule(CharClass.Digit, maskChar));
        }

        /// <summary>
        /// Masks all letter characters (A-Z, a-z) in the string with the specified mask character.
        /// All other characters remain unchanged.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.ProductCode, m => m.MaskLetters());
        /// // "ABC123XYZ" becomes "***123***"
        /// // "Test-456" becomes "****-456"
        /// </code>
        /// </example>
        public static StringMaskingBuilder MaskLetters(this StringMaskingBuilder builder, string maskChar = "*")
        {
            return builder.AddRule(new MaskCharClassRule(CharClass.Letter, maskChar));
        }

        /// <summary>
        /// Masks all whitespace characters (spaces, tabs, newlines) in the string with the specified mask character.
        /// All other characters remain unchanged.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="maskChar">Character to use for masking (default: "_")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.FullName, m => m.MaskWhitespace());
        /// // "John Doe" becomes "John_Doe"
        /// // "Hello   World" becomes "Hello___World"
        /// </code>
        /// </example>
        public static StringMaskingBuilder MaskWhitespace(this StringMaskingBuilder builder, string maskChar = "_")
        {
            return builder.AddRule(new MaskCharClassRule(CharClass.Whitespace, maskChar));
        }

        /// <summary>
        /// Masks all uppercase letter characters (A-Z) in the string with the specified mask character.
        /// All other characters remain unchanged.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.Code, m => m.MaskUppercase());
        /// // "HelloWorld" becomes "*ello*orld"
        /// // "ABC123abc" becomes "***123abc"
        /// </code>
        /// </example>
        public static StringMaskingBuilder MaskUppercase(this StringMaskingBuilder builder, string maskChar = "*")
        {
            return builder.AddRule(new MaskCharClassRule(CharClass.Upper, maskChar));
        }

        /// <summary>
        /// Masks all lowercase letter characters (a-z) in the string with the specified mask character.
        /// All other characters remain unchanged.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.Code, m => m.MaskLowercase());
        /// // "HelloWorld" becomes "H****W****"
        /// // "ABC123abc" becomes "ABC123***"
        /// </code>
        /// </example>
        public static StringMaskingBuilder MaskLowercase(this StringMaskingBuilder builder, string maskChar = "*")
        {
            return builder.AddRule(new MaskCharClassRule(CharClass.Lower, maskChar));
        }

        /// <summary>
        /// Masks all punctuation characters in the string with the specified mask character.
        /// All other characters remain unchanged.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.Text, m => m.MaskPunctuation());
        /// // "Hello, World!" becomes "Hello* World*"
        /// // "What?!?" becomes "What***"
        /// </code>
        /// </example>
        public static StringMaskingBuilder MaskPunctuation(this StringMaskingBuilder builder, string maskChar = "*")
        {
            return builder.AddRule(new MaskCharClassRule(CharClass.Punctuation, maskChar));
        }

        /// <summary>
        /// Masks all alphanumeric characters (letters and digits) in the string with the specified mask character.
        /// All other characters remain unchanged.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        /// masker.MaskFor(x => x.Text, m => m.MaskLettersOrDigits());
        /// // "Hello-123" becomes "*****-***"
        /// // "Test@456" becomes "****@***"
        /// </code>
        /// </example>
        public static StringMaskingBuilder MaskLettersOrDigits(this StringMaskingBuilder builder, string maskChar = "*")
        {
            return builder.AddRule(new MaskCharClassRule(CharClass.LetterOrDigit, maskChar));
        }

        /// <summary>
        /// Masks an email address with domain-aware strategies.
        /// Supports validation, plus addressing preservation, and multiple domain masking strategies.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="localKeep">Number of characters to keep unmasked in the local part (before @) (default: 1)</param>
        /// <param name="domainStrategy">Strategy for masking the domain part: "keep-root" (default), "keep-full", or "mask-all"</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <param name="validateFormat">Whether to validate email format before masking (default: true)</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <remarks>
        /// Domain strategies:
        /// - "keep-root": Shows only root domain (e.g., mail.example.com → example.com)
        /// - "keep-full": Keeps entire domain unchanged
        /// - "mask-all": Masks the domain as well
        /// Plus addressing (user+tag@domain.com) is preserved automatically.
        /// </remarks>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        ///
        /// // Keep root domain (default)
        /// masker.MaskFor(x => x.Email, m => m.EmailMask());
        /// // "user@mail.example.com" becomes "u***@example.com"
        ///
        /// // Keep full domain
        /// masker.MaskFor(x => x.Email, m => m.EmailMask(localKeep: 2, domainStrategy: "keep-full"));
        /// // "john.doe@company.com" becomes "jo******@company.com"
        ///
        /// // Mask everything including domain
        /// masker.MaskFor(x => x.Email, m => m.EmailMask(localKeep: 1, domainStrategy: "mask-all"));
        /// // "admin@example.com" becomes "a****@e******.com"
        ///
        /// // Plus addressing is preserved
        /// masker.MaskFor(x => x.Email, m => m.EmailMask());
        /// // "user+newsletter@example.com" becomes "u***+newsletter@example.com"
        /// </code>
        /// </example>
        public static StringMaskingBuilder EmailMask(
            this StringMaskingBuilder builder,
            int localKeep = 1,
            string domainStrategy = "keep-root",
            string maskChar = "*",
            bool validateFormat = true)
        {
            var strategy = domainStrategy.ToLowerInvariant() switch
            {
                "keep-root" => EmailDomainStrategy.KeepRoot,
                "keep-full" => EmailDomainStrategy.KeepFull,
                "mask-all" => EmailDomainStrategy.MaskAll,
                _ => throw new System.ArgumentException($"Unknown domain strategy: {domainStrategy}. Valid values are: keep-root, keep-full, mask-all", nameof(domainStrategy))
            };

            return builder.AddRule(new EmailMaskRule(localKeep, strategy, maskChar, validateFormat));
        }

        /// <summary>
        /// Masks phone numbers while preserving formatting and structure.
        /// Supports international formats (E.164), North American, and European phone numbers.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="keepLast">Number of digits to keep visible at the end (default: 2)</param>
        /// <param name="preserveSeparators">Whether to preserve formatting characters like spaces, dashes, parentheses (default: true)</param>
        /// <param name="countryHint">Optional country hint for ambiguous formats (not currently used)</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// // Preserve formatting (default)
        /// masker.MaskFor(x => x.Phone, m => m.PhoneMask(keepLast: 2));
        /// // "+45 12 34 56 78" becomes "+** ** ** ** 78"
        /// // "(555) 123-4567" becomes "(***) ***-**67"
        ///
        /// // Non-preserving mode (digits only)
        /// masker.MaskFor(x => x.Phone, m => m.PhoneMask(keepLast: 2, preserveSeparators: false));
        /// // "(555) 123-4567" becomes "********67"
        ///
        /// // Keep last 4 digits (PCI-DSS style)
        /// masker.MaskFor(x => x.Phone, m => m.PhoneMask(keepLast: 4));
        /// // "+1-555-123-4567" becomes "+*-***-***-4567"
        ///
        /// // Custom mask character
        /// masker.MaskFor(x => x.Phone, m => m.PhoneMask(keepLast: 3, maskChar: "X"));
        /// // "+1-555-1234" becomes "+X-XXX-X234"
        /// </code>
        /// </example>
        public static StringMaskingBuilder PhoneMask(
            this StringMaskingBuilder builder,
            int keepLast = 2,
            bool preserveSeparators = true,
            string countryHint = null,
            string maskChar = "*")
        {
            return builder.AddRule(new PhoneMaskRule(keepLast, preserveSeparators, countryHint, maskChar));
        }

        /// <summary>
        /// Masks credit card numbers with PCI-DSS compliance.
        /// By default, shows only the last 4 digits (PCI-DSS compliant).
        /// Optionally shows first 6 + last 4 digits (BIN + last 4).
        /// Preserves grouping characters (spaces, dashes) in the original format.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="keepFirst">Number of digits to keep visible at the start (default: 0)</param>
        /// <param name="keepLast">Number of digits to keep visible at the end (default: 4)</param>
        /// <param name="preserveGrouping">Whether to preserve spaces and dashes (default: true)</param>
        /// <param name="validateLuhn">Whether to validate card number using Luhn algorithm (default: false)</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <exception cref="System.ArgumentException">Thrown when keepFirst + keepLast exceeds 10 (PCI-DSS limit)</exception>
        /// <exception cref="System.FormatException">Thrown when validateLuhn is true and card number fails Luhn validation</exception>
        /// <example>
        /// <code>
        /// // Default: show last 4 digits only (PCI-DSS compliant)
        /// masker.MaskFor(x => x.CardNumber, m => m.CardMask());
        /// // "1234 5678 9012 3456" becomes "**** **** **** 3456"
        ///
        /// // Show BIN (first 6) + last 4 digits
        /// masker.MaskFor(x => x.CardNumber, m => m.CardMask(keepFirst: 6));
        /// // "1234 5678 9012 3456" becomes "1234 56** **** 3456"
        ///
        /// // With Luhn validation
        /// masker.MaskFor(x => x.CardNumber, m => m.CardMask(validateLuhn: true));
        /// // Throws FormatException if card number is invalid
        ///
        /// // Without grouping preservation
        /// masker.MaskFor(x => x.CardNumber, m => m.CardMask(preserveGrouping: false));
        /// // "1234 5678 9012 3456" becomes "************3456"
        /// </code>
        /// </example>
        public static StringMaskingBuilder CardMask(
            this StringMaskingBuilder builder,
            int keepFirst = 0,
            int keepLast = 4,
            bool preserveGrouping = true,
            bool validateLuhn = false,
            string maskChar = "*")
        {
            return builder.AddRule(new CardMaskRule(keepFirst, keepLast, preserveGrouping, validateLuhn, maskChar));
        }

        /// <summary>
        /// Applies cryptographic hashing for GDPR pseudonymization.
        /// Transforms data using industry-standard hash algorithms (SHA256, SHA512, MD5).
        /// Supports multiple salt modes and output formats.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="algorithm">The hash algorithm to use: "SHA256" (default), "SHA512", or "MD5"</param>
        /// <param name="saltMode">Salt generation mode: "static" (default, deterministic), "perRecord" (non-deterministic), or "perField" (field-specific)</param>
        /// <param name="outputFormat">Output format: "hex" (default), "base64", or "base64url"</param>
        /// <param name="staticSalt">Optional static salt as byte array (only used with saltMode="static")</param>
        /// <param name="fieldName">Field name for perField salt mode</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <remarks>
        /// <para>Salt modes:</para>
        /// <list type="bullet">
        /// <item><description><b>static:</b> Deterministic hashing - same input always produces same output (useful for lookups)</description></item>
        /// <item><description><b>perRecord:</b> Non-deterministic - each invocation produces different output (maximum privacy)</description></item>
        /// <item><description><b>perField:</b> Field-specific deterministic hashing - prevents cross-field correlation (requires fieldName parameter)</description></item>
        /// </list>
        /// <para>Uses RandomNumberGenerator for cryptographically secure salt generation.</para>
        /// <para>MD5 algorithm displays a warning as it is cryptographically broken.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        ///
        /// // Default: SHA256 with static salt, hex output (deterministic)
        /// masker.MaskFor(x => x.Email, m => m.Hash());
        /// // "user@example.com" becomes "a1b2c3d4..." (64 hex chars)
        ///
        /// // SHA512 with per-record salt (non-deterministic)
        /// masker.MaskFor(x => x.SSN, m => m.Hash(algorithm: "SHA512", saltMode: "perRecord"));
        /// // "123-45-6789" becomes different hash each time
        ///
        /// // SHA256 with per-field salt and base64 output
        /// masker.MaskFor(x => x.UserId, m => m.Hash(saltMode: "perField", outputFormat: "base64", fieldName: "UserId"));
        /// // "USER123" becomes Base64-encoded hash specific to UserId field
        ///
        /// // Base64Url format (URL-safe)
        /// masker.MaskFor(x => x.Token, m => m.Hash(outputFormat: "base64url"));
        /// // "secret-token" becomes URL-safe base64 string
        /// </code>
        /// </example>
        public static StringMaskingBuilder Hash(
            this StringMaskingBuilder builder,
            string algorithm = "SHA256",
            string saltMode = "static",
            string outputFormat = "hex",
            byte[] staticSalt = null,
            string fieldName = null)
        {
            var algo = System.Enum.Parse<HashAlgorithmType>(algorithm, ignoreCase: true);
            var mode = System.Enum.Parse<SaltMode>(saltMode, ignoreCase: true);
            var format = System.Enum.Parse<OutputFormat>(outputFormat, ignoreCase: true);

            return builder.AddRule(new HashRule(algo, mode, format, staticSalt, fieldName));
        }

        /// <summary>
        /// Masks International Bank Account Numbers (IBAN) while preserving format validity.
        /// Validates IBAN using ISO 13616 standard (length by country code) and ISO 7064 mod-97 checksum.
        /// Always preserves country code and check digits (first 4 characters) while masking account details.
        /// Supports 60+ countries and optionally preserves space grouping in the output.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="keepLast">Number of trailing characters to keep visible (default: 4)</param>
        /// <param name="preserveGrouping">Whether to preserve space grouping in 4-character blocks (default: true)</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <remarks>
        /// <para>
        /// This rule performs comprehensive IBAN validation:
        /// <list type="bullet">
        /// <item><description>Format validation: country code (2 letters) + check digits (2 digits) + account number</description></item>
        /// <item><description>Length validation: verifies length matches ISO 13616 standard for the country code</description></item>
        /// <item><description>Checksum validation: applies mod-97 algorithm per ISO 7064 standard</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Invalid IBANs are returned unchanged (graceful degradation), making it safe to apply to potentially invalid data.
        /// Supports 60+ countries including all EU member states, UK, Switzerland, Norway, and more.
        /// </para>
        /// <para>
        /// When preserveGrouping is true, the output maintains 4-character space-separated groups.
        /// If the input has spaces, they are preserved in the output format.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var masker = new BankAccountMasker();
        ///
        /// // Default: show last 4 digits, preserve grouping
        /// masker.MaskFor(x => x.IBAN, m => m.IBANMask());
        /// // "DE89370400440532013000" becomes "DE89**************3000"
        /// // "DE89 3704 0044 0532 0130 00" becomes "DE89 **** **** **** **30 00"
        ///
        /// // Show last 6 digits
        /// masker.MaskFor(x => x.IBAN, m => m.IBANMask(keepLast: 6));
        /// // "GB82WEST12345698765432" becomes "GB82************765432"
        ///
        /// // No grouping preservation
        /// masker.MaskFor(x => x.IBAN, m => m.IBANMask(preserveGrouping: false));
        /// // "DE89 3704 0044 0532 0130 00" becomes "DE89**************3000"
        ///
        /// // Custom mask character
        /// masker.MaskFor(x => x.IBAN, m => m.IBANMask(keepLast: 4, maskChar: '#'));
        /// // "FR1420041010050500013M02606" becomes "FR14###################2606"
        ///
        /// // Invalid IBANs return unchanged (graceful degradation)
        /// masker.MaskFor(x => x.IBAN, m => m.IBANMask());
        /// // "INVALID123" remains "INVALID123"
        ///
        /// // Supported countries (examples):
        /// // - Germany (DE): 22 chars
        /// // - France (FR): 27 chars
        /// // - United Kingdom (GB): 22 chars
        /// // - Spain (ES): 24 chars
        /// // - Italy (IT): 27 chars
        /// // - Netherlands (NL): 18 chars
        /// // - Switzerland (CH): 21 chars
        /// // - Norway (NO): 15 chars (shortest)
        /// // - Malta (MT): 31 chars (longest)
        /// // ... and 50+ more countries
        /// </code>
        /// </example>
        public static StringMaskingBuilder IBANMask(
            this StringMaskingBuilder builder,
            int keepLast = 4,
            bool preserveGrouping = true,
            char maskChar = '*')
        {
            return builder.AddRule(new IBANMaskRule(keepLast, preserveGrouping, maskChar));
        }

        /// <summary>
        /// Masks national identification numbers (SSN, Tax ID, etc.) for 100+ countries with format validation.
        /// Supports automatic format detection and country-specific patterns with checksum hints.
        /// Preserves separators like dashes and spaces in the original format.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="countryCode">ISO 3166-1 alpha-2 country code (e.g., "US", "UK", "DE"). If null, auto-detection is attempted. (default: "US")</param>
        /// <param name="keepFirst">Override number of characters to keep visible at the start. If null, uses pattern default.</param>
        /// <param name="keepLast">Override number of characters to keep visible at the end. If null, uses pattern default.</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <remarks>
        /// <para>Supports 100+ country formats including:</para>
        /// <list type="bullet">
        /// <item><description><b>European Union:</b> All 27 member states with their specific formats (e.g., AT, BE, BG, HR, CY, CZ, DK, EE, FI, FR, DE, GR, HU, IE, IT, LV, LT, LU, MT, NL, PL, PT, RO, SK, SI, ES, SE)</description></item>
        /// <item><description><b>Americas:</b> US, CA, MX, BR, AR, CL, CO, PE, UY, EC, BO, VE</description></item>
        /// <item><description><b>Asia-Pacific:</b> AU, NZ, JP, CN, KR, IN, SG, HK, TW, MY, TH, VN, IDN, PH</description></item>
        /// <item><description><b>Europe (non-EU):</b> UK, CH, NO, IS, RU, TR, UA</description></item>
        /// <item><description><b>Middle East & Africa:</b> IL, SA, AE, EG, MA, ZA, NG, KE, GH, PK</description></item>
        /// </list>
        /// <para>Each country pattern includes regex validation and comments about checksum algorithms where applicable.</para>
        /// <para>Automatic format detection attempts to match input against all known patterns if country code is null.</para>
        /// <para>Invalid formats are returned unchanged (graceful degradation).</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var masker = new PersonMasker();
        ///
        /// // US SSN (default)
        /// masker.MaskFor(x => x.SSN, m => m.NationalIdMask());
        /// // "123-45-6789" becomes "***-**-6789" (keepFirst=0, keepLast=4)
        ///
        /// // UK National Insurance Number
        /// masker.MaskFor(x => x.NINO, m => m.NationalIdMask("UK"));
        /// // "AB123456C" becomes "AB******C" (keepFirst=2, keepLast=1)
        ///
        /// // Germany with custom masking
        /// masker.MaskFor(x => x.TaxId, m => m.NationalIdMask("DE", keepFirst: 3, keepLast: 3));
        /// // "12345678901" becomes "123*****901"
        ///
        /// // Canada SIN
        /// masker.MaskFor(x => x.SIN, m => m.NationalIdMask("CA"));
        /// // "123-456-789" becomes "***-***-789"
        ///
        /// // Brazil CPF
        /// masker.MaskFor(x => x.CPF, m => m.NationalIdMask("BR"));
        /// // "123.456.789-01" becomes "***.***-01" (keepLast=4)
        ///
        /// // Auto-detect format (when country is unknown)
        /// masker.MaskFor(x => x.NationalId, m => m.NationalIdMask(countryCode: null));
        /// // "123-45-6789" becomes "***-**-6789" (auto-detected as US SSN)
        ///
        /// // China Resident ID
        /// masker.MaskFor(x => x.NationalId, m => m.NationalIdMask("CN"));
        /// // "11010119900307123X" becomes "**************123X"
        ///
        /// // India Aadhaar
        /// masker.MaskFor(x => x.Aadhaar, m => m.NationalIdMask("IN"));
        /// // "1234 5678 9012" becomes "**** **** 9012"
        ///
        /// // Invalid format returns unchanged
        /// masker.MaskFor(x => x.Id, m => m.NationalIdMask("US"));
        /// // "INVALID" remains "INVALID"
        ///
        /// // Unformatted variants (where common)
        /// masker.MaskFor(x => x.SSN, m => m.NationalIdMask("US_UNFORMATTED"));
        /// // "123456789" becomes "*****6789"
        /// </code>
        /// </example>
        public static StringMaskingBuilder NationalIdMask(
            this StringMaskingBuilder builder,
            string countryCode = "US",
            int? keepFirst = null,
            int? keepLast = null,
            string maskChar = "*")
        {
            return builder.AddRule(new NationalIdMaskRule(countryCode, keepFirst, keepLast, maskChar));
        }
    }
}
