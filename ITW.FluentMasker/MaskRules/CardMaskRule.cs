using System;
using System.Buffers;
using System.Linq;
using System.Text;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Credit card masking with PCI-DSS compliance.
    /// </summary>
    /// <remarks>
    /// <para>Masks credit card numbers while maintaining PCI-DSS compliance (max 10 digits visible).</para>
    /// <para>Preserves grouping characters (spaces, dashes) in the original format.</para>
    /// <para>Optionally validates card numbers using the Luhn algorithm.</para>
    /// <para>Uses ArrayPool&lt;char&gt; internally for high performance.</para>
    /// <para>Null and empty strings are returned unchanged.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Default: show last 4 digits (PCI-DSS compliant)
    /// var rule1 = new CardMaskRule();
    /// var result1 = rule1.Apply("1234 5678 9012 3456");
    /// // result1 = "**** **** **** 3456"
    ///
    /// // Show first 6 + last 4 (BIN + last 4)
    /// var rule2 = new CardMaskRule(keepFirst: 6, keepLast: 4);
    /// var result2 = rule2.Apply("1234-5678-9012-3456");
    /// // result2 = "1234-56** -****- 3456"
    ///
    /// // With Luhn validation
    /// var rule3 = new CardMaskRule(validateLuhn: true);
    /// var result3 = rule3.Apply("4532015112830366"); // Valid Visa
    /// // result3 = "************0366"
    ///
    /// // Without grouping preservation
    /// var rule4 = new CardMaskRule(preserveGrouping: false);
    /// var result4 = rule4.Apply("1234 5678 9012 3456");
    /// // result4 = "************3456"
    ///
    /// // Edge cases:
    /// rule1.Apply("");                       // Returns "" (empty string unchanged)
    /// rule1.Apply(null);                     // Returns null
    /// rule3.Apply("1234567890123456");       // Throws FormatException (invalid Luhn)
    /// </code>
    /// </example>
    public class CardMaskRule : IStringMaskRule
    {
        private readonly int _keepFirst;
        private readonly int _keepLast;
        private readonly bool _preserveGrouping;
        private readonly bool _validateLuhn;
        private readonly string _maskChar;

        /// <summary>
        /// Initializes a new instance of the <see cref="CardMaskRule"/> class.
        /// </summary>
        /// <param name="keepFirst">Number of digits to keep unmasked at the start (default: 0)</param>
        /// <param name="keepLast">Number of digits to keep unmasked at the end (default: 4)</param>
        /// <param name="preserveGrouping">Whether to preserve spaces and dashes (default: true)</param>
        /// <param name="validateLuhn">Whether to validate using Luhn algorithm (default: false)</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <exception cref="ArgumentException">Thrown when keepFirst or keepLast are negative, or total exceeds PCI-DSS limit</exception>
        /// <exception cref="ArgumentNullException">Thrown when maskChar is null</exception>
        public CardMaskRule(
            int keepFirst = 0,
            int keepLast = 4,
            bool preserveGrouping = true,
            bool validateLuhn = false,
            string maskChar = "*")
        {
            if (keepFirst < 0)
                throw new ArgumentException("Keep first must be non-negative", nameof(keepFirst));
            if (keepLast < 0)
                throw new ArgumentException("Keep last must be non-negative", nameof(keepLast));
            if (keepFirst + keepLast > 10)
                throw new ArgumentException("Total visible digits (keepFirst + keepLast) must not exceed 10 (PCI-DSS limit)", nameof(keepFirst));
            if (string.IsNullOrEmpty(maskChar))
                throw new ArgumentException("Mask character cannot be null or empty", nameof(maskChar));

            _keepFirst = keepFirst;
            _keepLast = keepLast;
            _preserveGrouping = preserveGrouping;
            _validateLuhn = validateLuhn;
            _maskChar = maskChar ?? throw new ArgumentNullException(nameof(maskChar));
        }

        /// <summary>
        /// Applies the card masking rule to the input string.
        /// </summary>
        /// <param name="input">The card number to mask. Can be null or empty.</param>
        /// <returns>
        /// The masked card number according to the configured strategy.
        /// Returns the original input if it is null or empty.
        /// </returns>
        /// <exception cref="FormatException">Thrown when validateLuhn is true and the card number fails Luhn validation</exception>
        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Extract digits only
            string digits = new string(input.Where(char.IsDigit).ToArray());

            // Return input unchanged if no digits found
            if (digits.Length == 0)
                return input;

            // Validate Luhn if requested
            if (_validateLuhn && !IsValidLuhn(digits))
                throw new FormatException($"Invalid card number (Luhn check failed)");

            // Mask the digits
            string maskedDigits = MaskCardDigits(digits);

            // Preserve or remove grouping
            if (_preserveGrouping)
                return PreserveGrouping(input, maskedDigits);
            else
                return maskedDigits;
        }

        /// <summary>
        /// Masks the card digits according to keepFirst and keepLast parameters.
        /// </summary>
        /// <param name="digits">The digit-only card number</param>
        /// <returns>The masked digits</returns>
        private string MaskCardDigits(string digits)
        {
            // If the card is too short to mask, return as-is
            if (_keepFirst + _keepLast >= digits.Length)
                return digits;

            int maskCount = digits.Length - _keepFirst - _keepLast;

            // Use ArrayPool for better performance
            var pool = ArrayPool<char>.Shared;
            char[] buffer = pool.Rent(digits.Length);

            try
            {
                // Copy first kept digits
                for (int i = 0; i < _keepFirst; i++)
                    buffer[i] = digits[i];

                // Fill middle with mask characters
                for (int i = _keepFirst; i < _keepFirst + maskCount; i++)
                    buffer[i] = _maskChar[0];

                // Copy last kept digits
                for (int i = 0; i < _keepLast; i++)
                    buffer[_keepFirst + maskCount + i] = digits[_keepFirst + maskCount + i];

                return new string(buffer, 0, digits.Length);
            }
            finally
            {
                pool.Return(buffer);
            }
        }

        /// <summary>
        /// Preserves the original grouping (spaces, dashes) while applying masked digits.
        /// </summary>
        /// <param name="original">The original input with grouping characters</param>
        /// <param name="maskedDigits">The masked digits (without grouping)</param>
        /// <returns>The masked card number with original grouping preserved</returns>
        private string PreserveGrouping(string original, string maskedDigits)
        {
            var result = new StringBuilder(original.Length);
            int digitIndex = 0;

            foreach (char c in original)
            {
                if (char.IsDigit(c))
                {
                    result.Append(maskedDigits[digitIndex++]);
                }
                else
                {
                    // Keep separator (space, dash, etc.)
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Validates a card number using the Luhn algorithm (mod 10 checksum).
        /// </summary>
        /// <param name="cardNumber">The digit-only card number to validate</param>
        /// <returns>True if the card number passes Luhn validation, false otherwise</returns>
        /// <remarks>
        /// The Luhn algorithm is a simple checksum formula used to validate credit card numbers.
        /// It works by doubling every second digit from right to left, summing the digits,
        /// and checking if the total is divisible by 10.
        /// </remarks>
        private bool IsValidLuhn(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber))
                return false;

            int sum = 0;
            bool alternate = false;

            // Process digits from right to left
            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int digit = cardNumber[i] - '0';

                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }

        // Explicit interface implementation to avoid ambiguity in method overload resolution
        string IMaskRule<string, string>.Apply(string input) => Apply(input);
    }
}
