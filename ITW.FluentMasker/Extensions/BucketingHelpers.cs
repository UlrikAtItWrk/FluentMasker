using ITW.FluentMasker.MaskRules;

namespace ITW.FluentMasker.Extensions
{
    /// <summary>
    /// Helper class providing factory methods for common bucketing scenarios.
    /// Simplifies creation of BucketizeRule instances with predefined buckets for common use cases.
    /// </summary>
    /// <remarks>
    /// <para>
    /// BucketizeRule is used directly with AbstractMasker.MaskFor() method:
    /// <code>
    /// masker.MaskFor(x => x.Age, BucketingHelpers.CreateAgeBuckets());
    /// masker.MaskFor(x => x.Salary, BucketingHelpers.CreateSalaryBuckets());
    /// </code>
    /// </para>
    /// </remarks>
    public static class BucketingHelpers
    {
        /// <summary>
        /// Creates a standard age bucketing rule with demographic ranges.
        /// Buckets: &lt;18, 18-29, 30-44, 45-59, 60+
        /// </summary>
        /// <returns>A BucketizeRule configured for age demographics</returns>
        /// <example>
        /// <code>
        /// public class PersonMasker : AbstractMasker&lt;Person&gt;
        /// {
        ///     public PersonMasker()
        ///     {
        ///         MaskFor(x => x.Age, BucketingHelpers.CreateAgeBuckets());
        ///         // Age 27 becomes "18-29"
        ///     }
        /// }
        /// </code>
        /// </example>
        public static BucketizeRule<int> CreateAgeBuckets()
        {
            return new BucketizeRule<int>(
                breaks: new[] { 0, 18, 30, 45, 60, 100 },
                labels: new[] { "<18", "18-29", "30-44", "45-59", "60+" }
            );
        }

        /// <summary>
        /// Creates a more granular age bucketing rule with 10-year intervals.
        /// Buckets: &lt;18, 18-29, 30-39, 40-49, 50-59, 60-69, 70+
        /// </summary>
        /// <returns>A BucketizeRule configured for detailed age demographics</returns>
        /// <example>
        /// <code>
        /// masker.MaskFor(x => x.Age, BucketingHelpers.CreateDetailedAgeBuckets());
        /// // Age 35 becomes "30-39"
        /// </code>
        /// </example>
        public static BucketizeRule<int> CreateDetailedAgeBuckets()
        {
            return new BucketizeRule<int>(
                breaks: new[] { 0, 18, 30, 40, 50, 60, 70, 120 },
                labels: new[] { "<18", "18-29", "30-39", "40-49", "50-59", "60-69", "70+" }
            );
        }

        /// <summary>
        /// Creates a standard salary bucketing rule for compensation data (USD).
        /// Buckets: &lt;30k, 30-60k, 60-90k, 90-120k, 120-150k, 150k+
        /// </summary>
        /// <returns>A BucketizeRule configured for salary ranges</returns>
        /// <example>
        /// <code>
        /// masker.MaskFor(x => x.Salary, BucketingHelpers.CreateSalaryBuckets());
        /// // Salary 75000 becomes "60-90k"
        /// </code>
        /// </example>
        public static BucketizeRule<decimal> CreateSalaryBuckets()
        {
            return new BucketizeRule<decimal>(
                breaks: new[] { 0m, 30000m, 60000m, 90000m, 120000m, 150000m, decimal.MaxValue },
                labels: new[] { "<30k", "30-60k", "60-90k", "90-120k", "120-150k", "150k+" }
            );
        }

        /// <summary>
        /// Creates a salary bucketing rule with wider ranges for senior positions.
        /// Buckets: &lt;50k, 50-75k, 75-100k, 100-150k, 150-200k, 200-300k, 300k+
        /// </summary>
        /// <returns>A BucketizeRule configured for senior-level salary ranges</returns>
        public static BucketizeRule<decimal> CreateSeniorSalaryBuckets()
        {
            return new BucketizeRule<decimal>(
                breaks: new[] { 0m, 50000m, 75000m, 100000m, 150000m, 200000m, 300000m, decimal.MaxValue },
                labels: new[] { "<50k", "50-75k", "75-100k", "100-150k", "150-200k", "200-300k", "300k+" }
            );
        }

