using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Implements declarative template-based masking with support for complex patterns.
    /// Templates use {{token}} syntax to define masking operations.
    /// </summary>
    /// <remarks>
    /// Supported tokens:
    /// - {{F}} or {{F|n}}: First n characters (default 1)
    /// - {{L}} or {{L|n}}: Last n characters (default 1)
    /// - {{*xN}}: N masked characters (e.g., {{*x5}} = "*****")
    /// - {{digits}} or {{digits|start-end}}: Extract digits with optional range
    /// - {{letters}} or {{letters|start-end}}: Extract letters with optional range
    /// </remarks>
    /// <example>
    /// var rule = new TemplateMaskRule("{{F}}{{*x6}}{{L}}");
    /// rule.Apply("JohnDoe"); // Returns "J******e"
    ///
    /// var phoneRule = new TemplateMaskRule("+{{digits|0-2}} ** ** {{digits|-2}}");
    /// phoneRule.Apply("+45 12 34 56 78"); // Returns "+45 ** ** 78"
    /// </example>
    public class TemplateMaskRule : IStringMaskRule
    {
        private readonly string _template;
        private readonly Regex _tokenPattern = new Regex(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateMaskRule"/> class.
        /// </summary>
        /// <param name="template">The template string with {{token}} placeholders</param>
        /// <exception cref="ArgumentNullException">Thrown when template is null</exception>
        public TemplateMaskRule(string template)
        {
            _template = template ?? throw new ArgumentNullException(nameof(template));
        }

        /// <summary>
        /// Applies the template mask rule to the input string.
        /// </summary>
        /// <param name="input">The string to mask</param>
        /// <returns>The masked string based on the template</returns>
        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return _tokenPattern.Replace(_template, match =>
            {
                string token = match.Groups[1].Value;
                return ProcessToken(token, input);
            });
        }

        /// <summary>
        /// Processes a single token from the template.
        /// </summary>
        /// <param name="token">The token to process (without the {{ }} delimiters)</param>
        /// <param name="input">The input string to extract data from</param>
        /// <returns>The processed token value</returns>
        private string ProcessToken(string token, string input)
        {
            // Parse token (e.g., "F|2", "*x5", "digits|0-3")
            var parts = token.Split('|');
            string command = parts[0];
            string args = parts.Length > 1 ? parts[1] : null;

            return command switch
            {
                "F" => GetFirst(input, args),
                "L" => GetLast(input, args),
                var s when s.StartsWith("*x") => GetMask(s, args),
                "digits" => GetDigits(input, args),
                "letters" => GetLetters(input, args),
                _ => "{{" + token + "}}" // Unknown token, keep as-is
            };
        }

        /// <summary>
        /// Gets the first n characters from the input.
        /// </summary>
        private string GetFirst(string input, string count)
        {
            int n = string.IsNullOrEmpty(count) ? 1 : int.Parse(count);
            return input.Length >= n ? input.Substring(0, n) : input;
        }

        /// <summary>
        /// Gets the last n characters from the input.
        /// </summary>
        private string GetLast(string input, string count)
        {
            int n = string.IsNullOrEmpty(count) ? 1 : int.Parse(count);
            return input.Length >= n ? input.Substring(input.Length - n) : input;
        }

        /// <summary>
        /// Generates a mask of asterisks.
        /// </summary>
        private string GetMask(string command, string _)
        {
            // Extract count from "*x5" → 5
            int count = int.Parse(command.Substring(2));
            return new string('*', count);
        }

        /// <summary>
        /// Extracts digits from the input with optional range.
        /// </summary>
        private string GetDigits(string input, string range)
        {
            var digits = new string(input.Where(char.IsDigit).ToArray());
            return ApplyRange(digits, range);
        }

        /// <summary>
        /// Extracts letters from the input with optional range.
        /// </summary>
        private string GetLetters(string input, string range)
        {
            var letters = new string(input.Where(char.IsLetter).ToArray());
            return ApplyRange(letters, range);
        }

        /// <summary>
        /// Applies a range specification to extract a substring.
        /// </summary>
        /// <param name="text">The text to extract from</param>
        /// <param name="range">Range specification like "0-3", "2-5", or "-2" (last 2)</param>
        /// <returns>The extracted substring</returns>
        private string ApplyRange(string text, string range)
        {
            if (string.IsNullOrEmpty(range))
                return text;

            // Parse "0-3" or "2-5" or "-2" (last 2)
            var parts = range.Split('-');

            // Handle negative-only range like "-2" (means last 2 characters)
            if (parts.Length == 2 && string.IsNullOrEmpty(parts[0]))
            {
                // This is a negative range like "-2"
                int negCount = int.Parse(parts[1]);
                int start = Math.Max(0, text.Length - negCount);
                return text.Substring(start);
            }

            int startPos = string.IsNullOrEmpty(parts[0]) ? 0 : int.Parse(parts[0]);
            int endPos = parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) ? int.Parse(parts[1]) : text.Length;

            // Handle negative indexing
            if (startPos < 0) startPos = text.Length + startPos;
            if (endPos < 0) endPos = text.Length + endPos;

            // Clamp to valid range
            startPos = Math.Max(0, Math.Min(startPos, text.Length));
            endPos = Math.Max(0, Math.Min(endPos, text.Length));

            return text.Substring(startPos, Math.Max(0, endPos - startPos));
        }
    }
}
