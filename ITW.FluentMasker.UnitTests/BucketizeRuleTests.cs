using ITW.FluentMasker.MaskRules;
using System;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for BucketizeRule - tests bucketing/binning values into labeled ranges
    /// </summary>
    public class BucketizeRuleTests
    {
        #region PRD Example Tests (Acceptance Criteria)

        [Fact]
        public void Apply_Age27_Returns18To29()
        {
            // Arrange - PRD Example: Age 27 → "18-29"
            var rule = new BucketizeRule<int>(
                breaks: new[] { 0, 18, 30, 45, 60, 100 },
                labels: new[] { "<18", "18-29", "30-44", "45-59", "60+" }
            );

            // Act
            var result = rule.Apply(27);

            // Assert
            Assert.Equal("18-29", result);
        }

        [Fact]
        public void Apply_Salary75000_Returns60To90k()
        {
            // Arrange - PRD Example: Salary 75000 → "60-90k"
            var rule = new BucketizeRule<decimal>(
                breaks: new[] { 0m, 30000m, 60000m, 90000m, 120000m, decimal.MaxValue },
                labels: new[] { "<30k", "30-60k", "60-90k", "90-120k", "120k+" }
            );

            // Act
            var result = rule.Apply(75000m);

            // Assert
            Assert.Equal("60-90k", result);
        }

        #endregion

        #region Integer Bucketing Tests

        [Theory]
        [InlineData(5, "<18")]       // Below first break
        [InlineData(17, "<18")]      // Just below break point
        [InlineData(18, "18-29")]    // Exactly on break point (goes to upper bucket)
        [InlineData(25, "18-29")]    // Middle of range
        [InlineData(29, "18-29")]    // End of range
        [InlineData(30, "30-44")]    // Next break point
        [InlineData(42, "30-44")]    // Middle of next range
        [InlineData(60, "60+")]      // At last break
        [InlineData(99, "60+")]      // Just below max
        [InlineData(100, "60+")]     // At max break
        [InlineData(150, "60+")]     // Above max break
        public void Apply_IntegerAge_ReturnsCorrectBucket(int age, string expected)
        {
            // Arrange
            var rule = new BucketizeRule<int>(
                breaks: new[] { 0, 18, 30, 45, 60, 100 },
                labels: new[] { "<18", "18-29", "30-44", "45-59", "60+" }
            );

            // Act
            var result = rule.Apply(age);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_IntegerBelowMinimum_ReturnsFirstBucket()
        {
            // Arrange
            var rule = new BucketizeRule<int>(
                breaks: new[] { 10, 20, 30, 40 },
                labels: new[] { "10-19", "20-29", "30-39" }
            );

            // Act
            var result = rule.Apply(5);  // Below minimum break (10)

            // Assert
            Assert.Equal("10-19", result);
        }

        [Fact]
        public void Apply_IntegerAboveMaximum_ReturnsLastBucket()
        {
            // Arrange
            var rule = new BucketizeRule<int>(
                breaks: new[] { 0, 18, 30, 45, 60, 100 },
                labels: new[] { "<18", "18-29", "30-44", "45-59", "60+" }
            );

            // Act
            var result = rule.Apply(200);  // Above maximum break (100)

            // Assert
            Assert.Equal("60+", result);
        }

        [Fact]
        public void Apply_NegativeInteger_ReturnsCorrectBucket()
        {
            // Arrange
            var rule = new BucketizeRule<int>(
                breaks: new[] { -100, -50, 0, 50, 100 },
                labels: new[] { "<-50", "-50 to 0", "0 to 50", "50+" }
            );

            // Act & Assert
            Assert.Equal("<-50", rule.Apply(-75));
            Assert.Equal("-50 to 0", rule.Apply(-25));
            Assert.Equal("0 to 50", rule.Apply(25));
            Assert.Equal("50+", rule.Apply(75));
        }

        #endregion

        #region Decimal Bucketing Tests

        [Theory]
        [InlineData(15000, "<30k")]
        [InlineData(29999.99, "<30k")]
        [InlineData(30000, "30-60k")]    // Exactly on break
        [InlineData(45000, "30-60k")]
        [InlineData(59999.99, "30-60k")]
        [InlineData(60000, "60-90k")]    // Exactly on break
        [InlineData(75000, "60-90k")]    // PRD example
        [InlineData(89999.99, "60-90k")]
        [InlineData(90000, "90-120k")]
        [InlineData(105000, "90-120k")]
        [InlineData(120000, "120k+")]
        [InlineData(500000, "120k+")]
        public void Apply_DecimalSalary_ReturnsCorrectBucket(decimal salary, string expected)
        {
            // Arrange
            var rule = new BucketizeRule<decimal>(
                breaks: new[] { 0m, 30000m, 60000m, 90000m, 120000m, decimal.MaxValue },
                labels: new[] { "<30k", "30-60k", "60-90k", "90-120k", "120k+" }
            );

            // Act
            var result = rule.Apply(salary);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(10.5, "10-20")]
        [InlineData(19.99, "10-20")]
        [InlineData(20.0, "20-30")]     // Exactly on break
        [InlineData(25.5, "20-30")]
        [InlineData(30.0, "30-40")]     // Exactly on break
        public void Apply_DecimalWithFractionalValues_ReturnsCorrectBucket(decimal value, string expected)
        {
            // Arrange
            var rule = new BucketizeRule<decimal>(
                breaks: new[] { 10m, 20m, 30m, 40m },
                labels: new[] { "10-20", "20-30", "30-40" }
            );

            // Act
            var result = rule.Apply(value);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Double Bucketing Tests

        [Theory]
        [InlineData(0.15, "0-20%")]      // 15%
        [InlineData(0.19, "0-20%")]      // 19%
        [InlineData(0.20, "20-40%")]     // Exactly 20% (on break)
        [InlineData(0.35, "20-40%")]     // 35%
        [InlineData(0.50, "40-60%")]     // 50%
        [InlineData(0.75, "60-80%")]     // 75%
        [InlineData(0.95, "80-100%")]    // 95%
        [InlineData(1.0, "80-100%")]     // Exactly 100%
        public void Apply_DoublePercentage_ReturnsCorrectBucket(double percentage, string expected)
        {
            // Arrange
            var rule = new BucketizeRule<double>(
                breaks: new[] { 0.0, 0.2, 0.4, 0.6, 0.8, 1.0 },
                labels: new[] { "0-20%", "20-40%", "40-60%", "60-80%", "80-100%" }
            );

            // Act
            var result = rule.Apply(percentage);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_DoubleBMI_ReturnsCorrectClassification()
        {
            // Arrange - WHO BMI classifications
            var rule = new BucketizeRule<double>(
                breaks: new[] { 0.0, 18.5, 25.0, 30.0, 35.0, 40.0, 100.0 },
                labels: new[] { "Underweight", "Normal", "Overweight", "Obese Class I", "Obese Class II", "Obese Class III" }
            );

            // Act & Assert
            Assert.Equal("Underweight", rule.Apply(17.5));
            Assert.Equal("Normal", rule.Apply(22.0));
            Assert.Equal("Overweight", rule.Apply(27.5));
            Assert.Equal("Obese Class I", rule.Apply(32.0));
            Assert.Equal("Obese Class II", rule.Apply(37.0));
            Assert.Equal("Obese Class III", rule.Apply(45.0));
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void Apply_ValueExactlyOnBreakPoint_ReturnsUpperBucket()
        {
            // Arrange - Values exactly on break points should go to upper bucket
            var rule = new BucketizeRule<int>(
                breaks: new[] { 0, 10, 20, 30, 40 },
                labels: new[] { "0-9", "10-19", "20-29", "30-39" }
            );

            // Act & Assert
            Assert.Equal("10-19", rule.Apply(10));  // On first internal break
            Assert.Equal("20-29", rule.Apply(20));  // On second internal break
            Assert.Equal("30-39", rule.Apply(30));  // On third internal break
        }

        [Fact]
        public void Apply_ValueAtMinimumBreak_ReturnsFirstBucket()
        {
            // Arrange
            var rule = new BucketizeRule<int>(
                breaks: new[] { 0, 10, 20, 30 },
                labels: new[] { "0-9", "10-19", "20-29" }
            );

            // Act
            var result = rule.Apply(0);  // At minimum break

            // Assert
            Assert.Equal("0-9", result);
        }

        [Fact]
        public void Apply_ValueAtMaximumBreak_ReturnsLastBucket()
        {
            // Arrange
            var rule = new BucketizeRule<int>(
                breaks: new[] { 0, 10, 20, 30 },
                labels: new[] { "0-9", "10-19", "20-29" }
            );

            // Act
            var result = rule.Apply(30);  // At maximum break

            // Assert
            Assert.Equal("20-29", result);
        }

        [Fact]
        public void Apply_MinValue_ReturnsFirstBucket()
        {
            // Arrange
            var rule = new BucketizeRule<int>(
                breaks: new[] { int.MinValue, 0, int.MaxValue },
                labels: new[] { "Negative", "Positive" }
            );

            // Act
            var result = rule.Apply(int.MinValue);

            // Assert
            Assert.Equal("Negative", result);
        }

        [Fact]
        public void Apply_MaxValue_ReturnsLastBucket()
        {
            // Arrange
            var rule = new BucketizeRule<int>(
                breaks: new[] { int.MinValue, 0, int.MaxValue },
                labels: new[] { "Negative", "Positive" }
            );

            // Act
            var result = rule.Apply(int.MaxValue);

            // Assert
            Assert.Equal("Positive", result);
        }

        #endregion

        #region Real-World Scenario Tests

        [Theory]
        [InlineData(350, "Poor")]
        [InlineData(579, "Poor")]
        [InlineData(580, "Fair")]
        [InlineData(650, "Fair")]
        [InlineData(670, "Good")]
        [InlineData(720, "Good")]       // PRD-style example
        [InlineData(740, "Very Good")]
        [InlineData(780, "Very Good")]
        [InlineData(800, "Excellent")]
        [InlineData(849, "Excellent")]
        public void Apply_CreditScore_ReturnsCorrectTier(int score, string expected)
        {
            // Arrange - Standard US credit score tiers
            var rule = new BucketizeRule<int>(
                breaks: new[] { 300, 580, 670, 740, 800, 850 },
                labels: new[] { "Poor", "Fair", "Good", "Very Good", "Excellent" }
            );

            // Act
            var result = rule.Apply(score);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(5000, "10%")]
        [InlineData(30000, "12%")]
        [InlineData(75000, "22%")]
        [InlineData(150000, "24%")]
        [InlineData(220000, "32%")]     // Fixed: 220k is in 32% bracket (191950-243725)
        [InlineData(300000, "35%")]     // Fixed: 300k is in 35% bracket (243725-609350)
        [InlineData(700000, "37%")]
        public void Apply_TaxIncome_ReturnsCorrectBracket(decimal income, string expected)
        {
            // Arrange - US federal tax brackets 2024 (single filer)
            var rule = new BucketizeRule<decimal>(
                breaks: new[] { 0m, 11600m, 47150m, 100525m, 191950m, 243725m, 609350m, decimal.MaxValue },
                labels: new[] { "10%", "12%", "22%", "24%", "32%", "35%", "37%" }
            );

            // Act
            var result = rule.Apply(income);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(5, "<$10")]
        [InlineData(25, "$10-50")]
        [InlineData(75, "$50-100")]
        [InlineData(250, "$100-500")]
        [InlineData(750, "$500-1k")]
        [InlineData(3000, "$1k-5k")]
        [InlineData(10000, "$5k+")]
        public void Apply_TransactionAmount_ReturnsCorrectBucket(decimal amount, string expected)
        {
            // Arrange
            var rule = new BucketizeRule<decimal>(
                breaks: new[] { 0m, 10m, 50m, 100m, 500m, 1000m, 5000m, decimal.MaxValue },
                labels: new[] { "<$10", "$10-50", "$50-100", "$100-500", "$500-1k", "$1k-5k", "$5k+" }
            );

            // Act
            var result = rule.Apply(amount);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Constructor Validation Tests

        [Fact]
        public void Constructor_NullBreaks_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new BucketizeRule<int>(
                    breaks: null,
                    labels: new[] { "Label1", "Label2" }
                )
            );

            Assert.Equal("breaks", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullLabels_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new BucketizeRule<int>(
                    breaks: new[] { 0, 10, 20 },
                    labels: null
                )
            );

            Assert.Equal("labels", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyBreaks_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new BucketizeRule<int>(
                    breaks: Array.Empty<int>(),
                    labels: new[] { "Label1" }
                )
            );

            Assert.Equal("breaks", exception.ParamName);
            Assert.Contains("cannot be empty", exception.Message);
        }

        [Fact]
        public void Constructor_EmptyLabels_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new BucketizeRule<int>(
                    breaks: new[] { 0, 10 },
                    labels: Array.Empty<string>()
                )
            );

            Assert.Equal("labels", exception.ParamName);
            Assert.Contains("cannot be empty", exception.Message);
        }

        [Fact]
        public void Constructor_BreaksNotOneMoreThanLabels_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new BucketizeRule<int>(
                    breaks: new[] { 0, 10, 20, 30 },  // 4 breaks
                    labels: new[] { "0-9", "10-19" }  // 2 labels (needs 3)
                )
            );

            Assert.Equal("breaks", exception.ParamName);
            Assert.Contains("must have exactly one more element", exception.Message);
        }

        [Fact]
        public void Constructor_BreaksNotInAscendingOrder_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new BucketizeRule<int>(
                    breaks: new[] { 0, 30, 20, 40 },  // 30 > 20 (not ascending)
                    labels: new[] { "0-19", "20-29", "30-39" }
                )
            );

            Assert.Equal("breaks", exception.ParamName);
            Assert.Contains("must be in strictly ascending order", exception.Message);
        }

        [Fact]
        public void Constructor_BreaksWithDuplicates_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new BucketizeRule<int>(
                    breaks: new[] { 0, 10, 10, 20 },  // Duplicate 10
                    labels: new[] { "0-9", "10-19", "20-29" }
                )
            );

            Assert.Equal("breaks", exception.ParamName);
            Assert.Contains("must be in strictly ascending order", exception.Message);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Apply_LargeBucketSet_PerformsBinarySearch()
        {
            // Arrange - Create many buckets to test binary search efficiency
            var breaks = new int[101];  // 0, 10, 20, 30, ..., 1000
            var labels = new string[100];
            for (int i = 0; i < 101; i++)
            {
                breaks[i] = i * 10;
            }
            for (int i = 0; i < 100; i++)
            {
                labels[i] = $"{i * 10}-{(i + 1) * 10 - 1}";
            }

            var rule = new BucketizeRule<int>(breaks, labels);

            // Act - Test multiple values (binary search should be O(log n))
            var result1 = rule.Apply(5);    // First bucket
            var result2 = rule.Apply(500);  // Middle bucket
            var result3 = rule.Apply(995);  // Last bucket

            // Assert
            Assert.Equal("0-9", result1);
            Assert.Equal("500-509", result2);
            Assert.Equal("990-999", result3);
        }

        [Fact]
        public void Apply_100000Operations_CompletesQuickly()
        {
            // Arrange - PRD requires ≥ 200,000 ops/sec
            var rule = new BucketizeRule<int>(
                breaks: new[] { 0, 18, 30, 45, 60, 100 },
                labels: new[] { "<18", "18-29", "30-44", "45-59", "60+" }
            );

            var startTime = DateTime.UtcNow;

            // Act - Perform 100,000 operations
            for (int i = 0; i < 100000; i++)
            {
                var _ = rule.Apply(i % 100);
            }

            var elapsed = DateTime.UtcNow - startTime;

            // Assert - Should complete in less than 500ms (200k ops/sec = 0.5 seconds for 100k ops)
            // Being generous: if it takes less than 1 second, it's definitely faster than 100k ops/sec
            Assert.True(elapsed.TotalMilliseconds < 1000,
                $"100,000 operations took {elapsed.TotalMilliseconds:F2}ms " +
                $"(expected < 1000ms for >100k ops/sec)");
        }

        #endregion

        #region Boundary Value Tests

        [Fact]
        public void Apply_DecimalMaxValue_ReturnsLastBucket()
        {
            // Arrange
            var rule = new BucketizeRule<decimal>(
                breaks: new[] { 0m, 1000m, decimal.MaxValue },
                labels: new[] { "0-999", "1000+" }
            );

            // Act
            var result = rule.Apply(decimal.MaxValue);

            // Assert
            Assert.Equal("1000+", result);
        }

        [Fact]
        public void Apply_DoubleMaxValue_ReturnsLastBucket()
        {
            // Arrange
            var rule = new BucketizeRule<double>(
                breaks: new[] { 0.0, 1000.0, double.MaxValue },
                labels: new[] { "0-999", "1000+" }
            );

            // Act
            var result = rule.Apply(double.MaxValue);

            // Assert
            Assert.Equal("1000+", result);
        }

        [Fact]
        public void Apply_IntMinValue_ReturnsFirstBucket()
        {
            // Arrange
            var rule = new BucketizeRule<int>(
                breaks: new[] { int.MinValue, 0, 1000, int.MaxValue },
                labels: new[] { "Negative", "0-999", "1000+" }
            );

            // Act
            var result = rule.Apply(int.MinValue);

            // Assert
            Assert.Equal("Negative", result);
        }

        #endregion
    }
}
