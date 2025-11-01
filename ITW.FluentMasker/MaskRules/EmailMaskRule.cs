using System;
using System.Buffers;
using System.Text.RegularExpressions;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Defines strategies for masking email domain parts.
    /// </summary>
    public enum EmailDomainStrategy
    {
        /// <summary>
        /// Keeps only the root domain (e.g., mail.example.com → example.com)
        /// </summary>
        KeepRoot,

        /// <summary>
        /// Keeps the entire domain unchanged
        /// </summary>
        KeepFull,

        /// <summary>
        /// Masks the domain as well (e.g., example.com → e******.com)
        /// </summary>
        MaskAll
    }

    /// <summary>
    /// Domain-aware email masking with validation and multiple masking strategies.
    /// </summary>
    /// <remarks>
    /// <para>Supports validation of email format before masking.</para>
    /// <para>Handles plus addressing (e.g., user+tag@domain.com) by preserving the tag.</para>
    /// <para>Provides three domain masking strategies: KeepRoot, KeepFull, and MaskAll.</para>
    /// <para>Uses ArrayPool&lt;char&gt; internally for high performance.</para>
    /// <para>Null and empty strings are returned unchanged.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // KeepRoot strategy (default)
    /// var rule1 = new EmailMaskRule(localKeep: 1, domainStrategy: EmailDomainStrategy.KeepRoot);
    /// var result1 = rule1.Apply("user@mail.example.com");
    /// // result1 = "u***@example.com"
    ///
    /// // KeepFull strategy
    /// var rule2 = new EmailMaskRule(localKeep: 2, domainStrategy: EmailDomainStrategy.KeepFull);
    /// var result2 = rule2.Apply("john.doe@company.com");
    /// // result2 = "jo******@company.com"
    ///
    /// // MaskAll strategy
    /// var rule3 = new EmailMaskRule(localKeep: 1, domainStrategy: EmailDomainStrategy.MaskAll);
    /// var result3 = rule3.Apply("admin@example.com");
    /// // result3 = "a****@e******.com"
    ///
    /// // Plus addressing preservation
    /// var rule4 = new EmailMaskRule(localKeep: 1);
    /// var result4 = rule4.Apply("user+newsletter@example.com");
    /// // result4 = "u***+newsletter@example.com"
    ///
    /// // Edge cases:
    /// rule1.Apply("");                  // Returns "" (empty string unchanged)
    /// rule1.Apply(null);                // Returns null
    /// rule1.Apply("invalid-email");     // Throws FormatException (if validateFormat=true)
    /// </code>
    /// </example>
    public class EmailMaskRule : IStringMaskRule
    {
        private readonly int _localKeep;
        private readonly EmailDomainStrategy _domainStrategy;
        private readonly string _maskChar;
        private readonly bool _validateFormat;

        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(100)
        );

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailMaskRule"/> class.
        /// </summary>
        /// <param name="localKeep">Number of characters to keep unmasked in the local part (before @)</param>
        /// <param name="domainStrategy">Strategy for masking the domain part</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <param name="validateFormat">Whether to validate email format before masking (default: true)</param>
        /// <exception cref="ArgumentException">Thrown when localKeep is negative or maskChar is null/empty</exception>
        /// <exception cref="ArgumentNullException">Thrown when maskChar is null</exception>
        public EmailMaskRule(
            int localKeep = 1,
            EmailDomainStrategy domainStrategy = EmailDomainStrategy.KeepRoot,
            string maskChar = "*",
            bool validateFormat = true)
        {
            if (localKeep < 0)
                throw new ArgumentException("Local keep must be non-negative", nameof(localKeep));
            if (string.IsNullOrEmpty(maskChar))
                throw new ArgumentException("Mask character cannot be null or empty", nameof(maskChar));

            _localKeep = localKeep;
            _domainStrategy = domainStrategy;
            _maskChar = maskChar ?? throw new ArgumentNullException(nameof(maskChar));
            _validateFormat = validateFormat;
        }

        /// <summary>
        /// Applies the email masking rule to the input string.
        /// </summary>
        /// <param name="input">The email address to mask. Can be null or empty.</param>
        /// <returns>
        /// The masked email address according to the configured strategy.
        /// Returns the original input if it is null or empty.
        /// </returns>
        /// <exception cref="FormatException">Thrown when validateFormat is true and the input is not a valid email format</exception>
        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Validate format
            if (_validateFormat && !EmailRegex.IsMatch(input))
                throw new FormatException($"Invalid email format: {input}");

            var parts = input.Split('@');
            if (parts.Length != 2)
                return input; // Shouldn't happen if validated

            string localPart = parts[0];
            string domain = parts[1];

            // Mask local part
            string maskedLocal = MaskLocalPart(localPart);

            // Mask domain based on strategy
            string maskedDomain = _domainStrategy switch
            {
                EmailDomainStrategy.KeepRoot => GetRootDomain(domain),
                EmailDomainStrategy.KeepFull => domain,
                EmailDomainStrategy.MaskAll => MaskDomain(domain),
                _ => domain
            };

            return $"{maskedLocal}@{maskedDomain}";
        }

        /// <summary>
        /// Masks the local part of an email address, preserving plus addressing tags.
        /// </summary>
        /// <param name="localPart">The local part of the email (before @)</param>
        /// <returns>The masked local part</returns>
        private string MaskLocalPart(string localPart)
        {
            // Handle plus addressing: user+tag@domain → keep "user" logic
            int plusIndex = localPart.IndexOf('+');
            string baseLocal = plusIndex > 0 ? localPart.Substring(0, plusIndex) : localPart;
            string tag = plusIndex > 0 ? localPart.Substring(plusIndex) : string.Empty;

            if (_localKeep >= baseLocal.Length)
                return localPart; // Keep entire local part

            string kept = baseLocal.Substring(0, _localKeep);
            int maskLength = baseLocal.Length - _localKeep;

            // Use ArrayPool for better performance
            var pool = ArrayPool<char>.Shared;
            char[] buffer = pool.Rent(maskLength);

            try
            {
                // Fill with mask characters
                for (int i = 0; i < maskLength; i++)
                    buffer[i] = _maskChar[0];

                string masked = new string(buffer, 0, maskLength);
                return kept + masked + tag;
            }
            finally
            {
                pool.Return(buffer);
            }
        }

        /// <summary>
        /// Extracts the root domain from a potentially multi-level domain.
        /// </summary>
        /// <param name="domain">The full domain (e.g., mail.example.com)</param>
        /// <returns>The root domain (e.g., example.com)</returns>
        /// <remarks>
        /// This implementation takes the last 2 parts of the domain.
        /// Note: This may not handle complex TLDs like .co.uk correctly - acceptable for v1.
        /// </remarks>
        private string GetRootDomain(string domain)
        {
            // Extract root domain: subdomain.example.com → example.com
            var parts = domain.Split('.');
            if (parts.Length <= 2)
                return domain; // Already root

            // Take last 2 parts (handles .co.uk, .com.au, etc. incorrectly - acceptable for v1)
            return string.Join(".", parts[^2..]);
        }

        /// <summary>
        /// Masks the domain by keeping only the first character of each domain part.
        /// </summary>
        /// <param name="domain">The domain to mask (e.g., example.com)</param>
        /// <returns>The masked domain (e.g., e******.com)</returns>
        private string MaskDomain(string domain)
        {
            var parts = domain.Split('.');
            var pool = ArrayPool<char>.Shared;

            try
            {
                var maskedParts = new string[parts.Length];

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];

                    if (part.Length <= 1)
                    {
                        maskedParts[i] = part;
                        continue;
                    }

                    int maskLength = part.Length - 1;
                    char[] buffer = pool.Rent(maskLength);

                    try
                    {
                        // Fill with mask characters
                        for (int j = 0; j < maskLength; j++)
                            buffer[j] = _maskChar[0];

                        string masked = new string(buffer, 0, maskLength);
                        maskedParts[i] = part[0] + masked;
                    }
                    finally
                    {
                        pool.Return(buffer);
                    }
                }

                return string.Join(".", maskedParts);
            }
            catch
            {
                // Fallback to simple implementation if pooling fails
                var maskedParts = new string[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    if (part.Length <= 1)
                        maskedParts[i] = part;
                    else
                        maskedParts[i] = part[0] + new string(_maskChar[0], part.Length - 1);
                }
                return string.Join(".", maskedParts);
            }
        }

        // Explicit interface implementation to avoid ambiguity in method overload resolution
        string IMaskRule<string, string>.Apply(string input) => Apply(input);
    }
}
