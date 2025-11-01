using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Masks International Bank Account Numbers (IBAN) while preserving format validity.
    /// Preserves country code and check digits while masking account details.
    /// Implements ISO 13616 standard for IBAN validation.
    /// </summary>
    /// <example>
    /// <code>
    /// var rule = new IBANMaskRule(keepLast: 4);
    /// string masked = rule.Apply("DE89370400440532013000");
    /// // Returns: "DE89************3000"
    ///
    /// // With grouping preserved:
    /// string masked = rule.Apply("DE89 3704 0044 0532 0130 00");
    /// // Returns: "DE89 **** **** **** **** 3000"
    /// </code>
    /// </example>
    public class IBANMaskRule : IStringMaskRule
    {
        private readonly int _keepLast;
        private readonly bool _preserveGrouping;
        private readonly char _maskChar;

        /// <summary>
        /// IBAN length by country code (ISO 13616).
        /// Maps country codes to their expected IBAN lengths.
        /// </summary>
        private static readonly Dictionary<string, int> IbanLengths = new()
        {
            ["AD"] = 24, ["AE"] = 23, ["AL"] = 28, ["AT"] = 20, ["AZ"] = 28,
            ["BA"] = 20, ["BE"] = 16, ["BG"] = 22, ["BH"] = 22, ["BR"] = 29,
            ["BY"] = 28, ["CH"] = 21, ["CR"] = 22, ["CY"] = 28, ["CZ"] = 24,
            ["DE"] = 22, ["DK"] = 18, ["DO"] = 28, ["EE"] = 20, ["EG"] = 29,
            ["ES"] = 24, ["FI"] = 18, ["FO"] = 18, ["FR"] = 27, ["GB"] = 22,
            ["GE"] = 22, ["GI"] = 23, ["GL"] = 18, ["GR"] = 27, ["GT"] = 28,
            ["HR"] = 21, ["HU"] = 28, ["IE"] = 22, ["IL"] = 23, ["IS"] = 26,
            ["IT"] = 27, ["JO"] = 30, ["KW"] = 30, ["KZ"] = 20, ["LB"] = 28,
            ["LC"] = 32, ["LI"] = 21, ["LT"] = 20, ["LU"] = 20, ["LV"] = 21,
            ["MC"] = 27, ["MD"] = 24, ["ME"] = 22, ["MK"] = 19, ["MR"] = 27,
            ["MT"] = 31, ["MU"] = 30, ["NL"] = 18, ["NO"] = 15, ["PK"] = 24,
            ["PL"] = 28, ["PS"] = 29, ["PT"] = 25, ["QA"] = 29, ["RO"] = 24,
            ["RS"] = 22, ["SA"] = 24, ["SE"] = 24, ["SI"] = 19, ["SK"] = 24,
            ["SM"] = 27, ["TN"] = 24, ["TR"] = 26, ["UA"] = 29, ["VA"] = 22,
            ["VG"] = 24, ["XK"] = 20
        };

        /// <summary>
        /// Creates a new instance of the IBANMaskRule.
        /// </summary>
        /// <param name="keepLast">Number of trailing characters to keep visible (default: 4)</param>
        /// <param name="preserveGrouping">Whether to preserve space grouping in the output (default: true)</param>
        /// <param name="maskChar">Character to use for masking (default: '*')</param>
        public IBANMaskRule(int keepLast = 4, bool preserveGrouping = true, char maskChar = '*')
        {
            if (keepLast < 0)
                throw new ArgumentException("keepLast must be non-negative", nameof(keepLast));

            _keepLast = keepLast;
            _preserveGrouping = preserveGrouping;
            _maskChar = maskChar;
        }

        /// <summary>
        /// Applies the IBAN masking rule to the input string.
        /// </summary>
        /// <param name="input">The IBAN to mask</param>
        /// <returns>Masked IBAN with country code and check digits visible, or original input if invalid</returns>
        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Check if input contains spaces (for grouping preservation)
            bool hasGrouping = input.Contains(' ');

            // Remove spaces and normalize
            string normalized = input.Replace(" ", "").ToUpperInvariant();

            // Validate IBAN format and checksum
            if (!IsValidIban(normalized))
                return input; // Invalid IBAN, return unchanged for graceful degradation

            // Mask: Keep country code + check digits (first 4 chars) + keepLast chars
            // e.g., DE89370400440532013000 → DE89************3000
            int maskStart = 4; // After country code (2 chars) + check digits (2 chars)
            int maskEnd = normalized.Length - _keepLast;

            if (maskEnd <= maskStart)
                return input; // Too short to mask meaningfully

            char[] masked = normalized.ToCharArray();
            for (int i = maskStart; i < maskEnd; i++)
            {
                masked[i] = _maskChar;
            }

            string result = new string(masked);

            // Re-apply grouping if requested and input had grouping
            if (_preserveGrouping && hasGrouping)
            {
                return FormatIbanWithSpaces(result);
            }

            return result;
        }

        /// <summary>
        /// Validates IBAN format and checksum using ISO 13616 and ISO 7064 standards.
        /// </summary>
        /// <param name="iban">The normalized IBAN string (no spaces, uppercase)</param>
        /// <returns>True if the IBAN is valid, false otherwise</returns>
        private bool IsValidIban(string iban)
        {
            // Must be at least 15 characters (shortest IBAN is NO with 15 chars)
            if (iban.Length < 15 || iban.Length > 34)
                return false;

            // First two characters must be letters (country code)
            if (!char.IsLetter(iban[0]) || !char.IsLetter(iban[1]))
                return false;

            // Next two characters must be digits (check digits)
            if (!char.IsDigit(iban[2]) || !char.IsDigit(iban[3]))
                return false;

            // Validate length for country code
            string countryCode = iban.Substring(0, 2);
            if (IbanLengths.TryGetValue(countryCode, out int expectedLength))
            {
                if (iban.Length != expectedLength)
                    return false;
            }
            else
            {
                // Unknown country code - still validate format but allow any reasonable length
                // This provides forward compatibility for new country codes
            }

            // Remaining characters must be alphanumeric
            for (int i = 4; i < iban.Length; i++)
            {
                if (!char.IsLetterOrDigit(iban[i]))
                    return false;
            }

            // Validate checksum using mod-97 algorithm
            return ValidateIbanChecksum(iban);
        }

        /// <summary>
        /// Validates IBAN checksum using mod-97 algorithm (ISO 7064).
        /// Algorithm:
        /// 1. Move first 4 characters to end
        /// 2. Replace letters with numbers (A=10, B=11, ..., Z=35)
        /// 3. Calculate mod 97 of the resulting number
        /// 4. Valid IBAN has remainder of 1
        /// </summary>
        /// <param name="iban">The normalized IBAN string</param>
        /// <returns>True if checksum is valid, false otherwise</returns>
        private bool ValidateIbanChecksum(string iban)
        {
            // Move first 4 characters to end
            string rearranged = iban.Substring(4) + iban.Substring(0, 4);

            // Replace letters with numbers (A=10, B=11, ..., Z=35)
            var numericString = new StringBuilder(rearranged.Length * 2);
            foreach (char c in rearranged)
            {
                if (char.IsDigit(c))
                {
                    numericString.Append(c);
                }
                else if (char.IsLetter(c))
                {
                    // A=10, B=11, ..., Z=35
                    numericString.Append((int)c - (int)'A' + 10);
                }
            }

            // Calculate mod 97 using chunked processing to avoid overflow
            string digits = numericString.ToString();
            int remainder = 0;

            // Process in chunks to avoid integer overflow
            foreach (char digit in digits)
            {
                remainder = (remainder * 10 + (digit - '0')) % 97;
            }

            // Valid IBAN has remainder of 1
            return remainder == 1;
        }

        /// <summary>
        /// Formats IBAN with spaces in groups of 4 characters.
        /// </summary>
        /// <param name="iban">The IBAN string without spaces</param>
        /// <returns>IBAN formatted with space separators</returns>
        private string FormatIbanWithSpaces(string iban)
        {
            var result = new StringBuilder(iban.Length + (iban.Length / 4));
            for (int i = 0; i < iban.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                    result.Append(' ');
                result.Append(iban[i]);
            }
            return result.ToString();
        }
    }
}
