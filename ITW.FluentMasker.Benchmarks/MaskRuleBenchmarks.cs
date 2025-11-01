using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ITW.FluentMasker.MaskRules;

namespace ITW.FluentMasker.Benchmarks
{
    /// <summary>
    /// Benchmarks for individual mask rules to verify performance targets.
    /// Target: ≥ 50,000 ops/sec for most rules, ≥ 40,000 ops/sec for complex rules.
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    [MarkdownExporter]
    public class MaskRuleBenchmarks
    {
        private readonly MaskStartRule _maskStartRule = new(5, "*");
        private readonly MaskEndRule _maskEndRule = new(5, "*");
        private readonly MaskMiddleRule _maskMiddleRule = new(2, 2, "*");
        private readonly MaskRangeRule _maskRangeRule = new(2, 5, "*");
        private readonly MaskPercentageRule _maskPercentageRule = new(0.5, MaskFrom.Start, "*");
        private readonly KeepFirstRule _keepFirstRule = new(3);
        private readonly KeepLastRule _keepLastRule = new(3);
        private readonly TruncateRule _truncateRule = new(10, "...");
        private readonly RedactRule _redactRule = new("[REDACTED]");
        private readonly NullOutRule _nullOutRule = new();
        private readonly TemplateMaskRule _templateMaskRule = new("{{F}}{{*x6}}{{L}}");
        private readonly BlacklistCharsRule _blacklistCharsRule = new("@.", "*");
        private readonly WhitelistCharsRule _whitelistCharsRule = new("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", "*");
        private readonly MaskCharClassRule _maskDigitsRule = new(CharClass.Digit, "*");
        private readonly MaskCharClassRule _maskLettersRule = new(CharClass.Letter, "*");
        private readonly MaskCharClassRule _maskWhitespaceRule = new(CharClass.Whitespace, "_");
        private readonly PhoneMaskRule _phoneMaskRulePreserve = new(keepLast: 4, preserveSeparators: true);
        private readonly PhoneMaskRule _phoneMaskRuleNoPreserve = new(keepLast: 4, preserveSeparators: false);

        private readonly string _shortString = "JohnDoe";
        private readonly string _mediumString = "john.doe@example.com";
        private readonly string _longString = new string('A', 1000);
        private readonly string _emailString = "Email: test@example.com";
        private readonly string _mixedString = "Test123ABC456xyz";
        private readonly string _phoneString = "Phone: 555-123-4567";
        private readonly string _phoneE164 = "+442071234567";
        private readonly string _phoneNorthAmerican = "(555) 123-4567";
        private readonly string _phoneInternational = "+1 (555) 123-4567";

        [GlobalSetup]
        public void Setup()
        {
            // Warmup - ensure JIT compilation is done
            _maskStartRule.Apply(_shortString);
            _maskMiddleRule.Apply(_shortString);
            _maskEndRule.Apply(_shortString);
        }

        #region MaskStart Benchmarks

        [Benchmark(Baseline = true, Description = "MaskStart on short string (7 chars)")]
        public string MaskStart_ShortString() => _maskStartRule.Apply(_shortString);

        [Benchmark(Description = "MaskStart on medium string (20 chars)")]
        public string MaskStart_MediumString() => _maskStartRule.Apply(_mediumString);

        [Benchmark(Description = "MaskStart on long string (1000 chars)")]
        public string MaskStart_LongString() => _maskStartRule.Apply(_longString);

        #endregion

        #region MaskEnd Benchmarks

        [Benchmark(Description = "MaskEnd on short string")]
        public string MaskEnd_ShortString() => _maskEndRule.Apply(_shortString);

        [Benchmark(Description = "MaskEnd on long string")]
        public string MaskEnd_LongString() => _maskEndRule.Apply(_longString);

        #endregion

        #region MaskMiddle Benchmarks

        [Benchmark(Description = "MaskMiddle on short string")]
        public string MaskMiddle_ShortString() => _maskMiddleRule.Apply(_shortString);

        [Benchmark(Description = "MaskMiddle on medium string")]
        public string MaskMiddle_MediumString() => _maskMiddleRule.Apply(_mediumString);

        [Benchmark(Description = "MaskMiddle on long string")]
        public string MaskMiddle_LongString() => _maskMiddleRule.Apply(_longString);

        #endregion

        #region MaskRange Benchmarks

        [Benchmark(Description = "MaskRange on short string")]
        public string MaskRange_ShortString() => _maskRangeRule.Apply(_shortString);

        [Benchmark(Description = "MaskRange on long string")]
        public string MaskRange_LongString() => _maskRangeRule.Apply(_longString);

        #endregion

        #region MaskPercentage Benchmarks

        [Benchmark(Description = "MaskPercentage on short string")]
        public string MaskPercentage_ShortString() => _maskPercentageRule.Apply(_shortString);

        [Benchmark(Description = "MaskPercentage on long string")]
        public string MaskPercentage_LongString() => _maskPercentageRule.Apply(_longString);

        #endregion

        #region Keep Benchmarks

        [Benchmark(Description = "KeepFirst on short string")]
        public string KeepFirst_ShortString() => _keepFirstRule.Apply(_shortString);

