using System;
using System.Security.Cryptography;
using System.Text;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Specifies the hash algorithm to use for cryptographic hashing.
    /// </summary>
    public enum HashAlgorithmType
    {
        /// <summary>
        /// SHA-256 algorithm (recommended, produces 64-character hex output)
        /// </summary>
        SHA256,

        /// <summary>
        /// SHA-512 algorithm (produces 128-character hex output)
        /// </summary>
        SHA512,

        /// <summary>
        /// MD5 algorithm (legacy, cryptographically broken - displays warning)
        /// </summary>
        MD5
    }

    /// <summary>
    /// Specifies how salt values are generated for hashing.
    /// </summary>
    public enum SaltMode
    {
        /// <summary>
        /// Unique random salt per record (non-deterministic - same input produces different outputs)
        /// </summary>
        PerRecord,

        /// <summary>
        /// Static configured salt (deterministic - same input produces same output)
        /// </summary>
        Static,

        /// <summary>
        /// Salt derived from field name (deterministic but not linkable across fields)
        /// </summary>
        PerField
    }

    /// <summary>
    /// Specifies the output format for the hash value.
    /// </summary>
    public enum OutputFormat
    {
        /// <summary>
        /// Hexadecimal lowercase format (e.g., "a1b2c3d4...")
        /// </summary>
        Hex,

        /// <summary>
        /// Standard Base64 format (e.g., "YWJjZGVm...")
        /// </summary>
        Base64,

        /// <summary>
        /// URL-safe Base64 format (replaces +/= with -_)
        /// </summary>
        Base64Url
    }

    /// <summary>
    /// Applies cryptographic hashing for GDPR pseudonymization.
    /// </summary>
    /// <remarks>
    /// <para>This rule uses industry-standard cryptographic hash algorithms to transform data in a one-way manner.</para>
    /// <para>Supports multiple salt modes for different use cases:</para>
    /// <list type="bullet">
    /// <item><description><b>Static:</b> Deterministic hashing - same input always produces same output (useful for lookups)</description></item>
    /// <item><description><b>PerRecord:</b> Non-deterministic - each invocation produces different output (maximum privacy)</description></item>
    /// <item><description><b>PerField:</b> Field-specific deterministic hashing - prevents cross-field correlation</description></item>
    /// </list>
    /// <para>Uses <see cref="RandomNumberGenerator"/> for cryptographically secure random salt generation.</para>
    /// <para>MD5 algorithm displays a warning as it is cryptographically broken.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // SHA256 with static salt (deterministic)
    /// var rule1 = new HashRule();
    /// var result1 = rule1.Apply("sensitive@email.com");
    /// // result1 = "a1b2c3d4..." (64 hex chars)
    ///
    /// // SHA512 with per-record salt (non-deterministic)
    /// var rule2 = new HashRule(HashAlgorithmType.SHA512, SaltMode.PerRecord);
    /// var result2a = rule2.Apply("John Doe");
    /// var result2b = rule2.Apply("John Doe");
    /// // result2a != result2b (different random salts)
    ///
    /// // SHA256 with per-field salt
    /// var rule3 = new HashRule(HashAlgorithmType.SHA256, SaltMode.PerField, OutputFormat.Base64, fieldName: "Email");
    /// var result3 = rule3.Apply("user@example.com");
    /// // result3 = Base64-encoded hash
    ///
    /// // Edge cases:
    /// rule1.Apply(null);   // Returns null
    /// rule1.Apply("");     // Returns "" (empty string unchanged)
    /// </code>
    /// </example>
    public class HashRule : IStringMaskRule
    {
        private readonly HashAlgorithmType _algorithmType;
        private readonly SaltMode _saltMode;
        private readonly OutputFormat _outputFormat;
        private readonly byte[] _staticSalt;
        private readonly string _fieldName;

        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        /// <summary>
        /// Initializes a new instance of the <see cref="HashRule"/> class.
        /// </summary>
        /// <param name="algorithm">The hash algorithm to use (default: SHA256)</param>
        /// <param name="saltMode">The salt generation mode (default: Static)</param>
        /// <param name="outputFormat">The output format (default: Hex)</param>
        /// <param name="staticSalt">The static salt to use (only for Static mode). If null, a random salt is generated once.</param>
        /// <param name="fieldName">The field name for PerField salt mode</param>
        /// <exception cref="ArgumentException">Thrown when fieldName is required for PerField mode but not provided</exception>
        public HashRule(
            HashAlgorithmType algorithm = HashAlgorithmType.SHA256,
            SaltMode saltMode = SaltMode.Static,
            OutputFormat outputFormat = OutputFormat.Hex,
            byte[] staticSalt = null,
            string fieldName = null)
        {
            _algorithmType = algorithm;
            _saltMode = saltMode;
            _outputFormat = outputFormat;
            _fieldName = fieldName;

            if (algorithm == HashAlgorithmType.MD5)
            {
                // Log warning but don't fail - allows legacy compatibility
                Console.WriteLine("WARNING: MD5 is cryptographically broken and should not be used for security purposes.");
            }

            if (saltMode == SaltMode.Static)
            {
                _staticSalt = staticSalt ?? GenerateDefaultSalt();
            }
            else if (saltMode == SaltMode.PerField && string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException("Field name required for PerField salt mode", nameof(fieldName));
            }
        }

        /// <summary>
        /// Applies the hash rule to the input string, producing a cryptographic hash.
        /// </summary>
        /// <param name="input">The string to hash. Can be null or empty.</param>
        /// <returns>
        /// The hashed string in the specified output format.
        /// Returns the original input if it is null or empty.
        /// </returns>
        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            byte[] salt = GetSalt();
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] saltedInput = new byte[salt.Length + inputBytes.Length];

            // Prepend salt to input (salt || input)
            Buffer.BlockCopy(salt, 0, saltedInput, 0, salt.Length);
            Buffer.BlockCopy(inputBytes, 0, saltedInput, salt.Length, inputBytes.Length);

            byte[] hash = ComputeHash(saltedInput);

            return FormatOutput(hash);
        }

        /// <summary>
        /// Gets the salt based on the configured salt mode.
        /// </summary>
        private byte[] GetSalt()
        {
            return _saltMode switch
            {
                SaltMode.Static => _staticSalt,
                SaltMode.PerRecord => GenerateRandomSalt(),
                SaltMode.PerField => GenerateFieldSalt(),
                _ => throw new NotSupportedException($"Salt mode {_saltMode} is not supported.")
            };
        }

        /// <summary>
        /// Generates a cryptographically secure random salt.
        /// </summary>
        private static byte[] GenerateRandomSalt()
        {
            byte[] salt = new byte[16]; // 128-bit salt
            _rng.GetBytes(salt);
            return salt;
        }

        /// <summary>
        /// Generates a deterministic salt based on the field name.
        /// </summary>
        private byte[] GenerateFieldSalt()
        {
            // Deterministic salt based on field name using SHA256
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(_fieldName));
        }

        /// <summary>
        /// Generates a default random static salt (used when no static salt is provided).
        /// </summary>
        private static byte[] GenerateDefaultSalt()
        {
            byte[] salt = new byte[16]; // 128-bit salt
            _rng.GetBytes(salt);
            return salt;
        }

        /// <summary>
        /// Computes the hash using the configured algorithm.
        /// </summary>
        private byte[] ComputeHash(byte[] input)
        {
            using HashAlgorithm algorithm = _algorithmType switch
            {
                HashAlgorithmType.SHA256 => SHA256.Create(),
                HashAlgorithmType.SHA512 => SHA512.Create(),
                HashAlgorithmType.MD5 => MD5.Create(),
                _ => throw new NotSupportedException($"Hash algorithm {_algorithmType} is not supported.")
            };

            return algorithm.ComputeHash(input);
        }

        /// <summary>
        /// Formats the hash output according to the configured format.
        /// </summary>
        private string FormatOutput(byte[] hash)
        {
            return _outputFormat switch
            {
                OutputFormat.Hex => BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant(),
                OutputFormat.Base64 => Convert.ToBase64String(hash),
                OutputFormat.Base64Url => Convert.ToBase64String(hash)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .TrimEnd('='),
                _ => throw new NotSupportedException($"Output format {_outputFormat} is not supported.")
            };
        }

        // Explicit interface implementation to avoid ambiguity in method overload resolution
        string IMaskRule<string, string>.Apply(string input) => Apply(input);
    }
}
