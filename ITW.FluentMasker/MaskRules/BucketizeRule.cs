using System;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Replaces exact numeric values with bucket labels for k-anonymity and privacy preservation.
    /// Uses binary search for efficient O(log n) bucket finding.
    /// </summary>
    /// <typeparam name="T">The input type (must be comparable, e.g., int, decimal, double)</typeparam>
    /// <remarks>
    /// <para>
    /// Bucketing (also called binning or generalization) is a privacy-preserving technique
    /// that replaces exact values with range labels. This achieves k-anonymity by grouping
    /// similar values together.
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// <list type="bullet">
    /// <item><description>Age bucketing: 27 → "18-29" (demographics)</description></item>
    /// <item><description>Salary ranges: 75000 → "60-90k" (compensation data)</description></item>
    /// <item><description>Time bucketing: 14:32 → "14:00-15:00" (temporal generalization)</description></item>
    /// <item><description>Credit score ranges: 720 → "700-749" (financial data)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Privacy Properties:</b>
    /// <list type="bullet">
    /// <item><description>Achieves k-anonymity by grouping k records with same bucket</description></item>
    /// <item><description>Reduces precision while preserving utility for analytics</description></item>
    /// <item><description>Prevents exact value re-identification</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <b>Example 1: Age Bucketing</b>
    /// <code>
    /// var rule = new BucketizeRule&lt;int&gt;(
    ///     breaks: new[] { 0, 18, 30, 45, 60, 100 },
    ///     labels: new[] { "&lt;18", "18-29", "30-44", "45-59", "60+" }
    /// );
    ///
    /// string result1 = rule.Apply(27);    // Returns "18-29"
    /// string result2 = rule.Apply(15);    // Returns "&lt;18"
    /// string result3 = rule.Apply(65);    // Returns "60+"
    /// string result4 = rule.Apply(30);    // Returns "30-44" (break point goes to upper bucket)
    /// </code>
    ///
    /// <b>Example 2: Salary Bucketing</b>
    /// <code>
    /// var rule = new BucketizeRule&lt;decimal&gt;(
    ///     breaks: new[] { 0m, 30000m, 60000m, 90000m, 120000m, decimal.MaxValue },
    ///     labels: new[] { "&lt;30k", "30-60k", "60-90k", "90-120k", "120k+" }
    /// );
    ///
    /// string result = rule.Apply(75000m);  // Returns "60-90k"
    /// </code>
    ///
    /// <b>Example 3: Credit Score Bucketing</b>
    /// <code>
    /// var rule = new BucketizeRule&lt;int&gt;(
    ///     breaks: new[] { 300, 580, 670, 740, 800, 850 },
    ///     labels: new[] { "Poor", "Fair", "Good", "Very Good", "Excellent" }
    /// );
    ///
    /// string result = rule.Apply(720);  // Returns "Good"
    /// </code>
    /// </example>
    public class BucketizeRule<T> : IMaskRule<T, string>
        where T : struct, IComparable<T>
    {
        private readonly T[] _breaks;
        private readonly string[] _labels;

        /// <summary>
        /// Initializes a new instance of the BucketizeRule class.
        /// </summary>
        /// <param name="breaks">
        /// Array of break points defining bucket boundaries. Must have exactly one more element than labels.
        /// Break points must be in ascending order.
        /// </param>
        /// <param name="labels">
        /// Array of labels for each bucket. Length must be exactly (breaks.Length - 1).
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when breaks or labels is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when:
        /// <list type="bullet">
        /// <item><description>breaks.Length != labels.Length + 1</description></item>
        /// <item><description>breaks array is not in ascending order</description></item>
        /// <item><description>breaks or labels arrays are empty</description></item>
        /// </list>
        /// </exception>
        /// <remarks>
        /// <para>
        /// The breaks array defines bucket boundaries. For n breaks, there are (n-1) buckets:
        /// <list type="bullet">
        /// <item><description>Bucket 0: [breaks[0], breaks[1])</description></item>
        /// <item><description>Bucket 1: [breaks[1], breaks[2])</description></item>
        /// <item><description>...</description></item>
        /// <item><description>Bucket (n-2): [breaks[n-2], breaks[n-1])</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Values exactly on a break point are assigned to the upper bucket (right-closed intervals).
        /// </para>
        /// </remarks>
        public BucketizeRule(T[] breaks, string[] labels)
        {
            if (breaks == null)
                throw new ArgumentNullException(nameof(breaks), "Breaks array cannot be null");

            if (labels == null)
                throw new ArgumentNullException(nameof(labels), "Labels array cannot be null");

            if (breaks.Length == 0)
                throw new ArgumentException("Breaks array cannot be empty", nameof(breaks));

            if (labels.Length == 0)
                throw new ArgumentException("Labels array cannot be empty", nameof(labels));

            if (breaks.Length != labels.Length + 1)
            {
                throw new ArgumentException(
                    $"Breaks array must have exactly one more element than labels. " +
                    $"Expected {labels.Length + 1} breaks for {labels.Length} labels, " +
                    $"but got {breaks.Length} breaks.",
                    nameof(breaks));
            }

            // Validate that breaks are in ascending order
            for (int i = 0; i < breaks.Length - 1; i++)
            {
                if (breaks[i].CompareTo(breaks[i + 1]) >= 0)
                {
                    throw new ArgumentException(
                        $"Breaks array must be in strictly ascending order. " +
                        $"Found breaks[{i}] = {breaks[i]} >= breaks[{i + 1}] = {breaks[i + 1]}",
                        nameof(breaks));
                }
            }

            _breaks = breaks;
            _labels = labels;
        }

        /// <summary>
        /// Applies the bucketing rule to the input value.
        /// </summary>
        /// <param name="input">The value to bucketize.</param>
        /// <returns>The bucket label corresponding to the input value.</returns>
        /// <remarks>
        /// <para>
        /// Uses binary search for O(log n) time complexity.
        /// </para>
        /// <para>
        /// <b>Edge Cases:</b>
        /// <list type="bullet">
        /// <item><description>Value below minimum break → first bucket (label[0])</description></item>
        /// <item><description>Value above maximum break → last bucket (label[n-1])</description></item>
        /// <item><description>Value exactly on break point → upper bucket (right-closed interval)</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string Apply(T input)
        {
            // Find the bucket index using binary search
            int bucketIndex = FindBucketIndex(input);
            return _labels[bucketIndex];
        }

        /// <summary>
        /// Finds the bucket index for the given value using binary search.
        /// Time complexity: O(log n)
        /// </summary>
        /// <param name="value">The value to find a bucket for.</param>
        /// <returns>The zero-based index of the bucket (0 to labels.Length-1).</returns>
        private int FindBucketIndex(T value)
        {
            // Handle edge cases first (optimization for common cases)

            // Value below minimum break → first bucket
            if (value.CompareTo(_breaks[0]) < 0)
            {
                return 0;
            }

            // Value above or equal to maximum break → last bucket
            if (value.CompareTo(_breaks[_breaks.Length - 1]) >= 0)
            {
                return _labels.Length - 1;
            }

            // Binary search for the correct bucket
            // We're looking for the largest index i where breaks[i] <= value < breaks[i+1]
            int left = 0;
            int right = _breaks.Length - 2; // -2 because we're checking breaks[mid] and breaks[mid+1]

            while (left <= right)
            {
                int mid = left + (right - left) / 2;

                // Check if value is in bucket [breaks[mid], breaks[mid+1])
                if (value.CompareTo(_breaks[mid]) >= 0 && value.CompareTo(_breaks[mid + 1]) < 0)
                {
                    return mid; // Found the bucket
                }
                else if (value.CompareTo(_breaks[mid]) < 0)
                {
                    // Value is in a lower bucket
                    right = mid - 1;
                }
                else
                {
                    // Value is in a higher bucket (value >= breaks[mid+1])
                    left = mid + 1;
                }
            }

            // This should never be reached due to edge case handling above,
            // but return last bucket as fallback
            return _labels.Length - 1;
        }
    }
}