        [Benchmark(Description = "KeepLast on short string")]
        public string KeepLast_ShortString() => _keepLastRule.Apply(_shortString);

        #endregion

        #region Structural Benchmarks

        [Benchmark(Description = "Truncate on long string")]
        public string Truncate_LongString() => _truncateRule.Apply(_longString);

        [Benchmark(Description = "Redact (constant replacement)")]
        public string Redact_ShortString() => _redactRule.Apply(_shortString);

        [Benchmark(Description = "NullOut (trivial operation)")]
        public string NullOut_ShortString() => _nullOutRule.Apply(_shortString);

        #endregion

        #region Template Benchmarks

        [Benchmark(Description = "TemplateMask on short string")]
        public string TemplateMask_ShortString() => _templateMaskRule.Apply(_shortString);

        [Benchmark(Description = "TemplateMask on medium string")]
        public string TemplateMask_MediumString() => _templateMaskRule.Apply(_mediumString);

        #endregion

        #region Character Filtering Benchmarks

        [Benchmark(Description = "BlacklistChars on short string")]
        public string BlacklistChars_ShortString() => _blacklistCharsRule.Apply(_shortString);

        [Benchmark(Description = "BlacklistChars on email string")]
        public string BlacklistChars_EmailString() => _blacklistCharsRule.Apply(_emailString);

        [Benchmark(Description = "BlacklistChars on medium string")]
        public string BlacklistChars_MediumString() => _blacklistCharsRule.Apply(_mediumString);

        [Benchmark(Description = "BlacklistChars on long string")]
        public string BlacklistChars_LongString() => _blacklistCharsRule.Apply(_longString);

        [Benchmark(Description = "WhitelistChars on short string")]
        public string WhitelistChars_ShortString() => _whitelistCharsRule.Apply(_shortString);

        [Benchmark(Description = "WhitelistChars on medium string")]
        public string WhitelistChars_MediumString() => _whitelistCharsRule.Apply(_mediumString);

        [Benchmark(Description = "WhitelistChars on long string")]
        public string WhitelistChars_LongString() => _whitelistCharsRule.Apply(_longString);

        #endregion

        #region CharClass Masking Benchmarks

        [Benchmark(Description = "MaskDigits on short mixed string")]
        public string MaskDigits_ShortString() => _maskDigitsRule.Apply(_mixedString);

        [Benchmark(Description = "MaskDigits on medium string")]
        public string MaskDigits_MediumString() => _maskDigitsRule.Apply(_mediumString);

        [Benchmark(Description = "MaskDigits on phone string")]
        public string MaskDigits_PhoneString() => _maskDigitsRule.Apply(_phoneString);

        [Benchmark(Description = "MaskDigits on long string")]
        public string MaskDigits_LongString() => _maskDigitsRule.Apply(_longString);

        [Benchmark(Description = "MaskLetters on short mixed string")]
        public string MaskLetters_ShortString() => _maskLettersRule.Apply(_mixedString);

        [Benchmark(Description = "MaskLetters on medium string")]
        public string MaskLetters_MediumString() => _maskLettersRule.Apply(_mediumString);

        [Benchmark(Description = "MaskLetters on long string")]
        public string MaskLetters_LongString() => _maskLettersRule.Apply(_longString);

        [Benchmark(Description = "MaskWhitespace on short string")]
        public string MaskWhitespace_ShortString() => _maskWhitespaceRule.Apply("Hello World");

        [Benchmark(Description = "MaskWhitespace on long string with spaces")]
        public string MaskWhitespace_LongString() => _maskWhitespaceRule.Apply("This is a long string with many spaces in it to test performance");

        #endregion

        #region PhoneMask Benchmarks

        [Benchmark(Description = "PhoneMask E.164 format (preserving separators)")]
        public string PhoneMask_E164_PreserveSeparators() => _phoneMaskRulePreserve.Apply(_phoneE164);

        [Benchmark(Description = "PhoneMask North American format (preserving)")]
        public string PhoneMask_NorthAmerican_PreserveSeparators() => _phoneMaskRulePreserve.Apply(_phoneNorthAmerican);

        [Benchmark(Description = "PhoneMask International format (preserving)")]
        public string PhoneMask_International_PreserveSeparators() => _phoneMaskRulePreserve.Apply(_phoneInternational);

        [Benchmark(Description = "PhoneMask North American (no separators)")]
        public string PhoneMask_NorthAmerican_NoSeparators() => _phoneMaskRuleNoPreserve.Apply(_phoneNorthAmerican);

        [Benchmark(Description = "PhoneMask International (no separators)")]
        public string PhoneMask_International_NoSeparators() => _phoneMaskRuleNoPreserve.Apply(_phoneInternational);

        #endregion

        #region Edge Case Benchmarks

        [Benchmark(Description = "MaskStart on empty string")]
        public string MaskStart_EmptyString() => _maskStartRule.Apply("");

        [Benchmark(Description = "MaskStart on null string")]
        public string MaskStart_NullString() => _maskStartRule.Apply(null);

        #endregion
    }
}