        /// <summary>
        /// Creates a credit score bucketing rule with standard credit tiers.
        /// Buckets: Poor (&lt;580), Fair (580-669), Good (670-739), Very Good (740-799), Excellent (800+)
        /// </summary>
        /// <returns>A BucketizeRule configured for credit score tiers</returns>
        /// <example>
        /// <code>
        /// masker.MaskFor(x => x.CreditScore, BucketingHelpers.CreateCreditScoreBuckets());
        /// // Score 720 becomes "Good"
        /// </code>
        /// </example>
        public static BucketizeRule<int> CreateCreditScoreBuckets()
        {
            return new BucketizeRule<int>(
                breaks: new[] { 300, 580, 670, 740, 800, 850 },
                labels: new[] { "Poor", "Fair", "Good", "Very Good", "Excellent" }
            );
        }

        /// <summary>
        /// Creates an income tax bracket bucketing rule (US federal tax brackets 2024).
        /// Buckets: 10% bracket, 12% bracket, 22% bracket, 24% bracket, 32% bracket, 35% bracket, 37% bracket
        /// </summary>
        /// <returns>A BucketizeRule configured for US federal income tax brackets</returns>
        /// <remarks>
        /// Tax brackets are for single filers, 2024 tax year.
        /// </remarks>
        public static BucketizeRule<decimal> CreateTaxBracketBuckets()
        {
            return new BucketizeRule<decimal>(
                breaks: new[] { 0m, 11600m, 47150m, 100525m, 191950m, 243725m, 609350m, decimal.MaxValue },
                labels: new[] { "10%", "12%", "22%", "24%", "32%", "35%", "37%" }
            );
        }

        /// <summary>
        /// Creates a housing price bucketing rule (USD).
        /// Buckets: &lt;100k, 100-200k, 200-300k, 300-500k, 500-750k, 750k-1M, 1M+
        /// </summary>
        /// <returns>A BucketizeRule configured for housing price ranges</returns>
        public static BucketizeRule<decimal> CreateHousingPriceBuckets()
        {
            return new BucketizeRule<decimal>(
                breaks: new[] { 0m, 100000m, 200000m, 300000m, 500000m, 750000m, 1000000m, decimal.MaxValue },
                labels: new[] { "<100k", "100-200k", "200-300k", "300-500k", "500-750k", "750k-1M", "1M+" }
            );
        }

        /// <summary>
        /// Creates a percentage bucketing rule with quintiles (0-20%, 20-40%, 40-60%, 60-80%, 80-100%).
        /// Useful for performance metrics, test scores, or any percentage-based data.
        /// </summary>
        /// <returns>A BucketizeRule configured for percentage quintiles</returns>
        /// <example>
        /// <code>
        /// masker.MaskFor(x => x.TestScore, BucketingHelpers.CreatePercentageQuintiles());
        /// // Score 75% becomes "60-80%"
        /// </code>
        /// </example>
        public static BucketizeRule<double> CreatePercentageQuintiles()
        {
            return new BucketizeRule<double>(
                breaks: new[] { 0.0, 0.2, 0.4, 0.6, 0.8, 1.0 },
                labels: new[] { "0-20%", "20-40%", "40-60%", "60-80%", "80-100%" }
            );
        }

        /// <summary>
        /// Creates a transaction amount bucketing rule for financial data.
        /// Buckets: &lt;$10, $10-50, $50-100, $100-500, $500-1k, $1k-5k, $5k+
        /// </summary>
        /// <returns>A BucketizeRule configured for transaction amounts</returns>
        public static BucketizeRule<decimal> CreateTransactionAmountBuckets()
        {
            return new BucketizeRule<decimal>(
                breaks: new[] { 0m, 10m, 50m, 100m, 500m, 1000m, 5000m, decimal.MaxValue },
                labels: new[] { "<$10", "$10-50", "$50-100", "$100-500", "$500-1k", "$1k-5k", "$5k+" }
            );
        }

        /// <summary>
        /// Creates a BMI (Body Mass Index) bucketing rule with WHO classifications.
        /// Buckets: Underweight, Normal, Overweight, Obese Class I, Obese Class II, Obese Class III
        /// </summary>
        /// <returns>A BucketizeRule configured for BMI classifications</returns>
        /// <example>
        /// <code>
        /// masker.MaskFor(x => x.BMI, BucketingHelpers.CreateBMIBuckets());
        /// // BMI 27.5 becomes "Overweight"
        /// </code>
        /// </example>
        public static BucketizeRule<double> CreateBMIBuckets()
        {
            return new BucketizeRule<double>(
                breaks: new[] { 0.0, 18.5, 25.0, 30.0, 35.0, 40.0, 100.0 },
                labels: new[] { "Underweight", "Normal", "Overweight", "Obese Class I", "Obese Class II", "Obese Class III" }
            );
        }
    }
}
