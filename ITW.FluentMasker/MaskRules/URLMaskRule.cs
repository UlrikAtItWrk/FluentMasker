using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Masks URLs by hiding or masking query parameters and path segments.
    /// Supports selective masking of specific query keys and path segments by index.
    /// </summary>
    /// <remarks>
    /// <para>Common use cases include:</para>
    /// <list type="bullet">
    /// <item><description>Preventing token leaks in logs: api.com/users?token=abc123 ? api.com/users?token=***</description></item>
    /// <item><description>Masking user IDs in paths: /users/12345/profile ? /users/***/profile</description></item>
    /// <item><description>Hiding sensitive query strings entirely</description></item>
    /// </list>
    /// <para>Preserves URL structure including scheme, host, port, and fragments.</para>
    /// <para>Invalid URLs are returned unchanged.</para>
    /// <para>Null and empty strings are returned unchanged.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Hide entire query string
    /// var rule1 = new URLMaskRule(hideQuery: true);
    /// var result1 = rule1.Apply("https://api.example.com/users?token=abc123&amp;id=456");
    /// // result1 = "https://api.example.com/users"
    ///
    /// // Mask specific query parameters
    /// var rule2 = new URLMaskRule(maskQueryKeys: new[] { "token", "apiKey" });
    /// var result2 = rule2.Apply("https://api.example.com/users?token=abc123&amp;user=john");
    /// // result2 = "https://api.example.com/users?token=***&amp;user=john"
    ///
    /// // Mask path segments
    /// var rule3 = new URLMaskRule(maskPathSegments: new[] { 2 });
    /// var result3 = rule3.Apply("https://api.example.com/users/12345/profile");
    /// // result3 = "https://api.example.com/users/***/profile"
    ///
    /// // Combine query and path masking
    /// var rule4 = new URLMaskRule(
    ///     maskQueryKeys: new[] { "token" },
    ///     maskPathSegments: new[] { 2 }
    /// );
    /// var result4 = rule4.Apply("https://api.example.com/users/12345?token=secret");
    /// // result4 = "https://api.example.com/users/***?token=***"
    ///
    /// // Invalid URL returns unchanged
    /// rule1.Apply("not-a-url");  // Returns "not-a-url"
    /// </code>
    /// </example>
    public class URLMaskRule : IStringMaskRule
    {
        private readonly bool _hideQuery;
        private readonly HashSet<string> _maskQueryKeys;
        private readonly HashSet<int> _maskPathSegments;
        private readonly string _maskValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="URLMaskRule"/> class.
        /// </summary>
        /// <param name="hideQuery">If true, removes the entire query string. Takes precedence over maskQueryKeys.</param>
        /// <param name="maskQueryKeys">Array of query parameter keys to mask. Case-sensitive.</param>
        /// <param name="maskPathSegments">Array of path segment indices (0-based) to mask. Negative indices are ignored.</param>
        /// <param name="maskValue">The value to use for masking (default: "***")</param>
        /// <remarks>
        /// If both hideQuery and maskQueryKeys are specified, hideQuery takes precedence.
        /// Path segments are 0-indexed. For "/users/123/profile", segment 0 is "users", segment 1 is "123", segment 2 is "profile".
        /// </remarks>
        public URLMaskRule(
            bool hideQuery = false,
            string[]? maskQueryKeys = null,
            int[]? maskPathSegments = null,
            string maskValue = "***")
        {
            _hideQuery = hideQuery;
            _maskQueryKeys = maskQueryKeys != null ? new HashSet<string>(maskQueryKeys) : new HashSet<string>();
            _maskPathSegments = maskPathSegments != null ? new HashSet<int>(maskPathSegments.Where(i => i >= 0)) : new HashSet<int>();
            _maskValue = maskValue ?? "***";
        }

        /// <summary>
        /// Applies the URL masking rule to the input string.
        /// </summary>
        /// <param name="input">The URL to mask. Can be null or empty.</param>
        /// <returns>
        /// The masked URL according to the configured rules.
        /// Returns the original input if it is null, empty, or not a valid absolute URL.
        /// </returns>
        public string Apply(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Try to parse as absolute URI
            if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
                return input; // Invalid URL, return unchanged

            var builder = new UriBuilder(uri);
            
            // Remember if original path had trailing slash
            bool hadTrailingSlash = uri.AbsolutePath.EndsWith("/") && uri.AbsolutePath.Length > 1;

            // Hide entire query string
            if (_hideQuery)
            {
                builder.Query = string.Empty;
            }
            // Mask specific query keys
            else if (_maskQueryKeys.Count > 0 && !string.IsNullOrEmpty(uri.Query))
            {
                builder.Query = MaskQueryParameters(uri.Query);
            }

            // Mask path segments
            if (_maskPathSegments.Count > 0 && !string.IsNullOrEmpty(uri.AbsolutePath))
            {
                var maskedPath = MaskPathSegments(uri.AbsolutePath);
                builder.Path = maskedPath;
                
                // Restore trailing slash if needed (UriBuilder removes it)
                if (hadTrailingSlash && !maskedPath.EndsWith("/"))
                {
                    builder.Path = maskedPath + "/";
                }
            }

            return builder.Uri.ToString();
        }

        /// <summary>
        /// Masks specific query parameters by their keys.
        /// </summary>
        /// <param name="queryString">The query string including the leading '?'</param>
        /// <returns>The masked query string</returns>
        private string MaskQueryParameters(string queryString)
        {
            // Remove leading '?' if present
            var query = queryString.TrimStart('?');
            if (string.IsNullOrEmpty(query))
                return string.Empty;

            var parameters = ParseQueryString(query);
            var maskedParams = new List<string>();

            foreach (var param in parameters)
            {
                var key = param.Key;
                var value = param.Value;

                // Mask if key is in the mask list
                if (_maskQueryKeys.Contains(key))
                {
                    maskedParams.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(_maskValue)}");
                }
                else
                {
                    // Keep original (already encoded in the original URL)
                    if (string.IsNullOrEmpty(value))
                    {
                        maskedParams.Add(Uri.EscapeDataString(key));
                    }
                    else
                    {
                        maskedParams.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
                    }
                }
            }

            return string.Join("&", maskedParams);
        }

        /// <summary>
        /// Parses a query string into key-value pairs.
        /// </summary>
        /// <param name="query">The query string without the leading '?'</param>
        /// <returns>List of key-value pairs</returns>
        private static List<KeyValuePair<string, string>> ParseQueryString(string query)
        {
            var result = new List<KeyValuePair<string, string>>();
            
            if (string.IsNullOrEmpty(query))
                return result;

            var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var index = pair.IndexOf('=');
                if (index >= 0)
                {
                    var key = Uri.UnescapeDataString(pair.Substring(0, index));
                    var value = Uri.UnescapeDataString(pair.Substring(index + 1));
                    result.Add(new KeyValuePair<string, string>(key, value));
                }
                else
                {
                    // Parameter without value
                    var key = Uri.UnescapeDataString(pair);
                    result.Add(new KeyValuePair<string, string>(key, string.Empty));
                }
            }

            return result;
        }

        /// <summary>
        /// Masks specific path segments by their indices.
        /// </summary>
        /// <param name="path">The URL path</param>
        /// <returns>The masked path</returns>
        private string MaskPathSegments(string path)
        {
            if (string.IsNullOrEmpty(path) || path == "/")
                return path;

            // Split path by '/', keeping empty entries to preserve structure
            var segments = path.Split('/');
            
            // Track the actual segment index (excluding empty entries)
            var segmentIndex = 0;
            
            for (int i = 0; i < segments.Length; i++)
            {
                // Skip empty segments (e.g., leading '/' creates empty first segment)
                if (string.IsNullOrEmpty(segments[i]))
                    continue;

                // Check if this segment should be masked
                if (_maskPathSegments.Contains(segmentIndex))
                {
                    segments[i] = _maskValue;
                }

                segmentIndex++;
            }

            return string.Join("/", segments);
        }

        // Explicit interface implementation to avoid ambiguity in method overload resolution
        string IMaskRule<string, string>.Apply(string input) => Apply(input);
    }
}
