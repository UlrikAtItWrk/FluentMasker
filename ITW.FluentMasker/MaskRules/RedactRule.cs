using System;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Replaces any input value with a configurable redaction placeholder text.
    /// </summary>
    /// <remarks>
    /// <para>The input value is completely replaced with the redaction text.</para>
    /// <para>Original value length is not preserved.</para>
    /// <para>Useful for standardized redaction markers in logs, reports, or data exports.</para>
    /// <para>Common redaction texts include "[REDACTED]", "[CLASSIFIED]", "███████", or custom markers.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Default redaction text:
    /// var rule = new RedactRule();
    /// rule.Apply("SensitiveData");  // Returns "[REDACTED]"
    ///
    /// // Custom redaction text:
    /// var classified = new RedactRule("[CLASSIFIED]");
    /// classified.Apply("TopSecret");  // Returns "[CLASSIFIED]"
    ///
    /// // Visual redaction:
    /// var visual = new RedactRule("███████");
    /// visual.Apply("Password123");  // Returns "███████"
    ///
    /// // Works with any input:
    /// rule.Apply("AnyValue");       // Returns "[REDACTED]"
    /// rule.Apply("");               // Returns "[REDACTED]"
    /// </code>
    /// </example>
    public class RedactRule : IStringMaskRule
    {
        private readonly string _redactionText;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedactRule"/> class.
        /// </summary>
        /// <param name="redactionText">The text to replace the input with (default: "[REDACTED]")</param>
        /// <exception cref="ArgumentNullException">Thrown when redactionText is null</exception>
        public RedactRule(string redactionText = "[REDACTED]")
        {
            _redactionText = redactionText ?? throw new ArgumentNullException(nameof(redactionText));
        }

        /// <summary>
        /// Applies the redaction rule to the input string.
        /// </summary>
        /// <param name="input">The string to redact (ignored)</param>
        /// <returns>The configured redaction text</returns>
        public string Apply(string input)
        {
            return _redactionText;
        }
    }
}
